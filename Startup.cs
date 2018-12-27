using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(OwinTwilio.Startup))]
namespace OwinTwilio
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
