using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SocialEventPlannerWeb.DataClasses;
using System.Diagnostics;
using Microsoft.SharePoint.Client;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.Online.SharePoint.Client;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.SharePoint.Client.WebParts;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Cors;


namespace SocialEventPlannerWeb
{
    public class EventsController : ApiController
    {

        //content for Yammer
        private string yammerAccessToken = "myYammerAccessToken";

        //vars for working with Yammer data
        private string sessionCookie = string.Empty;
        private string yamtrackCookie = string.Empty;
        private CookieContainer cc = new CookieContainer(2);
        private HttpWebResponse wResp;
        private HttpWebRequest wr;

        //yammer URLs
        private string graphPostUrl = "https://www.yammer.com/api/v1/activity.json";
        private string searchUrl = "https://www.yammer.com/api/v1/search.json";
        private string messageUrl = "https://www.yammer.com/api/v1/messages";

        //Twitter stuff
        private const string TWT_CONSUMER_KEY = "myTwitterKey";
        private const string TWT_CONSUMER_SECRET = "myTwitterSecret";
        private string TWT_ACCESS_TOKEN = string.Empty;

        //twitter Urls
        /*
            <TwitterBaseSearchUrl>https://api.twitter.com/1.1/search/tweets.json?q=</TwitterBaseSearchUrl>
            <TwitterBaseMobileUrl>https://mobile.twitter.com/</TwitterBaseMobileUrl>
            <TwitterRequestTokenUrl>https://api.twitter.com/oauth/request_token</TwitterRequestTokenUrl>
            <TwitterAuthorizeUrl>https://api.twitter.com/oauth/authorize</TwitterAuthorizeUrl>
            <TwitterAccessTokenUrl>https://api.twitter.com/oauth/access_token</TwitterAccessTokenUrl>
            <TwitterOauthRequestUrl>https://api.twitter.com/oauth2/token</TwitterOauthRequestUrl>
        */

        private string TWT_OAUTH_URL = "https://api.twitter.com/oauth2/token";
        private string TWT_SEARCH_URL = "https://api.twitter.com/1.1/search/tweets.json?q=";



        //sharepoint content
        private const string LIST_NAME = "myListName";

        //database connection
        private string conStr = "Data Source=tcp:mydatabaseserver.database.windows.net,1433;Initial Catalog=mydatabasename;User Id=myuserid;Password=myPassword;";


        // GET api/<controller>
        [Route("api/events/currentevents")]
        [HttpPost]
        public List<SocialEvent> Get([FromBody]string value)
        {
            List<SocialEvent> results = new List<SocialEvent>();

            try
            {
                SocialEvent se = SocialEvent.ParseJson(value);
                results = GetEventListItems(se.hostUrl, se.accessToken);
            }
            catch (Exception ex)
            {
                if (results.Count == 0)
                {
                    SocialEvent seEx = new SocialEvent();
                    results.Add(seEx);
                }

                results[0].success = false;
                results[0].errorMessage = ex.Message + "; STACK TRACE: " + ex.StackTrace;
            }

            return results;
        }

        public async Task<HttpResponseMessage> Get()
        {
            HttpResponseMessage result = Request.CreateResponse(HttpStatusCode.OK);

            try
            {
                await Task.Run( () => UpdateYammerWithTwitterContent());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                result = Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message, ex);
            }

            return result;
        }


        //// GET api/<controller>/5
        //public string Get(int id)
        //{
        //    return "value";
        //}


