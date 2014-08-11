using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Http;


namespace SocialEventPlannerWeb
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            //for WebApi support
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}