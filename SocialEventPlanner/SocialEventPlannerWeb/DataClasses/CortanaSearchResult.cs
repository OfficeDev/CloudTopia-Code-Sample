using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SocialEventPlannerWeb.DataClasses
{
    [DataContract]
    public class CortanaSearchResult : SerializedJson<CortanaSearchResult>
    {
        [DataMember(Name = "messages")]
        public List<YammerMessage> YammerMessages { get; set; }

        [DataMember(Name = "events")]
        public List<SearchResultEventData> Events { get; set; }

        [DataMember(Name = "WasError")]
        public bool IsError { get; set; }

        [DataMember(Name = "ErrorMessage")]
        public string ErrorMessage { get; set; }

        public CortanaSearchResult()
        {
            this.YammerMessages = new List<YammerMessage>();
            this.Events = new List<SearchResultEventData>();
            this.IsError = false;
            this.ErrorMessage = string.Empty;
        }
    }

    [DataContract]
    public class SearchResultEventData
    {
        [DataMember(Name = "ObjectGraphID")]
        public string ObjectGraphID { get; set; }

        [DataMember(Name = "ObjectGraphUrl")]
        public string ObjectGraphUrl { get; set; }
        
        [DataMember(Name = "TwitterTags")]
        public string TwitterTags { get; set; }

        [DataMember(Name = "EventName")]
        public string EventName { get; set; }

        [DataMember(Name = "EventDate")]
        public string EventDate { get; set; }


        public SearchResultEventData() { }
    }
}