using System;
using System.Collections.Generic;
using System.Linq;
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
    public class YammerSearchResults : SerializedJson<YammerSearchResults>
    {
        [DataMember(Name = "count")]
        public YammerSearchResultTotals ResultTotals { get; set; }

        [DataMember(Name = "messages")]
        public YammerGraphMessages Messages { get; set; }

        [DataMember(Name = "groups")]
        public List<YammerGroup> Groups { get; set; }

        //[DataMember(Name = "references")]
        //public List<YammerMessagesReferences> References { get; set; }

        public YammerSearchResults()
        {
            this.Groups = new List<YammerGroup>();
            //this.References = new List<YammerMessagesReferences>();
        }

        [DataContract]
        public class YammerSearchResultTotals
        {
            [DataMember(Name = "messages")]
            public int Messages { get; set; }

            [DataMember(Name = "groups")]
            public int Groups { get; set; }

            [DataMember(Name = "topics")]
            public int Topics { get; set; }

            [DataMember(Name = "uploaded_files")]
            public int UploadedFiles { get; set; }

            [DataMember(Name = "users")]
            public int Users { get; set; }

            [DataMember(Name = "pages")]
            public int Pages { get; set; }

            [DataMember(Name = "events")]
            public int Events { get; set; }

            [DataMember(Name = "praises")]
            public int Praises { get; set; }
        }
    }
}