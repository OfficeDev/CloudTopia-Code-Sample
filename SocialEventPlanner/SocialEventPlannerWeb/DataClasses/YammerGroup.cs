using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Diagnostics;


namespace SocialEventPlannerWeb.DataClasses
{
    [DataContract]
    public class YammerGroup : SerializedJson<YammerGroup>
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "created_at")]
        public string CreatedAt { get; set; }

        [DataMember(Name = "creator_id")]
        public string CreatedByID { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "full_name")]
        public string FullName { get; set; }

        [DataMember(Name = "mugshot_id")]
        public string MugshotID { get; set; }

        [DataMember(Name = "mugshot_url")]
        public string MugshotUrl { get; set; }

        [DataMember(Name = "mugshot_url_template")]
        public string MugshotUrlTemplate { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "office365_url")]
        public string Office365Url { get; set; }

        [DataMember(Name = "privacy")]
        public string PrivacyLevel { get; set; }

        [DataMember(Name = "show_in_directory")]
        public bool ShowInDirectory { get; set; }

        [DataMember(Name = "state")]
        public string Status { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "web_url")]
        public string WebUrl { get; set; }

        [DataMember(Name = "stats")]
        public YammerGroupStats GroupStats { get; set; }

        public YammerGroup()
        {
            this.GroupStats = new YammerGroupStats();
        }
    }

    public class YammerGroupStats
    {
        [DataMember(Name = "last_message_at")]
        public string LastMessageAt { get; set; }

        [DataMember(Name = "last_message_id")]
        public string LastMessageID { get; set; }

        [DataMember(Name = "members")]
        public int Members { get; set; }

        [DataMember(Name = "updates")]
        public int Updates { get; set; }
    }
}