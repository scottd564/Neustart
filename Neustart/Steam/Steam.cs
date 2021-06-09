using Nancy;
using Nancy.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Neustart.Steam
{
    public class SteamLogin
    {
        private SteamConfig cfg;
        public bool IsValid;

        public string SteamID64 { get; internal set; }
        public SteamUser User { get; internal set; }

        public SteamLogin(SteamConfig cfg)
        {
            this.cfg = cfg;
        }

        public static string BuildLoginQuery(SteamConfig cfg)
        {
            var param = new Dictionary<string, string>()
            {
                {"openid.mode", "checkid_setup"},
                {"openid.return_to", cfg.OAuthReturn },
                {"openid.realm", cfg.OAuthReturn },
                {"openid.ns", "http://specs.openid.net/auth/2.0"},
                {"openid.ns.sreg", "http://openid.net/extensions/sreg/1.1"},
                {"openid.claimed_id", "http://specs.openid.net/auth/2.0/identifier_select" },
                {"openid.identity", "http://specs.openid.net/auth/2.0/identifier_select" }
            };

            return cfg.OAuthUri + "?" + string.Join("&", param.Select(kvp => kvp.Key + "=" + HttpUtility.UrlEncode(kvp.Value)));
        }

        public string BuildQuery(Request req)
        {
            var param = new Dictionary<string, string>(){
                {"openid.mode", "check_authentication"},
                {"openid.assoc_handle", req.Query["openid.assoc_handle"]},
                {"openid.signed", req.Query["openid.signed"]},
                {"openid.sig", req.Query["openid.sig"]},
                {"openid.ns", "http://specs.openid.net/auth/2.0"},
            };

            string[] signed = ((string)req.Query["openid.signed"]).Split(',');
            foreach (string id in signed)
            {
                string key = "openid." + id;
                if (!param.ContainsKey(key))
                    param.Add(key, req.Query[key]);
            }

            return string.Join("&", param.Select(kvp => kvp.Key + "=" + HttpUtility.UrlEncode(kvp.Value)));
        }

        public async Task<bool> ValidateToken(Request req)
        {
            string returnUri = req.Query["openid.return_to"];
            if (returnUri != cfg.OAuthReturn)
                return false;

            StringContent query = new StringContent(BuildQuery(req));
            query.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage msg = await httpClient.PostAsync(cfg.OAuthUri, query);
                string content = await msg.Content.ReadAsStringAsync();
                IsValid = content?.Contains("is_valid:true") ?? false;

                if (IsValid)
                    SteamID64 = ((string)req.Query["openid.claimed_id"]).Split('/').Last();

                return IsValid;
            }
        }

        public async Task<SteamUser> GetUser()
        {
            if (!IsValid) return null;

            string url = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + cfg.API + "&steamids=" + SteamID64;
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    string content = await httpClient.GetStringAsync(url);
                    JObject data = JObject.Parse(content);
                    JToken res = data?["response"]?["players"]?[0];

                    if (res != null)
                    {
                        return User = new SteamUser
                        {
                            Name = (string)res["personaname"],
                            Avatar = (string)res?["avatarfull"],
                            SteamID64 = (string)res?["steamid"],
                            SteamID32 = SteamID64ToSteamID32((string)res?["steamid"])
                        };
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return null;
        }

        public static string DiscordIDToSteamID64(string id)
        {
            string bin = Convert.ToString(long.Parse(id), 2)
                        .PadLeft(64, '0')
                        .Remove(31, 1)
                        .Insert(31, "1");

            return Convert.ToInt64(bin, 2).ToString();
        }

        public static string SteamID64ToSteamID32(long id)
        {
            long z = (id - 76561197960265728) / 2;
            long y = id % 2;
            return $"STEAM_0:{y}:{z}";
        }

        public static string SteamID64ToSteamID32(string id)
        {
            return SteamID64ToSteamID32(long.Parse(id));
        }

        public bool IsValidSteamID32(string id32)
        {
            throw new NotImplementedException();
        }

        public bool IsValidSteamID64(string id64)
        {
            throw new NotImplementedException();
        }
    }
}
