using Nancy;
using Nancy.Cryptography;
using Neustart.Steam;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neustart.APIServer
{
    public class JSONResponse
    {
        public SteamUser SteamSession;
        public List<AppConfig> Servers;
    }

    public class JSONContainer
    {
        public bool Status { get => Response != null; protected set { } }
        public string StatusMessage;
        public JSONResponse Response;

        public JSONContainer(JSONResponse obj = null)
        {
            Response = obj;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Routing : NancyModule
    {
        public Routing()
        {
            Get("/", MainPage);
            Get("/login", Login);
            Get("/logout", Logout);
            Get("/servers", GetServers);
            Get("/server/{id}", ManageServer);
        }

        private dynamic MainPage(dynamic parameters)
        {
            if (Session["state"] == null)
                Session["state"] = CryptographyConfiguration.Default.EncryptionProvider.Encrypt(DateTime.Now.ToLongDateString() + (new Random()).Next(1, 123124));

            if (Session["steam"] == null)
                return View["Login.html"];
            else
                return View[new ViewModel() { SteamSession = Session["steam"] as SteamUser }];
        }

        private Response GetServers(dynamic parameters)
        {
            if (Session["steam"] == null)
                return Response.AsJson(new JSONContainer
                {
                    StatusMessage = "You are not logged in or whitelisted."
                });

            return Response.AsJson(new JSONContainer(new JSONResponse
            {
                SteamSession = Session["steam"] as SteamUser,
                Servers = Core.GetContainer().m_AppContainer.Select(e => e.Config).ToList()
            }));
        }

        private async Task<Response> Login(dynamic parameters)
        {
            if (Session["steam"] == null && Request.Query["openid.return_to"] == null)
                return Response.AsRedirect(SteamLogin.BuildLoginQuery(Core.Cfg.Steam));

            SteamLogin steam = new SteamLogin(Core.Cfg.Steam);
            await steam.ValidateToken(Request);

            SteamUser user = await steam.GetUser();
            if (user != null)
            {
                if (Core.Cfg.Web.AllowedSteamIDs.Contains(user.SteamID64) || Core.Cfg.Web.AllowedSteamIDs.Contains(user.SteamID32))
                {
                    Debug.Log($"{user.Name} - {user.SteamID32} - {user.SteamID64} authenticated");
                    Session["steam"] = user;
                }
            }

            return Response.AsRedirect("/");
        }

        private Response Logout(dynamic parameters)
        {
            Session.DeleteAll();
            return Response.AsRedirect("/");
        }

        private Response ManageServer(dynamic parameters)
        {
            if (Session["steam"] == null)
                return Response.AsJson(new JSONContainer
                {
                    StatusMessage = "You are not logged in or whitelisted."
                });

            App server = Core.GetContainer().m_AppContainer.Find(app => app.Config.ID == (string)parameters.id);
            if (server == null)
                return Response.AsJson(new JSONContainer
                {
                    StatusMessage = "There is no server with this ID available"
                });

            SteamUser user = Session["steam"] as SteamUser;

            string mode = server.Running ? "stopping" : "running";
            Debug.Log($"{user.Name} - {user.SteamID32} - {user.SteamID64} is {mode} '{server.Config.ID}'");

            if (server.Running)
                server.Stop();
            else
                server.Start();

            return Response.AsJson(new JSONContainer
            {
                StatusMessage = $"You are {mode} the process. Please allow 20 seconds before trying again if it doesn't work"
            });
        }

    }
}