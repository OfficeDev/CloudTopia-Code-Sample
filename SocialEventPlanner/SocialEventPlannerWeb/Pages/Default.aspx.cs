using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
using Microsoft.SharePoint.Client;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.Online.SharePoint.Client;


namespace SocialEventPlannerWeb
{
    public partial class Default : System.Web.UI.Page
    {

        //name of the list
        private const string LIST_NAME = "myListName";

        //hidden values for storing the access token on postbacks
        private const string HDN_HOST_WEB = "hdnHostWeb";
        private const string HDN_ACC_TOKEN = "hdnAccessToken";
            

        protected void Page_PreInit(object sender, EventArgs e)
        {
            //Uri redirectUrl;
            //switch (SharePointContextProvider.CheckRedirectionStatus(Context, out redirectUrl))
            //{
            //    case RedirectionStatus.Ok:
            //        return;
            //    case RedirectionStatus.ShouldRedirect:
            //        Response.Redirect(redirectUrl.AbsoluteUri, endResponse: true);
            //        break;
            //    case RedirectionStatus.CanNotRedirect:
            //        Response.Write("An error occurred while processing your request.");
            //        Response.End();
            //        break;
            //}
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                #region HighTrust Version
                ////get the access token and host so that we can use it for other requests on the site
                //var hostWeb = Page.Request["SPHostUrl"];

                ////passing null as the Windows identity so we can use it as an AppOnly request
                //using (var ctx = TokenHelper.GetS2SClientContextWithWindowsIdentity(new Uri(hostWeb), null))
                //{
                //    ctx.Load(ctx.Web, web => web.Title, user => user.CurrentUser);
                //    ctx.ExecuteQuery();
                //    Microsoft.SharePoint.Client.User curUser = ctx.Web.CurrentUser;

                //    //if the current user is me, then show the link to the clean up page
                //    if (curUser.IsSiteAdmin)
                //    {
                //        string link = "<a href='cleanapp.aspx'>Clean Up App</a>";
                //        CleanUpLit.Text = link;
                //    }

                //    //now query the list and get all the social events
                //    Response.Write(ctx.Web.Title);
                //}

                #endregion

                #region LowTrust Version

                if (!IsPostBack)
                {
                    //get the context token and host web
                    var contextToken = TokenHelper.GetContextTokenFromRequest(Page.Request);
                    var hostWeb = Page.Request["SPHostUrl"];

                    //create the tokenContent from it so we can get an AccessToken to use for AppOnly cals
                    SharePointContextToken tokenContent = TokenHelper.ReadAndValidateContextToken(contextToken, Request.Url.Authority);

                    //get the Access tokenj
                    string accessToken = TokenHelper.GetAccessToken(tokenContent.RefreshToken, TokenHelper.SharePointPrincipal,
                        new Uri(hostWeb).Authority, TokenHelper.GetRealmFromTargetUrl(new Uri(hostWeb))).AccessToken;

                    //now store it in view state so we can call out to other pages in our app with it
                    ViewState[HDN_HOST_WEB] = hostWeb;
                    ViewState[HDN_ACC_TOKEN] = accessToken;

                    //write it out to hidden so that it can be used by client code
                    //Url encode the hostWeb so it can be passed to REST endpoint and successfully parsed (otherwise the ":" in the URL blocks it)
                    hiddenLit.Text = GetHiddenHtml(HDN_HOST_WEB, HttpUtility.UrlEncode(hostWeb)) + GetHiddenHtml(HDN_ACC_TOKEN, accessToken);

                    // The following code gets the client context and Title property by using TokenHelper.
                    // To access other properties, the app may need to request permissions on the host web.
                    var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

                    using (var clientContext = spContext.CreateUserClientContextForSPHost())
                    //using (var clientContext = TokenHelper.GetClientContextWithAccessToken(hostWeb, accessToken))
                    {
                        clientContext.Load(clientContext.Web, web => web.Title, user => user.CurrentUser);
                        clientContext.ExecuteQuery();
                        Microsoft.SharePoint.Client.User curUser = clientContext.Web.CurrentUser;

                        //if the current user is me, then show the link to the clean up page
                        if (curUser.IsSiteAdmin)
                        {
                            CleanUpPnl.Visible = true;
                        }

                        ////now query the list and get all the social events
                        //Response.Write(clientContext.Web.Title);

                        #region SQL data test
                        ////TEST TO CHECK OUT DATABASE CONNECTIVITY
                        //SqlConnection cn = new SqlConnection(conStr);
                        //SqlCommand cm = new SqlCommand("tblObjectGraph");
                        //cm.Connection = cn;
                        //cm.CommandText = "select * from tblObjectGraph";
                        //SqlDataAdapter da = new SqlDataAdapter(cm);

                        //DataSet ds = new DataSet();
                        //da.Fill(ds);

                        //string data = string.Empty;
                        //foreach(DataRow dr in ds.Tables[0].Rows)
                        //{
                        //    data += "ID = " + ((double)dr["ObjectGraphID"]).ToString() + "; Url = " + (string)dr["ObjectGraphUrl"] + "; TwitterTags = " + (string)dr["TwitterTags"] + "<br/>";
                        //}

                        //Response.Write("Database data:<p>" + data + "</p>");
                        #endregion

                        #region IIS Info
                        //Response.Write("<p>PhysicalPath = " + Request.PhysicalPath + "<br/>" + 
                        //    "PhysicalApplicationPath = " + Request.PhysicalApplicationPath + "<br/></p>");

                        //string vars = string.Empty;
                        //foreach (string key in Request.ServerVariables.Keys)
                        //{
                        //    vars += key + " = " + Request.ServerVariables[key] + "<br/>";
                        //}

                        //Response.Write("<p>Server Variables:</p><p>" + vars + "</p>");
                        #endregion
                    }
                }
                #endregion

                #region LowTrust VS 2012 Version
                // The following code gets the client context and Title property by using TokenHelper.
                // To access other properties, you may need to request permissions on the host web.

                //var contextToken = TokenHelper.GetContextTokenFromRequest(Page.Request);
                //var hostWeb = Page.Request["SPHostUrl"];

                //using (var clientContext = TokenHelper.GetClientContextWithContextToken(hostWeb, contextToken, Request.Url.Authority))
                //{
                //    clientContext.Load(clientContext.Web, web => web.Title, user => user.CurrentUser);
                //    clientContext.ExecuteQuery();
                //    Response.Write(clientContext.Web.Title);
                //}
                #endregion
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Response.Write("ERROR: " + ex.Message);
            }
        }

