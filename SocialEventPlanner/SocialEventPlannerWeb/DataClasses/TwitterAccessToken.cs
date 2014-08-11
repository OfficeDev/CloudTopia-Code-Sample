using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Reflection;


namespace SocialEventPlannerWeb.DataClasses
{
    [DataContract]
    public class TwitterAccessToken : SerializedJson<TwitterAccessToken>
    {
        [DataMember(Name = "token_type")]
        public string TokenType { get; set; }

        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }
    }
}