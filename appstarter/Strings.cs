using System;
using System.Collections;
using System.Text;

namespace ws
{
    // at the moment we do not go via resource manager
    class Str
    {
        public static Str Def = new Str();
        private Hashtable data = new Hashtable();

        public Str() 
        {
            InitDefaults();
        }

        public string Get(string id) 
        {
            string s = (string)data[id];
            if (s == null) return string.Empty;
            return s;
        }

        public const string SPrefix = "str.";
        public const string Error = "error";
        //public const string RestartNeeded = "restart";
        public const string CannotClearCache = "failcache";
        public const string FailedCrc = "failcsum";
        public const string FailedLength = "faillen";
        public const string Connecting = "connecting";
        public const string NoSpace = "nospace";
        public const string NoNetPath = "nonetpath";
        public const string LicenseTitle = "lic.title";
        public const string LicenseOk = "lic.ok";
        public const string LicenseCancel = "lic.cancel";
        public const string RemoteError = "remotefail";
        public const string VerifyingFile = "verify";
        public const string ConfirmClose = "userclose";

        private void InitDefaults() 
        {
            data[Error] = "Error";
            //data[RestartNeeded] = "Done. New files will be used at next run!";
            data[CannotClearCache] = "Failed! Files still in use?";
            data[FailedCrc] = "Checksum failed ";
            data[FailedLength] = "Length check failed ";
            data[Connecting] = "...";
            data[NoSpace] = "Not enough free disk space";
            data[NoNetPath] = "Cannot start from local network!";
            data[LicenseTitle] = "License";
            data[LicenseOk] = "&Accept";
            data[LicenseCancel] = "&Decline";
            data[RemoteError] = "Deployment failure";
            data[VerifyingFile] = "Verifying ...";
            data[ConfirmClose] = "Work is in progress! Do you really want to close the application?";
        }
        
        public void Replace(string key, string val) 
        {
            string k = key.ToLower();
            if (k.Length <= SPrefix.Length) return;
            k = k.Substring(SPrefix.Length);
            string c = (string)data[k];
            if (c == null) return;
            data[k] = val;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            SortedList sl = new SortedList(data);
            foreach (DictionaryEntry de in sl) 
            {
                sb.Append(SPrefix + (string)de.Key).Append(": ").Append((string)de.Value).Append(Environment.NewLine);
            }
            return sb.ToString();
        }
    }//EOC
}