        private string GetHiddenHtml(string key, string value)
        {
            return "<input type=\"hidden\" id=\"" + key + "\" name=\"" + key + "\" value=\"" + value + "\" />";
        }

        protected void CleanUpBtn_Click(object sender, EventArgs e)
        {
            try
            {
                //get the access info
                //now store it in view state so we can call out to other pages in our app with it
                string hostWeb = (string)ViewState[HDN_HOST_WEB];
                string accessToken = (string)ViewState[HDN_ACC_TOKEN];

                using (ClientContext ctx = TokenHelper.GetClientContextWithAccessToken(hostWeb, accessToken))
                {
                    List l = ctx.Web.Lists.GetByTitle(LIST_NAME);
                    ctx.Load(l);
                    ctx.ExecuteQuery();

                    l.DeleteObject();
                    ctx.ExecuteQuery();

                    StatusLbl.CssClass = "goodStatus";
                    StatusLbl.Text = "The list was deleted.  You now need to reinstall the application.";
                    CleanUpBtn.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                StatusLbl.CssClass = "badStatus";

                if (ex.Message.ToLower().Contains("does not exist"))
                    StatusLbl.Text = "The list does not exist, it may have been already deleted, in which case the application needs to be installed again.";
                else
                    StatusLbl.Text = "There was a problem deleting the list: " + ex.Message;
            }

        }

        protected void AddEventBtn_Click(object sender, EventArgs e)
        {
            try
            {
                //here's what we need to do:
                //1.  Create a new site
                //2.  Get the Url and create a new ObjectGraph item
                //3.  Record the Url, OG and TwitterTag data in the SharePoint list
                //4.  Record the Url, OG and TwitterTag data in SQL
                //5.  Query to get the list of events again and bind to the grid

                string hostWeb = Request.Form[HDN_HOST_WEB];
                string encodedToken = Request.Form[HDN_ACC_TOKEN];

                string newUrl = AddNewSite(encodedToken, hostWeb, Request.Form["eventNameTxt"], Request.Form["eventDateTxt"]);
                if (! string.IsNullOrEmpty(newUrl))
                {
                    StatusLbl.Text = "Congratulations - your new event site has been created at " + newUrl;
                    StatusLbl.CssClass = "goodStatus";
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                StatusLbl.Text = "Sorry, we couldn't create your event.  Here's what happened: " + ex.Message;
                StatusLbl.CssClass = "badStatus";
            }
        }

        //RETURNS THE URL OF THE NEWLY CREATED SITE
        private string AddNewSite(string encodedToken, string hostWeb, string eventName, string eventDate)
        {
            string result = string.Empty;

            try
            {
                //assume that the token being used is the base64 encoded access token sent to the client
                byte[] bToken = Convert.FromBase64String(encodedToken);
                string accessToken = System.Text.UTF8Encoding.UTF8.GetString(bToken);

                using (ClientContext ctx = TokenHelper.GetClientContextWithAccessToken(hostWeb, accessToken))
                {
                    //get the current user to set as owner
                    var currUser = ctx.Web.CurrentUser;
                    ctx.Load(currUser);
                    ctx.ExecuteQuery();

                    //get the Urls used to create the new site
                    int uniqueUrl = 0;
                    string baseUrl = "https://yammo.sharepoint.com/sites/" + eventName;
                    string webUrl = baseUrl;
                    Uri tenantAdminUri = new Uri("https://yammo-admin.sharepoint.com");
                
                    //get the realm
                    string realm = TokenHelper.GetRealmFromTargetUrl(tenantAdminUri);

                    //get the app only access token for the admin site
                    var token = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, tenantAdminUri.Authority, realm).AccessToken;

                    //create the client context for the admin site
                    using (var adminContext = TokenHelper.GetClientContextWithAccessToken(tenantAdminUri.ToString(), token))
                    {
                        var tenant = new Tenant(adminContext);

                        //look to see if a site already exists at that Url; if it does then create it 
                        bool siteExists = true;

                        while (siteExists)
                        {
                            try
                            {
                                //look for the site
                                Site s = tenant.GetSiteByUrl(webUrl);
                                adminContext.Load(s);
                                adminContext.ExecuteQuery();

                                //if it exists then update the webUrl                            
                                uniqueUrl += 1;
                                webUrl = baseUrl + uniqueUrl.ToString();
                            }
                            catch
                            {
                                //doesn't exist, therefore the Url is good
                                siteExists = false;
                            }
                        }

                        var properties = new SiteCreationProperties()
                        {
                            Url = webUrl,
                            Owner = currUser.Email,
                            Title = eventName + " on " + eventDate,
                            Template = "STS#0",
                            StorageMaximumLevel = 100, 
                            UserCodeMaximumLevel = 0
                        };

                        //start the SPO operation to create the site
                        SpoOperation op = tenant.CreateSite(properties);
                        adminContext.Load(tenant);
                        adminContext.Load(op, i => i.IsComplete);
                        adminContext.ExecuteQuery();

                        //check if site creation operation is complete
                        while (!op.IsComplete)
                        {
                            //wait 30seconds and try again
                            System.Threading.Thread.Sleep(30000);
                            op.RefreshLoad();
                            adminContext.ExecuteQuery();
                        }

                        //set the webUrl for the return value
                        result = webUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                result = string.Empty;
                Debug.WriteLine(ex.Message);
                StatusLbl.Text = "Sorry, we couldn't create your event.  Here's what happened: " + ex.Message;
                StatusLbl.CssClass = "badStatus";
            }

            return result;
        }
    }
}