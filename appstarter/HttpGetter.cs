using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Cache;
using System.Reflection;

namespace ws
{
    class HttpGetter : IDisposable
    {
        private HttpWebResponse response = null;
        private Stream stream = null;
        private RemoteFile rm = null;

        ~HttpGetter() { Dispose(); }

        public static HttpWebRequest SetRequestDefaults(HttpWebRequest request)
        {
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.AllowAutoRedirect = true;
            request.CachePolicy = noCachePolicy;
            request.UserAgent = Config.Default.UserAgent;
            //request.KeepAlive = false;
            return request;
        }

        public void Init(RemoteFile rm)
        {
            if (this.rm != null) throw new Exception(Str.Def.Get(Str.RemoteError));
            this.rm = rm;
        }

        public static string GetTempFilePath(string outFile) 
        {
            return outFile + ".part";
        }

        public void Dump(string outFile, Monitor m, string msg)
        {
            string tempFile = HttpGetter.GetTempFilePath(outFile);
            Stream outs = null;
            try
            {
                if (File.Exists(tempFile))
                {
                    outs = new FileStream(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 1024 * 1024 * 5);
                    outs.Seek(0, SeekOrigin.End);
                }
                else
                {
                    outs = Utils.OpenWrite(tempFile);
                }

                
                if (outs.Position > 0)
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(rm.url);
                        request = HttpGetter.SetRequestDefaults(request);
                        SetRange(request, outs.Position);
                        response = (HttpWebResponse)request.GetResponse();
                        if (m.ShouldStop()) { return; }
                    }
                    catch (System.Net.WebException ex)
                    {
                        if (response != null) response.Close();
                        response = null;
                        if (ex.Message.IndexOf("416") > 0)
                        {
                            if (outs != null)
                            {
                                outs.Close();
                                outs = null;
                            }
                            Utils.DeleteFile(outFile);
                            Utils.DeleteFile(tempFile);
                            outs = Utils.OpenWrite(tempFile);
                        }
                        else { throw ex; }
                    }
                }
                if (response == null) 
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(rm.url);
                    request = HttpGetter.SetRequestDefaults(request);
                    response = (HttpWebResponse)request.GetResponse();
                    if (m.ShouldStop()) { return; }
                }
                if (response.StatusCode != HttpStatusCode.PartialContent)
                {
                    outs.Seek(0, SeekOrigin.Begin);
                    outs.SetLength(0);
                }
                else 
                {
                    RawLog.Default.Log("resume");
                }
                stream = response.GetResponseStream();
                long cl = response.ContentLength;
                long fileTotalLength = -1;
                if(cl >= 0)
                {
                    fileTotalLength = outs.Position + cl;
                }

                // verify size if we can
                if (!Config.Default.AppEncOn && (fileTotalLength >= 0) && rm.IsSizeValid()) 
                {
                    if (rm.size != fileTotalLength) 
                    {
                        if (outs != null)
                        {
                            outs.Close();
                            outs = null;
                        }
                        Utils.DeleteFile(tempFile);
                        throw new Exception(Str.Def.Get(Str.FailedLength) + rm.name);
                    }
                }
                
                long len = rm.size;
                if ((cl >= 0) && (len != cl))
                {
                    len = cl;

                    // update gui
                    if (Config.Default.ReportFileSize)
                    {
                        msg += " " + Utils.SizeStr(len);
                        m.Log(msg);
                    }

                    // update rm
                    if (!rm.IsSizeValid())
                    {
                        rm.size = fileTotalLength;
                    }
                }

                if (m.ShouldStop()) { return; }
                Utils.WriteStream(stream, outs, m, len);
                outs.Close();
                outs = null;
                Dispose();

                if (m.ShouldStop()) { return; }

                Encoder.Encode(tempFile, Config.Default.AppEnc, false);

                // check length
                m.Log(Str.Def.Get(Str.VerifyingFile));
                if (!rm.ValidateSize(tempFile)) 
                {
                    Utils.DeleteFile(tempFile);
                    throw new Exception(Str.Def.Get(Str.FailedLength) + rm.name);
                }
                // check crc
                if (rm.ShouldCheckCrc)
                {
                    CheckCrc(tempFile, m);
                }

                Utils.DeleteFile(outFile);
                File.Move(tempFile, outFile);
            }
            finally 
            {
                if (outs != null)
                {
                    outs.Close();
                    outs = null;
                }
                Dispose();
            }
        }

        private void CheckCrc(string tempFile, Monitor m) 
        {
            Crc hash = new Crc();
            using (Stream outs = Utils.OpenRead(tempFile))
            {
                hash.Update(outs, m);
            }
            if (m.ShouldStop()) { return; }
            string fcrc = hash.GetValue();
            if (!rm.crc.ToLower().Equals(fcrc))
            {
                Utils.DeleteFile(tempFile);
                throw new Exception(Str.Def.Get(Str.FailedCrc) + rm.name);
            }
        }

        public void Dispose()
        {
            if (stream != null) try { stream.Close(); }
                catch { }
            stream = null;
            if (response != null) try { response.Close(); }
                catch { }
            response = null;
        }

        private static void SetRange(HttpWebRequest request, long start)
        {
            SetRange(request, start, -1);
        }

        private static void SetRange(HttpWebRequest request, long start, long end)
        {
            MethodInfo method = typeof(WebHeaderCollection).GetMethod
                    ("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            string key = "Range";
            string val = ((end < 0)
                ? string.Format("bytes={0}-", start)
                : string.Format("bytes={0}-{1}", start, end));
            method.Invoke(request.Headers, new object[] { key, val });
        }

    }//EOC

    /*
    // string contentRange = response.Headers[HttpResponseHeader.ContentRange];
    // ContentRange cr = new ContentRange();
    // cr.Init(contentRange);
    public class ContentRange
    {
        long start = -1L;
        long end = -1L;
        long length = -1L;

        public bool IsValidRange
        {
            get { return ((start >= 0) && (end >= 0) && (start <= end)); }
        }

        public bool IsValidLength
        {
            get { return (length >= 0); }
        }

        public bool IsValid
        {
            get { return (IsValidLength && IsValidRange); }
        }

        public long RangeLength
        {
            get
            {
                if (!IsValidRange) return -1;
                return (end - start) + 1;
            }
        }

        public long TotalLength
        {
            get
            {
                return length;
            }
        }

        public void Init(string contentRange)
        {
            start = end = length = -1L;
            try
            {
                if (contentRange != null)
                {
                    if (contentRange.StartsWith("bytes "))
                    {
                        contentRange = contentRange.Substring(6);
                        string[] p = contentRange.Split('/');
                        string[] r = p[0].Split('-');
                        start = Convert.ToInt64(r[0]);
                        if (r.Length > 1)
                        {
                            end = Convert.ToInt64(r[1]);
                        }
                        if ((p.Length > 1) && !p[1].Equals("*"))
                        {
                            length = Convert.ToInt64(p[1]);
                        }
                    }
                }
            }
            catch (Exception xx)
            {
                Utils.OnError(xx);
                start = end = length = -1L;
            }
        }
    }//EOC
     */ 
}
