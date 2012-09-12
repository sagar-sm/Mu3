using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace Mu3
{
    static class Security
    {
        public static PasswordVault _vault = new PasswordVault();

        private static string _lfm_sessionkey = "";
        public static string LfmSessionKey
        {
            get { return _lfm_sessionkey; }
            set { _lfm_sessionkey = value; }
        }

        private static string _twitter_secret = "";
        public static string TwitterConsumerSecret
        {
            get { return _twitter_secret; }
            set { _twitter_secret = value; }
        }

        private static string _twitter_access = "";
        public static string TwitterAccessToken
        {
            get { return _twitter_access; }
            set { _twitter_access = value; }
        }


        /*public void Add(string username, string password)
        {
            var c = new PasswordCredential("MyAppResourceName", username, password);
            _vault.Add(c);
        }*/


    }
}
