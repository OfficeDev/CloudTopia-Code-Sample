using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;


namespace SocialEventPlannerWeb.DataClasses
{
    [DataContract]
    public class YammerGraphObject
    {
        [DataMember(Name = "activity")]
        public YammerActivity Activity { get; set; }

        public YammerGraphObject()
        {
            this.Activity = new YammerActivity();
        }

        public override string ToString()
        {
            string jsonData = string.Empty;

            try
            {
                DataContractJsonSerializer ys = new DataContractJsonSerializer(typeof(YammerGraphObject));
                MemoryStream msBack = new MemoryStream();
                ys.WriteObject(msBack, this);
                msBack.Position = 0;
                StreamReader sr = new StreamReader(msBack);
                jsonData = sr.ReadToEnd();

                //now fix up the forward slash escaping
                jsonData = jsonData.Replace("\\/", "/");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error serializing into string: " + ex.Message);
            }

            return jsonData;
        }
    }

    [DataContract]
    public class YammerActivity
    {
        [DataMember(Name = "actor")]
        public YammerActor Actor { get; set; }

        [DataMember(Name = "action")]
        public string Action { get; set; }

        [DataMember(Name = "object")]
        public YammerGraphObjectInstance Object { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "private")]
        public bool Private { get; set; }

        [DataMember(Name = "users")]
        public List<YammerActor> Users { get; set; }

        public YammerActivity()
        {
            this.Actor = new YammerActor();
            this.Object = new YammerGraphObjectInstance();
            this.Users = new List<YammerActor>();
            this.Private = false;
        }
    }

    [DataContract]
    public class YammerActor
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "email")]
        public string Email { get; set; }

        public YammerActor() { }

        public YammerActor(string name, string email)
        {
            this.Name = name;
            this.Email = email;
        }
    }

    [DataContract]
    public class YammerGraphObjectInstance
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "image")]
        public string Image { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }
    }

    [DataContract]
    public class YammerGraphObjectItem
    {
        [DataMember(Name = "action_id")]
        public string action_id { get; set; }

        [DataMember(Name = "object_id")]
        public string object_id { get; set; }

        [DataMember(Name = "actor_id")]
        public string actor_id { get; set; }

        [DataMember(Name = "InlineHtml")]
        public string InlineHtml { get; set; }

        public YammerGraphObjectItem() { }
    }
}