        // POST api/<controller>
        public SocialEvent Post([FromBody]string value)
        {
            //here's what we need to do:
            //1.  Create a new site
            //2.  Get the Url and create a new ObjectGraph item
            //3.  Record the Url, event name, event date, and TwitterTag data in the SharePoint list
            //4.  Record the OG ID, OG URL, and TwitterTag data in SQL

            SocialEvent se = new SocialEvent();
            se.success = false;

            try
            {
                //get the data that was uploaded; both convert options fail so wrote my own
                //se = SocialEvent.GetInstanceFromJson(value);
                //se = JsonConvert.DeserializeObject<SocialEvent>(value);
                se = SocialEvent.ParseJson(value);
                se.rawPostData = value;

                //create a new site
                string newUrl = AddNewSite(se.accessToken, se.hostUrl, se.eventName, se.eventDate);

                //if it works, plug in the new site url
                if (!string.IsNullOrEmpty(newUrl))
                {
                    //plug the new Url into the return value
                    se.newSiteUrl = newUrl;

                    //create a new GraphObject item
                    YammerGraphObjectItem gi = CreateOpenGraphItem(newUrl, se.eventName, se.eventDate, se.twitterTags);

                    if (gi != null)
                    {
                        //record the Url, event name, event date, and TwitterTag data in the SharePoint list
                        if (AddEventToSharePoint(newUrl, se.eventName, se.eventDate, se.twitterTags, gi.object_id, se.accessToken, se.hostUrl))
                        {
                            //add the script editor to the home page of the new site and use the Yammer Embed to show the object graph discussion
                            AddYammerEmbedToSharePointSite(newUrl, se.accessToken);

                            //record the OG ID, OG URL, and TwitterTag data in SQL
                            using (SqlConnection cn = new SqlConnection(conStr))
                            {
                                cn.Open();

                                SqlCommand cm = new SqlCommand("addEvent", cn);
                                cm.CommandType = CommandType.StoredProcedure;

                                cm.Parameters.Add(new SqlParameter("@ObjectGraphID", Double.Parse(gi.object_id)));
                                cm.Parameters.Add(new SqlParameter("@ObjectGraphUrl", newUrl));
                                cm.Parameters.Add(new SqlParameter("@TwitterTags", se.twitterTags));
                                cm.Parameters.Add(new SqlParameter("@EventName", se.eventName));
                                cm.Parameters.Add(new SqlParameter("@EventDate", se.eventDate));

                                cm.ExecuteNonQuery();

                                cn.Close();
                            }
                            se.success = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                se.errorMessage = ex.Message + "; STACK TRACE: " + ex.StackTrace;
            }

            return se;
        }

        [Route("api/events/updateevent")]
        [HttpPost]
        public SocialEvent UpdateEvent([FromBody]string value)
        {
            SocialEvent se = new SocialEvent();

            try
            {
                //get the data that was passed in 
                se = SocialEvent.ParseJson(value);

                //update the SharePoint list item
                if (UpdateSharePointListItem(se.accessToken, se.hostUrl, se.objectGraphID, se.twitterTags))
                {
                    //update SQL Azure
                    using (SqlConnection cn = new SqlConnection(conStr))
                    {
                        cn.Open();

                        SqlCommand cm = new SqlCommand("updateEventTags", cn);
                        cm.CommandType = CommandType.StoredProcedure;

                        cm.Parameters.Add(new SqlParameter("@ObjectGraphID", Double.Parse(se.objectGraphID)));
                        cm.Parameters.Add(new SqlParameter("@TwitterTags", se.twitterTags));

                        cm.ExecuteNonQuery();

                        cn.Close();
                    }

                    se.success = true;
                }
                else
                {
                    se.success = false;
                    se.errorMessage = "Unable to update SharePoint list item";
                }
                
            }
            catch (Exception ex)
            {
                se.success = false;
                se.errorMessage = ex.Message + "; STACK TRACE: " + ex.StackTrace;
            }

            return se;
        }


        [Route("api/events/deleteevent")]
        [HttpPost]
        public SocialEvent DeleteEvent([FromBody]string value)
        {
            SocialEvent se = new SocialEvent();

            try
            {
                //get the data that was passed in 
                se = SocialEvent.ParseJson(value);

                //update the SharePoint list item
                if (DeleteSharePointListItem(se.accessToken, se.hostUrl, se.objectGraphID))
                {
                    //update SQL Azure
                    using (SqlConnection cn = new SqlConnection(conStr))
                    {
                        cn.Open();

                        SqlCommand cm = new SqlCommand("deleteEvent", cn);
                        cm.CommandType = CommandType.StoredProcedure;

                        cm.Parameters.Add(new SqlParameter("@ObjectGraphID", Double.Parse(se.objectGraphID)));

                        cm.ExecuteNonQuery();

                        cn.Close();
                    }

                    se.success = true;
                }
                else
                {
                    se.success = false;
                    se.errorMessage = "Unable to delete SharePoint list item";
                }

            }
            catch (Exception ex)
            {
                se.success = false;
                se.errorMessage = ex.Message + "; STACK TRACE: " + ex.StackTrace;
            }

            return se;
        }


        [Route("api/events/search")]
        [HttpGet]
        public CortanaSearchResult SearchEvents(string tagName)
        {
            CortanaSearchResult result = new CortanaSearchResult();

            //search SQL Azure
            try
            {
                using (SqlConnection cn = new SqlConnection(conStr))
                {
                    SqlCommand cm = new SqlCommand("findEventByTag", cn);
                    cm.CommandType = CommandType.StoredProcedure;
                    cm.Parameters.Add(new SqlParameter("@Tag", "%" + tagName + "%"));

                    SqlDataAdapter da = new SqlDataAdapter(cm);

                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    if (
                        (ds != null) &&
                        (ds.Tables.Count > 0) &&
                            (ds.Tables[0].Rows.Count > 0)
                        )
                    {
                        var sqlResults = from DataRow dr in ds.Tables[0].Rows
                                         select new SearchResultEventData
                                         {
                                             ObjectGraphID = ((double)dr["ObjectGraphID"]).ToString(),
                                             ObjectGraphUrl = (string)dr["ObjectGraphUrl"],
                                             TwitterTags = (string)dr["TwitterTags"],
                                             EventName = (string)dr["EventName"],
                                             EventDate = ((DateTime)dr["EventDate"]).ToShortDateString()
                                         };

                        result.Events = sqlResults.ToList<SearchResultEventData>();
                        
                        //NOTE:  Only search Yammer if you found one or more matching events; it's not 
                        //designed to be a general purpose Yammer search (although it could); it's designed
                        //to find Yammer messages that might be related to events we found
                        try
                        {
                            string response = MakeGetRequest(searchUrl + "?search=" + tagName + "&match=any-exact", yammerAccessToken);
                            YammerSearchResults ysr = YammerSearchResults.GetInstanceFromJson(response);

                            //look for the item in the results
                            if ((ysr != null) && (ysr.Messages.Messages.Count > 0))
                                result.YammerMessages = ysr.Messages.Messages;
                        }
                        catch (Exception yammerEx)
                        {
                            result.IsError = true;
                            result.ErrorMessage += Environment.NewLine +
                                "Error message searching Yammer: " + yammerEx.Message +
                                "; stack trace: " + yammerEx.StackTrace;
                        }
                    }
                }
            }
            catch (Exception sqlEx)
            {
                result.IsError = true;
                result.ErrorMessage = "Error searching SQL: " +
                    sqlEx.Message + "; stack trace: " + sqlEx.StackTrace;
            }


            return result;
        }


        //// PUT api/<controller>/5
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/<controller>/5
        //public void Delete(int id)
        //{
        //}


        private List<SocialEvent> GetEventListItems(string hostUrl, string accessToken)
        {
            List<SocialEvent> results = new List<SocialEvent>();

            try
            {
                using (ClientContext ctx = TokenHelper.GetClientContextWithAccessToken(hostUrl, accessToken))
                {
                    //get our event list
                    List eventsList = ctx.Web.Lists.GetByTitle(LIST_NAME);

                    CamlQuery query = CamlQuery.CreateAllItemsQuery(100, new string[] { "EventName", "EventDate", "TwitterTags", "ObjectGraphID", "SiteUrl" });
                    ListItemCollection items = eventsList.GetItems(query);
                    ctx.Load(items, itms => itms.OrderBy(itm => itm["EventDate"]));
                    ctx.ExecuteQuery();

                    #region another ordering option
                    //var orderedItems = from ListItem li in items
                    //                   orderby li["EventDate"] descending
                    //                   select new SocialEvent
                    //                   {
                    //                       eventName = (string)li["EventName"],
                    //                       eventDate = (string)li["EventDate"],
                    //                       twitterTags = (string)li["TwitterTags"],
                    //                       objectGraphID = (string)li["ObjectGraphID"],
                    //                       newSiteUrl = (string)li["SiteUrl"]
                    //                   };

                    ////put in a separate try block here because we don't want zero items to throw an 
                    ////exception up the chain
                    //try
                    //{
                    //    results = orderedItems.ToList<SocialEvent>();
                    //}
                    //catch
                    //{
                    //    //ignore
                    //}
                    #endregion

                    foreach (ListItem li in items)
                    {
                        results.Add(new SocialEvent((string)li["EventName"],
                            ((DateTime)li["EventDate"]).ToShortDateString(),
                            (string)li["TwitterTags"],
                            (string)li["ObjectGraphID"],
                            (string)li["SiteUrl"]));
                    } 
                }
            }
            catch (Exception)
            {            
                throw;
            }

            return results;
        }


        private bool DeleteSharePointListItem(string accessToken, string hostUrl, string objectGraphID)
        {
            bool result = true;

            try
            {
                int id = GetSharePointListItemID(accessToken, hostUrl, objectGraphID);

                if (id > 0)
                {
                    using (ClientContext ctx = TokenHelper.GetClientContextWithAccessToken(hostUrl, accessToken))
                    {
                        //get the list
                        List appList = ctx.Web.Lists.GetByTitle(LIST_NAME);

                        //delete the item
                        ListItem li = appList.GetItemById(id);
                        li.DeleteObject();
                        ctx.ExecuteQuery();
                    }
                }
                else
                    result = false;
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }


        private bool UpdateSharePointListItem(string accessToken, string hostUrl, string objectGraphID, string twitterTags)
        {
            bool result = true;

            try
            {
                int id = GetSharePointListItemID(accessToken, hostUrl, objectGraphID);

                if (id > 0)
                {
                    using (ClientContext ctx = TokenHelper.GetClientContextWithAccessToken(hostUrl, accessToken))
                    {
                        //get the list
                        List appList = ctx.Web.Lists.GetByTitle(LIST_NAME);

                        //update the item
                        ListItem li = appList.GetItemById(id);
                        li["TwitterTags"] = twitterTags;
                        li.Update();
                        ctx.ExecuteQuery();
                    }
                }
                else
                    result = false;
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        private int GetSharePointListItemID(string accessToken, string hostUrl, string objectGraphID)
        {
            int result = 0;

            try
            {
                using (ClientContext ctx = TokenHelper.GetClientContextWithAccessToken(hostUrl, accessToken))
                {
                    //get the list
                    List appList = ctx.Web.Lists.GetByTitle(LIST_NAME);

                    //need the CAML changed so that ViewXml is CAML that only 
                    CamlQuery cq = new CamlQuery();

                    cq.ViewXml = @"<View>" +
                        "<Query>" +
                            "<Where>" +
                            "<Eq>" +
                                "<FieldRef Name='ObjectGraphID'/>" +
                                "<Value Type='Text'>" + objectGraphID + "</Value>" +
                            "</Eq>" +
                            "</Where>" +
                        "</Query>" +
                        "</View>";

                    Microsoft.SharePoint.Client.ListItemCollection items =
                        appList.GetItems(cq);

                    ctx.Load(items, flds => flds.IncludeWithDefaultProperties(item => item.DisplayName,
                        item => item["ObjectGraphID"], item => item["TwitterTags"], item => item["EventName"], item => item["ID"]));

                    ctx.ExecuteQuery();

                    //make sure we got at least one item back
                    if (items.Count == 1)
                    {
                        //set the return value
                        result = (int)items[0]["ID"];
                    }
                    else
                        throw new Exception("Unable to find SharePoint list item");
                }
            }
            catch (Exception)
            {              
                throw;
            }

            return result;
        }


        //RETURNS THE URL OF THE NEWLY CREATED SITE
        private string AddNewSite(string accessToken, string hostUrl, string eventName, string eventDate)
        {
            string result = string.Empty;

            try
            {
                using (ClientContext ctx = TokenHelper.GetClientContextWithAccessToken(hostUrl, accessToken))
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
                                try
                                {
                                    //doesn't exist, need to check deleted sites too 
                                    DeletedSiteProperties dsp = tenant.GetDeletedSitePropertiesByUrl(webUrl);
                                    adminContext.Load(dsp);
                                    adminContext.ExecuteQuery();

                                    //if it exists then update the webUrl                            
                                    uniqueUrl += 1;
                                    webUrl = baseUrl + uniqueUrl.ToString();
                                }
                                catch 
                                {
                                    //okay it REALLY doesn't exist, so go ahead and grab this url
                                    siteExists = false;
                                }
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
                            System.Threading.Thread.Sleep(20000);
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
                Debug.WriteLine(ex.Message);
                throw;
            }

            return result;
        }

        private YammerGraphObjectItem CreateOpenGraphItem(string Url, string eventName, string eventDate, string twitterTags)
        {
            YammerGraphObjectItem gi = null;

            try
            {
                YammerGraphObject go = new YammerGraphObject();
                go.Activity.Action = "create";
                go.Activity.Actor = new YammerActor("Steve Peschka", "speschka@yammo.onmicrosoft.com");
                go.Activity.Message = "This is the discussion page for the " + eventName + " event on " + eventDate;

                go.Activity.Users.Add(new YammerActor("Anne Wallace", "annew@yammo.onmicrosoft.com"));
                go.Activity.Users.Add(new YammerActor("Garth Fort", "garthf@yammo.onmicrosoft.com"));

                YammerGraphObjectInstance jo = new YammerGraphObjectInstance();
                jo.Url = Url;
                jo.Title = eventName;  
                jo.Description = "This is the discussion page for the " + eventName + " event on " + eventDate;  
                jo.Image = "https://socialevents.azurewebsites.net/images/eventplanning.png"; 
                jo.Type = "document";

                go.Activity.Object = jo;

                string postData = go.ToString();

                string response = MakePostRequest(postData, graphPostUrl, yammerAccessToken, "application/json");

                //serialize the results into an object with the OG ID
                if (!string.IsNullOrEmpty(response))
                {
                    gi = JsonConvert.DeserializeObject<YammerGraphObjectItem>(response);
                    string newMsg = "Welcome to the Yammer discussion for the " + eventName + 
                        " event, happening on " + eventDate + ".  We'll also be tracking external " + 
                        "discussions about this event on Twitter by using the tags " + twitterTags + ".";

                    CreateOpenGraphPost(gi.object_id, newMsg);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

            return gi;
        }

        private bool CreateOpenGraphPost(string ID, string Message)
        {
            bool result = true;

            try
            {
                string msg = "body=" + Message + "&attached_objects[]=open_graph_object:" +
                            ID + "&skip_body_notifications=true";

                //try adding the message
                string response = MakePostRequest(msg, messageUrl + ".json", yammerAccessToken);

                if (string.IsNullOrEmpty(response))
                    result = false;
            }
            catch (Exception)
            {          
                throw;
            }

            return result;
        }

        private bool AddEventToSharePoint(string siteUrl, string eventName, string eventDate, string twitterTags, string objectGraphID, string accessToken, string hostUrl)
        {
            bool result = true;

            try
            {
                using (ClientContext ctx = TokenHelper.GetClientContextWithAccessToken(hostUrl, accessToken))
                {

                    //get our event list
                    List eventsList = ctx.Web.Lists.GetByTitle(LIST_NAME);

                    //create the list item
                    ListItemCreationInformation ci = new ListItemCreationInformation();
                    ListItem newItem = eventsList.AddItem(ci);

                    newItem["Title"] = eventName;
                    newItem["SiteUrl"] = siteUrl;
                    newItem["EventName"] = eventName;
                    newItem["EventDate"] = eventDate;
                    newItem["TwitterTags"] = twitterTags;
                    newItem["ObjectGraphID"] = objectGraphID;

                    //update the list item
                    newItem.Update();

                    //add the item to the list
                    ctx.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                result = false;
                throw;
            }

            return result;
        }

        private bool AddYammerEmbedToSharePointSite(string SiteUrl, string accessToken)
        {
            bool result = true;

            try
            {
                Uri siteUri = new Uri(SiteUrl);

                //get the realm
                string realm = TokenHelper.GetRealmFromTargetUrl(siteUri);

                //get the app only access token for the admin site
                var token = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, siteUri.Authority, realm).AccessToken;

                //create the client context for the new site
                using (ClientContext ctx = TokenHelper.GetClientContextWithAccessToken(SiteUrl, token))
                {
                    ctx.Load(ctx.Web, w => w.RootFolder, w => w.RootFolder.WelcomePage, w => w.ServerRelativeUrl);
                    ctx.ExecuteQuery();

                    const string URL_REPLACE = "$URL$";

                    //NOTE:  Getting the embedded code causes some issue that has the thing fail with invalid Xml or DWP 
                    string partTxt = @"<webParts>
  <webPart xmlns=""http://schemas.microsoft.com/WebPart/v3"">
    <metaData>
      <type name=""Microsoft.SharePoint.WebPartPages.ScriptEditorWebPart, Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"" />
      <importErrorMessage>Cannot import this Web Part.</importErrorMessage>
    </metaData>
    <data>
      <properties>
        <property name=""ExportMode"" type=""exportmode"">All</property>
        <property name=""HelpUrl"" type=""string"" />
        <property name=""Hidden"" type=""bool"">False</property>
        <property name=""Description"" type=""string"">Allows authors to insert HTML snippets or scripts.</property>
        <property name=""Content"" type=""string"">    &lt;script type=""text/javascript""
        src=""https://assets.yammer.com/assets/platform_embed.js""&gt;&lt;/script&gt;

    &lt;div id=""embedded-feed"" style=""height:400px;width:500px;""&gt;&lt;/div&gt;

    &lt;script&gt;

        yam.connect.embedFeed({
            container: ""#embedded-feed"",
            network: ""yammo.onmicrosoft.com"",
            feedType: ""open-graph"",
            objectProperties: {
                url: ""$URL$""
            }
        });
    &lt;/script&gt;
</property>
        <property name=""CatalogIconImageUrl"" type=""string"" />
        <property name=""Title"" type=""string"">Script Editor</property>
        <property name=""AllowHide"" type=""bool"">True</property>
        <property name=""AllowMinimize"" type=""bool"">True</property>
        <property name=""AllowZoneChange"" type=""bool"">True</property>
        <property name=""TitleUrl"" type=""string"" />
        <property name=""ChromeType"" type=""chrometype"">None</property>
        <property name=""AllowConnect"" type=""bool"">True</property>
        <property name=""Width"" type=""unit"" />
        <property name=""Height"" type=""unit"" />
        <property name=""HelpMode"" type=""helpmode"">Navigate</property>
        <property name=""AllowEdit"" type=""bool"">True</property>
        <property name=""TitleIconImageUrl"" type=""string"" />
        <property name=""Direction"" type=""direction"">NotSet</property>
        <property name=""AllowClose"" type=""bool"">True</property>
        <property name=""ChromeState"" type=""chromestate"">Normal</property>
      </properties>
    </data>
  </webPart>
</webParts>";

                    partTxt = partTxt.Replace(URL_REPLACE, SiteUrl);

                    Microsoft.SharePoint.Client.File webPage = ctx.Web.GetFileByServerRelativeUrl(ctx.Web.ServerRelativeUrl + ctx.Web.RootFolder.WelcomePage);

                    ctx.Load(webPage);
                    ctx.Load(webPage.ListItemAllFields);
                    ctx.ExecuteQuery();

                    string wikiField = (string)webPage.ListItemAllFields["WikiField"];

                    LimitedWebPartManager wpm = webPage.GetLimitedWebPartManager(Microsoft.SharePoint.Client.WebParts.PersonalizationScope.Shared);

                    //remove the OOB site feeds web part
                    WebPartDefinitionCollection allParts = wpm.WebParts;
                    ctx.Load(allParts);
                    ctx.ExecuteQuery();

                    for (int i = 0; i < allParts.Count; i++)
                    {
                        WebPart almostDeadPart = allParts[i].WebPart;
                        ctx.Load(almostDeadPart);
                        ctx.ExecuteQuery();

                        if (almostDeadPart.Title == "Site Feed")
                        {
                            allParts[i].DeleteWebPart();
                            ctx.ExecuteQuery();
                            break;
                        }
                    }

                    //continue on so we can add a new part
                    WebPartDefinition wpDef = wpm.ImportWebPart(partTxt);
                    WebPartDefinition wp = wpm.AddWebPart(wpDef.WebPart, "wpz", 1);

                    ctx.Load(wp);
                    ctx.ExecuteQuery();

                    #region AMS Helper Clean Up Code

                    XmlDocument xd = new XmlDocument();
                    xd.PreserveWhitespace = true;
                    xd.LoadXml(wikiField);

                    // Sometimes the wikifield content seems to be surrounded by an additional div? 
                    XmlElement layoutsTable = xd.SelectSingleNode("div/div/table") as XmlElement;
                    if (layoutsTable == null)
                    {
                        layoutsTable = xd.SelectSingleNode("div/table") as XmlElement;
                    }

                    XmlElement layoutsZoneInner = layoutsTable.SelectSingleNode(string.Format("tbody/tr[{0}]/td[{1}]/div/div", 1, 1)) as XmlElement;
                    // - space element
                    XmlElement space = xd.CreateElement("p");
                    XmlText text = xd.CreateTextNode(" ");
                    space.AppendChild(text);

                    // - wpBoxDiv
                    XmlElement wpBoxDiv = xd.CreateElement("div");
                    layoutsZoneInner.AppendChild(wpBoxDiv);

                    XmlAttribute attribute = xd.CreateAttribute("class");
                    wpBoxDiv.Attributes.Append(attribute);
                    attribute.Value = "ms-rtestate-read ms-rte-wpbox";
                    attribute = xd.CreateAttribute("contentEditable");
                    wpBoxDiv.Attributes.Append(attribute);
                    attribute.Value = "false";
                    // - div1
                    XmlElement div1 = xd.CreateElement("div");
                    wpBoxDiv.AppendChild(div1);
                    div1.IsEmpty = false;
                    attribute = xd.CreateAttribute("class");
                    div1.Attributes.Append(attribute);
                    attribute.Value = "ms-rtestate-read " + wp.Id.ToString("D");
                    attribute = xd.CreateAttribute("id");
                    div1.Attributes.Append(attribute);
                    attribute.Value = "div_" + wp.Id.ToString("D");
                    // - div2
                    XmlElement div2 = xd.CreateElement("div");
                    wpBoxDiv.AppendChild(div2);
                    div2.IsEmpty = false;
                    attribute = xd.CreateAttribute("style");
                    div2.Attributes.Append(attribute);
                    attribute.Value = "display:none";
                    attribute = xd.CreateAttribute("id");
                    div2.Attributes.Append(attribute);
                    attribute.Value = "vid_" + wp.Id.ToString("D");

                    Microsoft.SharePoint.Client.ListItem listItem = webPage.ListItemAllFields;
                    listItem["WikiField"] = xd.OuterXml;
                    listItem.Update();
                    ctx.ExecuteQuery();

                    #endregion
                }
            }
            catch (Exception)
            {              
                throw;
            }

            return result;
        }


        private void UpdateYammerWithTwitterContent()
        {
            try
            {
                //get the collection of twitter tags and Yammer object IDs
                using (SqlConnection cn = new SqlConnection(conStr))
                {
                    SqlCommand cm = new SqlCommand("getAllEvents", cn);
                    cm.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter da = new SqlDataAdapter(cm);

                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    if (
                        (ds != null) &&
                        (ds.Tables.Count > 0) &&
                            (ds.Tables[0].Rows.Count > 0)
                        )
                    {
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            //pull out the info for each event
                            string objectGraphID = ((double)dr["ObjectGraphID"]).ToString();
                            string siteUrl = (string)dr["ObjectGraphUrl"];
                            string tags = (string)dr["TwitterTags"];

                            //put the tags into an array
                            string[] ttags = tags.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                            foreach (string tag in ttags)
                            {
                                string query = tag;

                                //if it doesn't begin with a hashtag, then add it
                                //can't change "tag" because its the iterateor
                                if (query.Substring(0, 1) != "#")
                                    query = "#" + query;

                                //get the search results
                                SearchResults queryResults = GetTwitterSearchResults(query);

                                //enumerate through matches
                                foreach (SearchResult sr in queryResults.Results)
                                {
                                    #region Html Version of Post, not used here
                                    //create the string we'll post to the Yammer groups
                                    //string newPost = "From Twitter: <img src=\"" +
                                    //    sr.User.PictureUrl + "\" style=\"margin-right: 10px;\" />" +
                                    //    "<b>" + sr.User.FromUser + " says</b> - " + sr.Title + "<p>Click here to see " +
                                    //    "more from <a href=\"https://twitter.com/" + sr.User.FromUser + "\">" +
                                    //    sr.User.FromUser + "</a></p>Found: " +
                                    //    DateTime.Now.ToShortDateString();
                                    #endregion

                                    string newPost = "From Twitter: " +
                                        sr.User.FromUser + " says - " + sr.Title + ".  See the post and more at " +
                                        "https://twitter.com/" + sr.User.FromUser + ".  Found on " +
                                        DateTime.Now.ToShortDateString();

                                    CreateOpenGraphPost(objectGraphID, newPost);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        private SearchResults GetTwitterSearchResults(string query)
        {
            SearchResults results = new SearchResults();
            string response = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(TWT_ACCESS_TOKEN))
                {
                    //create the authorization key
                    string appKey = Convert.ToBase64String(
                        System.Text.UTF8Encoding.UTF8.GetBytes((HttpUtility.UrlEncode(TWT_CONSUMER_KEY) + ":" + HttpUtility.UrlEncode(TWT_CONSUMER_SECRET)))
                        );

                    //set the other data for our post
                    string contentType = "application/x-www-form-urlencoded;charset=UTF-8";
                    string postData = "grant_type=client_credentials";

                    //need to get the oauth token first
                    response = MakePostRequest(postData, TWT_OAUTH_URL, null, contentType, appKey);

                    //serialize it into our class
                    TwitterAccessToken accessToken = TwitterAccessToken.GetInstanceFromJson(response);

                    //plug the value into our local AccessToken variable
                    TWT_ACCESS_TOKEN = accessToken.AccessToken;
                }

                if (!string.IsNullOrEmpty(TWT_ACCESS_TOKEN))
                {
                    //now that we have our token we can go search for tweets
                    response = MakeGetRequest(TWT_SEARCH_URL + HttpUtility.UrlEncode(query), TWT_ACCESS_TOKEN);

                    //plug the data back into our return value
                    results = SearchResults.GetInstanceFromJson(response);

                    //trim out any tweets older than one day, which is how frequently
                    //this task should get invoked
                    if (results.Results.Count > 0)
                    {
                        //retrieve items added in the last 24 hours
                        var newResults = from SearchResult oneResult in results.Results
                                         where DateTime.Now.AddDays(-1) <
                                         DateTime.Parse(oneResult.Published)
                                         select oneResult;

                        results.Results = newResults.ToList<SearchResult>();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return results;
        }

        private static string GetAuthenticityToken(string rawHtml)
        {
            string result = string.Empty;

            try
            {
                int at = rawHtml.IndexOf("<meta name=\"authenticity_token\" id=\"authenticity_token\"");

                if (at > -1)
                {
                    //get the authenticity token string
                    int et = rawHtml.IndexOf("/>", at + 1);
                    string tokenText = rawHtml.Substring(at, et - at);

                    //get the token value
                    int ts = tokenText.IndexOf("content=");
                    int es = tokenText.LastIndexOf("\"");

                    result = tokenText.Substring(ts + 9, es - ts - 9);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetAuthenticityToken: " + ex.Message);
            }

            return result;
        }

        private void SetCookies()
        {
            try
            {
                const string YAMTRAK_COOKIE = "yamtrak_id";
                const string SESSION_COOKIE = "_workfeed_session_id";

                //no normalization to Set-Cookie content and Cookies on WebRequest is not populated so 
                //we are doing guesstimating parsing
                //Set-Cookie: yamtrak_id=2f1621f7-7452-4f7e-a974-6a85eb5ca22d; path=/; expires=Fri, 26-Sep-2014 15:20:54 GMT; secure; HttpOnly,_workfeed_session_id=34a53fdeab7da22fc4ae088fb19a2307; path=/; secure; HttpOnly
                string cookies = wResp.Headers["Set-Cookie"];

                if (string.IsNullOrEmpty(cookies))
                {
                    cc = new CookieContainer();
                }
                else
                {
                    int cStart = cookies.IndexOf("=");
                    int cEnd = cookies.IndexOf("HttpOnly,");

                    //sometimes the cookie ends with "HttpOnly," and sometimes it ends with "secure"
                    if (
                        (cookies.Substring(cStart + 1, cEnd + 8 - cStart - 1).IndexOf(YAMTRAK_COOKIE) > -1) ||
                         (cookies.Substring(cStart + 1, cEnd + 8 - cStart - 1).IndexOf(SESSION_COOKIE) > -1)
                        )
                    {
                        //change the end to look for secure
                        cEnd = cookies.IndexOf("secure,");
                    }

                    string tempCook1 = cookies.Substring(cStart + 1, cEnd + 8 - cStart - 1);
                    tempCook1 = tempCook1.Remove(tempCook1.IndexOf(";"));

                    cStart = cookies.IndexOf("=", cEnd);
                    string tempCook2 = cookies.Substring(cStart + 1);
                    tempCook2 = tempCook2.Remove(tempCook2.IndexOf(";"));

                    if (cookies.StartsWith("yamtrak"))
                    {
                        yamtrackCookie = tempCook1;
                        sessionCookie = tempCook2;
                    }
                    else
                    {
                        sessionCookie = tempCook1;
                        yamtrackCookie = tempCook2;
                    }

                    cc = new CookieContainer();
                    cc.Add(new Cookie(YAMTRAK_COOKIE, yamtrackCookie, "/", "www.yammer.com"));
                    cc.Add(new Cookie(SESSION_COOKIE, sessionCookie, "/", "www.yammer.com"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in SetCookies: " + ex.Message);
            }
        }

        private string MakePostRequest(string postBody, string url, string authHeader = null, string contentType = null,
            string twitterAuth = null)
        {
            string results = string.Empty;

            try
            {
                //get the session and yamtrack cookie
                SetCookies();

                wr = WebRequest.CreateHttp(url);
                wr.Method = "POST";
                wr.CookieContainer = cc;

                //if an authHeader was provided, add it as a Bearer token to the request
                if (!string.IsNullOrEmpty(authHeader))
                    wr.Headers.Add("Authorization", "Bearer " + authHeader);

                //if Twitter auth header was included, add it to the request
                if (!string.IsNullOrEmpty(twitterAuth))
                    wr.Headers.Add("Authorization", "Basic " + twitterAuth);

                byte[] postByte = Encoding.UTF8.GetBytes(postBody);

                if (string.IsNullOrEmpty(contentType))
                    wr.ContentType = "application/x-www-form-urlencoded";
                else
                    wr.ContentType = contentType;

                wr.ContentLength = postByte.Length;
                Stream postStream = wr.GetRequestStream();
                postStream.Write(postByte, 0, postByte.Length);
                postStream.Close();

                wResp = (HttpWebResponse)wr.GetResponse();
                postStream = wResp.GetResponseStream();
                StreamReader postReader = new StreamReader(postStream);

                results = postReader.ReadToEnd();

                postReader.Close();
                postStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MakePostRequest: " + ex.Message);
            }

            return results;
        }

        private string MakeGetRequest(string Url, string authHeader = null)
        {
            string results = string.Empty;

            try
            {
                wr = WebRequest.CreateHttp(Url);
                wr.Method = "GET";

                if (!string.IsNullOrEmpty(authHeader))
                    wr.Headers.Add("Authorization", "Bearer " + authHeader);

                wResp = (HttpWebResponse)wr.GetResponse();

                Stream dataStream = wResp.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);

                results = reader.ReadToEnd();

                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MakeGetRequest: " + ex.Message);
            }

            return results;
        }

        //you would probably want to pass in the Yammer user info as well, or else use a 
        //service account for this
        private YammerGraphObjectItem GetYammerGraphItem(string Url)
        {
            YammerGraphObjectItem gi = null;

            string response = string.Empty;

            try
            {
                //we use two different methods to look for the OG item because if there aren't any messages with 
                //the OG item, then it won't be returned in search results.  In that case we have to perform some action 
                //on the OG item, which will use Url as it's key, find the existing item, and return it's ID in response
                response = MakeGetRequest(searchUrl + "?search=" + Url + "&match=any-exact", yammerAccessToken);
                YammerSearchResults ysr = YammerSearchResults.GetInstanceFromJson(response);

                //look for the item in the results
                if ((ysr != null) && (ysr.Messages.Messages.Count > 0))
                {
                    var msgs = from YammerMessage ym in ysr.Messages.Messages
                               where ym.Attachments.Count > 0 &&
                               ym.Attachments[0].WebUrl == Url
                               select ym;

                    List<YammerMessage> yMsgs = msgs.ToList<YammerMessage>();

                    if (yMsgs.Count > 0)
                    {
                        gi = new YammerGraphObjectItem();
                        gi.object_id = yMsgs[0].Attachments[0].RecordID;
                        gi.InlineHtml = yMsgs[0].Attachments[0].InlineHtml;
                    }
                }

                //if we didn't find the OG item via search, then just follow it privately again, which 
                //gives you an object ID reference in return
                if (gi == null)
                {
                    //create a graph object with the minimum number of properties necessary to get back the OG ID
                    YammerGraphObject go = new YammerGraphObject();
                    go.Activity.Action = "follow";
                    go.Activity.Actor = new YammerActor("Steve Peschka", "speschka@yammo.onmicrosoft.com");
                    go.Activity.Private = true;

                    YammerGraphObjectInstance jo = new YammerGraphObjectInstance();
                    jo.Url = Url;

                    go.Activity.Object = jo;
                    string postData = go.ToString();
                    response = MakePostRequest(postData, graphPostUrl, yammerAccessToken, "application/json");

                    //serialize the results into an object with the OG ID
                    gi = JsonConvert.DeserializeObject<YammerGraphObjectItem>(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetYammerGraphItem: " + ex.Message);
            }

            return gi;
        }

    }
}