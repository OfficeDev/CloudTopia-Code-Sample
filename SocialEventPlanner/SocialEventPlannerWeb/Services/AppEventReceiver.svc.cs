using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using System.Diagnostics;


namespace SocialEventPlannerWeb.Services
{
    public class AppEventReceiver : IRemoteEventService
    {
        private const string LIST_NAME = "AF785492-48DC-4E52-BAFB-94BEEA3E4F6E";


        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            SPRemoteEventResult result = new SPRemoteEventResult();

            //get the host web url
            Uri hostUrl = properties.AppEventProperties.HostWebFullUrl;

            //get the operation context so we can figure out the host URL for this service, from 
            //which we can get the Authority for a client context
            System.ServiceModel.OperationContext oc = System.ServiceModel.OperationContext.Current;

            Uri localUrl = null;

            //UPDATE:  THIS CODE WORKED FINE FOR WHEN YOU USE THE WEB APP CREATED BY VS.NET
            //FOR THE SHAREPOINT APP, BUT IT BREAKS WHEN YOU DEPLOY TO A REAL IIS SERVER
            //BECAUSE YOU END UP GETTING TWO BASEADDRESSES, BUT NOT WITH THE RIGHT SCHEME.
            //FOR EXAMPLE, THE FIRST ONE IS THE ADDRESS OF THIS HOST BUT WITH THE HTTP SCHEME.
            //THE SECOND ONE IS HTTPS, BUT THE HOST NAME IS THE FQDN OF THE SERVER.  SINCE 
            //SHAREPOINT DOESN'T RECOGNIZE THAT AS THE ENDPOINT IT TRUSTS FOR THIS CODE, IT BLOWS
            //UP WHEN RUNNING THE CODE BELOW AND THE WHOLE THING FAILS

            #region Code That Breaks In IIS
            ////now enumerate through the Host base addresses and look for the SSL connection
            //foreach (Uri u in oc.Host.BaseAddresses)
            //{
            //    if (u.Scheme.ToLower() == "https")
            //    {
            //        localUrl = u;
            //        break;
            //    }
            //}
            #endregion

            //assume first base address is ours, which it has been so far
            if (oc.Host.BaseAddresses.Count > 0)
                localUrl = oc.Host.BaseAddresses[0];

            //make sure we found our local URL
            if (localUrl != null)
            {
                //using (ClientContext ctx = TokenHelper.CreateAppEventClientContext(properties, false))
                using (ClientContext ctx = TokenHelper.GetClientContextWithContextToken(hostUrl.ToString(), properties.ContextToken, localUrl.Authority))
                {
                    if (ctx != null)
                    {
                        //try to retrieve the list first to see if it exists
                        List l = ctx.Web.Lists.GetByTitle(LIST_NAME);
                        ctx.Load(l);

                        //have to put in a try block because of course it throw an exception if 
                        //list doesn't exist
                        try
                        {
                            ctx.ExecuteQuery();
                        }
                        catch (Exception noListEx)
                        {
                            //look to see if the exception is that the list doesn't exist
                            if (noListEx.Message.ToLower().Contains("does not exist"))
                            {
                                //code here to create list
                                Web web = ctx.Web;

                                ListCreationInformation ci = new ListCreationInformation();
                                ci.Title = LIST_NAME;
                                ci.TemplateType = (int)ListTemplateType.GenericList;
                                ci.QuickLaunchOption = QuickLaunchOptions.Off;

                                l = web.Lists.Add(ci);
                                l.Description = "List for tracking events with the Event Planner Social App";

                                Field fldEventName = l.Fields.AddFieldAsXml("<Field DisplayName='EventName' Type='Text' />", true, AddFieldOptions.DefaultValue);
                                Field fldSiteUrl = l.Fields.AddFieldAsXml("<Field DisplayName='SiteUrl' Type='Text' />", true, AddFieldOptions.DefaultValue);
                                Field fldTwitterTags = l.Fields.AddFieldAsXml("<Field DisplayName='TwitterTags' Type='Text' />", true, AddFieldOptions.DefaultValue);

                                Field fldEventDate = l.Fields.AddFieldAsXml("<Field DisplayName='EventDate' Type='DateTime' />", true, AddFieldOptions.DefaultValue);
                                FieldDateTime dtEventDate = ctx.CastTo<FieldDateTime>(fldEventDate);
                                dtEventDate.DisplayFormat = DateTimeFieldFormatType.DateOnly;
                                dtEventDate.Update();

                                Field fldGraphID = l.Fields.AddFieldAsXml("<Field DisplayName='ObjectGraphID' Type='Text' />", true, AddFieldOptions.DefaultValue);
                                FieldText txtGraphID = ctx.CastTo<FieldText>(fldGraphID);
                                txtGraphID.Indexed = true;
                                txtGraphID.Update();

                                l.Hidden = true;
                                l.Update();

                                try
                                {
                                    //this creates the list
                                    ctx.ExecuteQuery();

                                    //all of the rest of this is to remove the list from the "Recent" list that appears
                                    //in sites by default, which is really a set of navigation links

                                    //get the site and root web, where the navigation lives
                                    Site s = ctx.Site;
                                    Web rw = s.RootWeb;

                                    //get the QuickLaunch navigation, which is where the Recent nav lives
                                    ctx.Load(rw, x => x.Navigation, x => x.Navigation.QuickLaunch);
                                    ctx.ExecuteQuery();

                                    //now extract the Recent navigation node from the collection
                                    var vNode = from NavigationNode nn in rw.Navigation.QuickLaunch
                                                where nn.Title == "Recent"
                                                select nn;

                                    NavigationNode nNode = vNode.First<NavigationNode>();

                                    //now we need to get the child nodes of Recent, that's where our list should be found
                                    ctx.Load(nNode.Children);
                                    ctx.ExecuteQuery();

                                    var vcNode = from NavigationNode cn in nNode.Children
                                                 where cn.Title == LIST_NAME
                                                 select cn;

                                    //now that we have the node representing our list, delete it
                                    NavigationNode cNode = vcNode.First<NavigationNode>();
                                    cNode.DeleteObject();

                                    ctx.ExecuteQuery();
                                }
                                catch (Exception newListFailEx)
                                {
                                    Debug.WriteLine("Creation of new list failed: " + newListFailEx.Message);
                                }
                            }
                        }

                        //okay, so if we're here then the list should exist, and we should be good to go at this point
                    }
                }
            }


            #region OOB Template Code
            //using (ClientContext clientContext = TokenHelper.CreateAppEventClientContext(properties, false))
            //{
            //    if (clientContext != null)
            //    {
            //        clientContext.Load(clientContext.Web);
            //        clientContext.ExecuteQuery();
            //    }
            //}
            #endregion

            return result;
        }

        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            // This method is not used by app events
        }
    }
}
