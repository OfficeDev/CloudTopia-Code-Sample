using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Diagnostics;


namespace CortanaSocialEvents
{
    [DataContract]
    public class CortanaSearchResult 
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

        public static CortanaSearchResult GetInstanceFromJson(string data)
        {
            CortanaSearchResult returnDataType = new CortanaSearchResult();

            try
            {
                MemoryStream ms = new MemoryStream();
                byte[] buf = System.Text.UTF8Encoding.UTF8.GetBytes(data);
                ms.Write(buf, 0, Convert.ToInt32(buf.Length));
                ms.Position = 0;
                DataContractJsonSerializer s = new DataContractJsonSerializer(typeof(CortanaSearchResult));
                returnDataType = (CortanaSearchResult)s.ReadObject(ms);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting instance from JSON: " + ex.Message);
            }

            return returnDataType;
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

    [DataContract]
    public class YammerMessage
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "sender_id")]
        public string SenderID { get; set; }

        [DataMember(Name = "replied_to_id")]
        public string RepliedToID { get; set; }

        [DataMember(Name = "created_at")]
        public string CreatedAt { get; set; }

        [DataMember(Name = "network_id")]
        public string NetworkID { get; set; }

        [DataMember(Name = "message_type")]
        public string MessageType { get; set; }

        [DataMember(Name = "sender_type")]
        public string SenderType { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "web_url")]
        public string WebUrl { get; set; }

        [DataMember(Name = "body")]
        public YammerMessageContent MessageContent { get; set; }

        [DataMember(Name = "thread_id")]
        public string ThreadID { get; set; }

        [DataMember(Name = "client_type")]
        public string ClientType { get; set; }

        [DataMember(Name = "client_url")]
        public string ClientUrl { get; set; }

        [DataMember(Name = "system_message")]
        public bool SystemMessage { get; set; }

        [DataMember(Name = "direct_message")]
        public bool DirectMessage { get; set; }

        [DataMember(Name = "chat_client_sequence")]
        public string ChatClientSequence { get; set; }

        [DataMember(Name = "content_excerpt")]
        public string ContentExcerpt { get; set; }

        [DataMember(Name = "language")]
        public string Language { get; set; }

        public YammerMessage()
        {
            this.MessageContent = new YammerMessageContent();
        }
    }

    [DataContract]
    public class YammerMessageContent
    {
        [DataMember(Name = "parsed")]
        public string ParsedText { get; set; }

        [DataMember(Name = "plain")]
        public string PlainText { get; set; }

        [DataMember(Name = "rich")]
        public string RichText { get; set; }
    }


}