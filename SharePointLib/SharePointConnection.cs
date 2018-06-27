using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace org.commitworld.web.business.sharepoint
{
    public class SharePointConnectionParams
    {
        public string SiteRoot { get; set; }
        public string SitePath { get; set; }
        public NetworkCredential Credential { get; set; }
    }

    public class ParamsBuilder
    {
        private string siteRoot;
        private string sitePath;
        private bool useCredentials;
        private string username;
        private string pwd;
        private string userDomain;
        public ParamsBuilder SiteURL(string root)
        {
            siteRoot = root;
            return this;
        }
        public ParamsBuilder SitePath(string path)
        {
            sitePath = path;
            return this;
        }
        public ParamsBuilder UseCredentials(bool flag)
        {
            useCredentials = flag;
            return this;
        }
        public ParamsBuilder Credentials(string user, string passwd, string domain)
        {
            username = user;
            pwd = passwd;
            userDomain = domain;
            return this;
        }
        public SharePointConnectionParams Build()
        {
            return new SharePointConnectionParams()
            {
                SiteRoot = siteRoot,
                SitePath = sitePath,
                Credential = useCredentials ? new NetworkCredential(username, pwd, userDomain) : null
            };
        }
    }
}
