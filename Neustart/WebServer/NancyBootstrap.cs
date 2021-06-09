using Nancy;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.Security;
using Nancy.Session;
using Nancy.TinyIoc;

namespace Neustart
{
    class NancyBoostrap : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            Conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Content"));
            Conventions.ViewLocationConventions.Add((viewName, model, context) => "Content/Views/" + viewName);

            Csrf.Enable(pipelines);
            CookieBasedSessions.Enable(pipelines, new CookieBasedSessionsConfiguration()
            {
                Serializer = new JsonSessionSerialiser()
            });
        }

        public override void Configure(INancyEnvironment environment)
        {
            bool debug = Core.Cfg.Web.DebugMode;
            environment.Tracing(enabled: debug, displayErrorTraces: debug);
        }
    }
}