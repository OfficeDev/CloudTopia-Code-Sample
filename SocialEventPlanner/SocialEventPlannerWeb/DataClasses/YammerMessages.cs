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

        [DataMember(Name = "attachments")]
        public List<YammerAttachment> Attachments { get; set; }

        [DataMember(Name = "liked_by")]
        public YammerLikes Likes { get; set; }

        public YammerMessage()
        {
            this.Attachments = new List<YammerAttachment>();
            this.Likes = new YammerLikes();
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

    [DataContract]
    public class YammerAttachment
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "record_id")]
        public string RecordID { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "web_url")]
        public string WebUrl { get; set; }

        [DataMember(Name = "inline_url")]
        public string InlineUrl { get; set; }

        [DataMember(Name = "inline_html")]
        public string InlineHtml { get; set; }

        [DataMember(Name = "liked_by")]
        public YammerLikes Likes { get; set; }

        public YammerAttachment()
        {
            this.Likes = new YammerLikes();
        }
    }

    [DataContract]
    public class YammerLikes
    {
        [DataMember(Name = "count")]
        public int Count { get; set; }

        [DataMember(Name = "names")]
        public List<YammerLikeUser> Names { get; set; }

        public YammerLikes()
        {
            this.Names = new List<YammerLikeUser>();
        }
    }

    [DataContract]
    public class YammerLikeUser
    {
        [DataMember(Name = "full_name")]
        public string FullName { get; set; }

        [DataMember(Name = "permalink")]
        public string PermaLink { get; set; }

        [DataMember(Name = "user_id")]
        public string UserID { get; set; }
    }

    [DataContract]
    public class YammerMessagesMetadata
    {
        [DataMember(Name = "older_available")]
        public string OlderAvailable { get; set; }

        [DataMember(Name = "requested_poll_interval")]
        public string RequestedPollInterval { get; set; }

        [DataMember(Name = "realtime")]
        public YammerRealTimeInfo RealtimeInfo { get; set; }

        [DataMember(Name = "last_seen_message_id")]
        public string LastSeenMessageID { get; set; }

        [DataMember(Name = "current_user_id")]
        public string CurrentUserID { get; set; }

        [DataMember(Name = "liked_message_ids")]
        public List<string> LikedMessageIDs { get; set; }

        [DataMember(Name = "followed_user_ids")]
        public List<string> FollowedUserIDs { get; set; }

        [DataMember(Name = "followed_references")]
        public List<string> FollowedReferences { get; set; }

        [DataMember(Name = "ymodules")]
        public List<YammerYModules> YModules { get; set; }

        [DataMember(Name = "feed_name")]
        public string FeedName { get; set; }

        [DataMember(Name = "feed_desc")]
        public string FeedDescription { get; set; }

        [DataMember(Name = "direct_from_body")]
        public string DirectFromBody { get; set; }

        public YammerMessagesMetadata()
        {
            this.RealtimeInfo = new YammerRealTimeInfo();
            this.LikedMessageIDs = new List<string>();
            this.FollowedUserIDs = new List<string>();
            this.FollowedReferences = new List<string>();
            this.YModules = new List<YammerYModules>();
        }
    }

    [DataContract]
    public class YammerYModules
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "inline_html")]
        public string InlineHtml { get; set; }

        [DataMember(Name = "viewer_id")]
        public string ViewerID { get; set; }
    }

    [DataContract]
    public class YammerRealTimeInfo
    {
        [DataMember(Name = "uri")]
        public string Uri { get; set; }

        [DataMember(Name = "authentication_token")]
        public string AuthToken { get; set; }

        [DataMember(Name = "channel_id")]
        public string ChannelID { get; set; }
    }

    [DataContract]
    public class YammerMessagesReferences
    {
        [DataMember(Name = "activated_at")]
        public string ActivatedAt { get; set; }

        [DataMember(Name = "full_name")]
        public string FullName { get; set; }

        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "job_title")]
        public string JobTitle { get; set; }

        [DataMember(Name = "mugshot_url")]
        public string MugshotUrl { get; set; }

        [DataMember(Name = "mugshot_url_template")]
        public string MugshotUrlTemplate { get; set; }

        [DataMember(Name = "name")]
        public string UserName { get; set; }

        [DataMember(Name = "state")]
        public string ActivityState { get; set; }

        [DataMember(Name = "stats")]
        public YammerReferenceStats Stats { get; set; }

        [DataMember(Name = "type")]
        public string UserType { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "web_url")]
        public string WebUrl { get; set; }

        public YammerMessagesReferences()
        {
            this.Stats = new YammerReferenceStats();
        }
    }

    [DataContract]
    public class YammerReferenceStats
    {
        [DataMember(Name = "followers")]
        public string Followers { get; set; }

        [DataMember(Name = "following")]
        public string Following { get; set; }

        [DataMember(Name = "updates")]
        public string Updates { get; set; }
    }

    [DataContract]
    public class YammerGraphMessages : SerializedJson<YammerGraphMessages>
    {
        [DataMember(Name = "messages")]
        public List<YammerMessage> Messages { get; set; }

        //having these in here causes the serialization to fail 
        //when getting messages from GraphObjects
        //[DataMember(Name = "meta")]
        //public YammerMessagesMetadata Metadata { get; set; }

        //[DataMember(Name = "references")]
        //public List<YammerMessagesReferences> References { get; set; }

        public YammerGraphMessages()
        {
            this.Messages = new List<YammerMessage>();
        }
    }

    [DataContract]
    public class YammerMessages : SerializedJson<YammerMessages>
    {
        [DataMember(Name = "messages")]
        public List<YammerMessage> Messages { get; set; }

        [DataMember(Name = "meta")]
        public YammerMessagesMetadata Metadata { get; set; }

        [DataMember(Name = "references")]
        public List<YammerMessagesReferences> References { get; set; }

        public YammerMessages()
        {
            this.Messages = new List<YammerMessage>();
            this.Metadata = new YammerMessagesMetadata();
            this.References = new List<YammerMessagesReferences>();
        }
    }
}