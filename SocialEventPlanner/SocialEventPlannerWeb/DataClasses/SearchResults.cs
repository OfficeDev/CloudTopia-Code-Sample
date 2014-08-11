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
    public class TwitterUser
    {
        [DataMember(Name = "screen_name", Order = 1)]
        public string FromUser { get; set; }

        [DataMember(Name = "profile_image_url", Order = 2)]
        public string PictureUrl { get; set; }
    }

    [DataContract]
    public class SearchResult
    {
        [DataMember(Name = "text", Order = 0)]
        public string Title { get; set; }

        [DataMember(Name = "id_str", Order = 2)]
        public string Id { get; set; }

        [DataMember(Name = "source", Order = 4)]
        public string Link { get; set; }

        private string mPublished = string.Empty;
        [DataMember(Name = "created_at", Order = 5)]
        public string Published
        {
            get
            {
                return mPublished;
            }
            set
            {
                //date format is like this:  Sun Jul 07 18:32:46 +0000 2013
                //which is not a valid .NET DateTime

                int s = value.IndexOf(" +");
                int e = value.LastIndexOf(" ");
                mPublished = value.Remove(s, e - s);

                //now remove the day descriptor
                mPublished = mPublished.Substring(mPublished.IndexOf(" ") + 1);

                //that get us down to this, with the time in the middle:  Jul 07 19:27:06 2013
                string yy = mPublished.Substring(mPublished.LastIndexOf(" ") + 1);
                mPublished = mPublished.Remove(mPublished.LastIndexOf(" "));

                //get the time
                string tt = mPublished.Substring(mPublished.LastIndexOf(" ") + 1);
                mPublished = mPublished.Remove(mPublished.LastIndexOf(" "));

                //reassemble
                mPublished = mPublished + " " + yy + " " + tt;
            }
        }

        [DataMember(Name = "user", Order = 6)]
        public TwitterUser User { get; set; }
    }

    [DataContract]
    public class SearchResults : SerializedJson<SearchResults>
    {
        public SearchResults()
        {
            this.Results = new List<SearchResult>();
        }

        [DataMember(Name = "statuses")]
        public List<SearchResult> Results { get; set; }
    }

}