using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.Diagnostics;


namespace SocialEventPlannerWeb.DataClasses
{
    [DataContract]
    public class SocialEvent : SerializedJson<SocialEvent>
    {
        [DataMember(Name="eventName")]
        public string eventName { get; set; }

        [DataMember(Name = "eventDate")]
        public string eventDate { get; set; }

        [DataMember(Name = "twitterTags")]
        public string twitterTags { get; set; }

        [DataMember(Name = "hostUrl")]
        public string hostUrl { get; set; }

        [DataMember(Name = "accessToken")]
        public string accessToken { get; set; }

        [DataMember(Name = "errorMessage")]
        public string errorMessage { get; set; }

        [DataMember(Name = "newSiteUrl")]
        public string newSiteUrl { get; set; }

        [DataMember(Name = "rawPostData")]
        public string rawPostData { get; set; }

        [DataMember(Name = "objectGraphID")]
        public string objectGraphID { get; set; }

        [DataMember(Name = "success")]
        public bool success { get; set; }

        public SocialEvent() { }

        public SocialEvent(string eventName, string eventDate, string twitterTags, string objectGraphID, string siteUrl)
        {
            this.eventName = eventName;
            this.eventDate = eventDate;
            this.twitterTags = twitterTags;
            this.objectGraphID = objectGraphID;
            this.newSiteUrl = siteUrl;
        }



        public static SocialEvent ParseJson(string value)
        {
            SocialEvent se = new SocialEvent();

            //this is an example of the text:
            //{eventName:"BigBash",
            //eventDate:"12/13/2014",
            //twitterTags:"speschka",
            //hostUrl:"https://yammo.sharepoint.com/sites/events",
            //accessToken:"ZXlKMGVYQWlPaUpLVjFRaUxDSmhiR2NpT2lKU1V6STFOaUlzSW5nMWRDSTZJbXR5YVUxUVpH
            //MUNkbmcyT0hOclZEZ3RiVkJCUWpOQ2MyVmxRU0o5LmV5SmhkV1FpT2lJd01EQXdNREF3TXkwd01EQXdMVEJtWmpF
            //dFkyVXdNQzB3TURBd01EQXdNREF3TURBdmVXRnRiVzh1YzJoaGNtVndiMmx1ZEM1amIyMUFZems0WmpReFlUQXRaVGxr
            //TnkwMFpETXhMVGxoWXpRdE9ERXpZamd4Tmpnd01UYzNJaXdpYVhOeklqb2lNREF3TURBd01ERXRNREF3TUMwd01EQXdMV013
            //TURBdE1EQXdNREF3TURBd01EQXdRR001T0dZME1XRXdMV1U1WkRjdE5HUXpNUzA1WVdNMExUZ3hNMkk0TVRZNE1ERTNOeUlz
            //SW01aVppSTZNVFF3TURBNU1EZ3pNQ3dpWlhod0lqb3hOREF3TVRNME1ETXdMQ0p1WVcxbGFXUWlPaUl4TURBek0yWm1aamcy
            //WTJObE1EazRJaXdpWVdOMGIzSWlPaUppTW1Gak16VTBOeTFqTVRGbExUUTJPREl0WVdFeU9TMW1NbUV4Tm1NMllqZzFZV0ZBWX
            //prNFpqUXhZVEF0WlRsa055MDBaRE14TFRsaFl6UXRPREV6WWpneE5qZ3dNVGMzSWl3aWFXUmxiblJwZEhsd2NtOTJhV1JsY2lJ
            //NkluVnlianBtWldSbGNtRjBhVzl1T20xcFkzSnZjMjltZEc5dWJHbHVaU0o5Lk9Mczh6UGFsMXBmMVBlNS1yZzBobG9nTHB1SU
            //lPTlFqazBxaUh4REltUERyekdRa29Sdy1CWHNOTnRTeGtwMlpCNlpraTFKc1RndGZBbm05VC05WjNmNjVOTml0THNXT0dQSkp1
            //OXlFNXVrdEVmTW9PNHZ6M2UxQk55Znpxc2RjR1c2VGNhOEhqc2kwQ05CUW1iaGx0Q1Rab01zbHdRN3lIVDc4V0Q5U0xwd3ZtTUJ
            //rcjEydzhveUJMLVNva2daTUYyMUt4eG9yYlI5RHVwalNqUThHaDZ5OXViNmlKZzVPckRkU01mOUMwWjBWb242bkprNkFVcXBtV2JWNX
            //dSbGZodGF0aGRNNk1DYWdUSVBOd1hpRXd6YzFhbU5wZXdaWktLamlmX181MTQ3XzNwbE9nc1VCR0labXAzVlo0RzM2UGNENHFq
            //YWZ6WXRUX3UtQUVMcTM3UQ=="}

            try
            {
                //begin by trimming out the curly brackets
                string txt = TrimEnds(value);
                txt = TrimEnds(txt);

                //split the rest into an array
                string[] values = txt.Split(",".ToCharArray());

                //get the type and use it to set properties on the class instance
                var seType = typeof(SocialEvent);

                //enumerate through each one
                foreach (string v in values)
                {
                    //split the key and value
                    string[] kv = v.Split(":".ToCharArray());

                    try
                    {
                        string newValue = kv[1];

                        if (kv[0] == "hostUrl")
                            newValue = System.Web.HttpUtility.UrlDecode(kv[1]);

                        seType.GetProperty(kv[0]).
                            SetValue(se, newValue);
                    }
                    catch (Exception propEx)
                    {
                        Debug.WriteLine(propEx.Message);
                    }

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return se;
        }


        public static string TrimEnds(string value)
        {
            string result = value;

            try
            {
                result = result.Substring(1);
                result = result.Remove(result.Length - 1, 1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return result;
        }
    }
}