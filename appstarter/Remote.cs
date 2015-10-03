using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Cache;
using System.Reflection;


namespace ws
{
    class Remote
    {
        public static Remote Default = new Remote();
        private ArrayList remoteFiles = new ArrayList();
        private ArrayList remoteFileAsocs = new ArrayList();

        private bool CanCheckServer() 
        {
            DateTimeOffset now = DateTimeOffset.Now;
            long ticks = Config.Default.KeyStore.GetLong(KeyStoreIds.LastDateTicksL, now.Ticks, false);
            long offset = Config.Default.KeyStore.GetLong(KeyStoreIds.LastDateOffsetL, now.Offset.Ticks, false);
            DateTimeOffset lastTime = new DateTimeOffset(ticks, new TimeSpan(offset));
            double hours = now.Subtract(lastTime).TotalHours;
            RawLog.Default.Log("DH "
                + hours.ToString(System.Globalization.CultureInfo.InvariantCulture) 
                + " "
                + Config.Default.DeltaCheckHours.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if ((hours >= 0.0) && (hours < Config.Default.DeltaCheckHours))
            {
                return false; // nothing to do (yet!)
            }
            return true;
        }

        public delegate void DBeforeCallStart();
        public void DoWork(Monitor m, bool callStart, DBeforeCallStart beforeCallStart) 
        {
            // check time interval if not gui
            if (!callStart && !CanCheckServer())
            {
                return;
            }

            // clean up temp dir as neccesary
            string remoteFilesList = Config.Default.GetFileListPath(true);
            ArrayList remoteCurrentFiles = null;
            ArrayList remoteCurrentFilesAsoc = new ArrayList();
            if (File.Exists(remoteFilesList))
            {
                try
                {
                    remoteCurrentFiles = RemoteFile.Load(remoteFilesList, ref remoteCurrentFilesAsoc);
                }
                catch (Exception ex)
                {
                    RawLog.Default.Log(ex.Message, true);
                    Config.Default.NewVersionFileTag = false;
                    string tempDir = Config.Default.GetPath(null, true, true);
                    Utils.DeleteDir(tempDir);
                }
            }

            // get data from server
            if (m != null) m.Log(Str.Def.Get(Str.Connecting));
            long lastTimeTicks = Config.Default.KeyStore.GetLong(KeyStoreIds.RemoteFilesLastDateL, DateTime.MinValue.Ticks, false);
            if (!GetRemoteVersion(m, ref lastTimeTicks)) 
            {
                if ((m != null) && m.ShouldStop()) { return; }
                SetLastCheckDate(lastTimeTicks);
                if (Config.Default.updateBeforeStart) 
                {
                    if (callStart)
                    {
                        if (beforeCallStart != null)
                        {
                            beforeCallStart();
                        }
                        if (Local.Start())
                        {
                            throw new Exception("Nothing to execute");
                        }
                        if (m != null) m.Log(string.Empty);
                    }
                }
                return;
            }
            if ((m != null) && m.ShouldStop()) { return; }
            if (remoteFiles == null)
            {
                throw new Exception(Str.Def.Get(Str.RemoteError));
            }
            ArrayList newRemoteFiles = this.remoteFiles;
            if ((remoteCurrentFiles != null) && (remoteCurrentFiles.Count > 0))
            {
                newRemoteFiles = RemoteFile.GetNewFiles(remoteCurrentFiles, this.remoteFiles, false, true);
                ArrayList tempFileToDelete = RemoteFile.GetRemovedFiles(remoteCurrentFiles, this.remoteFiles, true);
                RemoteFile.DeleteFiles(tempFileToDelete, true);
            }
            if ((m != null) && m.ShouldStop()) { return; }
            //string tempDir = Config.Default.GetPath(null, true, true);
            //Utils.DeleteDir(tempDir);
            ArrayList files = RemoteFile.GetNewFiles(Local.localFiles, newRemoteFiles); // this.remoteFiles
            if ((files == null) || (callStart && (files.Count <= 0))) 
            {
                throw new Exception(Str.Def.Get(Str.RemoteError));
            }
            if (files.Count <= 0) 
            {
                RemoteFile.Save(Config.Default.GetFileListPath(true), this.remoteFiles, this.remoteFileAsocs);
                SetLastCheckDate(lastTimeTicks);
                ArrayList toDelete = RemoteFile.GetRemovedFiles(Local.localFiles, this.remoteFiles, false);
                bool newAssoc = !FileAssoc.AreEqual(Local.localFileAsocs, this.remoteFileAsocs);
                if (newAssoc || ((toDelete != null) && (toDelete.Count > 0))) 
                {
                    Config.Default.NewVersionFileTag = true;
                }
                //if (m != null) m.Log(Str.Def.Get(Str.NothingToDo));
                return;
            }

            // invalidate current update if any
            Config.Default.NewVersionFileTag = false;
            RemoteFile.Save(Config.Default.GetFileListPath(true), remoteFiles, this.remoteFileAsocs);

            // delete new files in any, and check for space
            RemoteFile.DeleteFiles(files, true);
            long outSize = 0;
            if (!RemoteFile.CheckDiskSpace(files, ref outSize)) 
            {
                throw new Exception(Str.Def.Get(Str.NoSpace) + " " + Utils.SizeStr(outSize));
            }

            if (m != null) m.Log("(" + files.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
            bool isStarter = false;
            for (int i = 0; i < files.Count; i++)
            {
                if ((m != null) && m.ShouldStop()) { return; }
                RemoteFile rm = (RemoteFile)files[i];
                isStarter = rm.IsStarterReplacer;
                if (isStarter) 
                {
                    Config.Default.StarterNewVersion = Config.Default.StarterLastVersion;
                }
                if ((m != null) && m.ShouldStop()) { return; }
                string msgRaw = "[" + (i + 1) + " / " + files.Count + "] " + rm.DisplayName;
                string msg = msgRaw;
                if (Config.Default.ReportFileSize && rm.IsSizeValid()) 
                {
                    msg += " " + Utils.SizeStr(rm.size);
                }
                m.Log( msg );
                using (HttpGetter hg = new HttpGetter())
                {
                    hg.Init(rm);
                    string outFile = rm.GetPath(true);
                    try
                    {
                        hg.Dump(outFile, m, msgRaw);
                    }
                    catch (Exception ex)
                    {
                        Utils.DeleteFile(outFile);
                        throw ex;
                    }
                    finally 
                    {
                        hg.Dispose();
                    }
                    if ((m != null) && m.ShouldStop())
                    {
                        Utils.DeleteFile(outFile);
                        break;
                    }
                    // file ok
                    if (isStarter) 
                    {
                        RawLog.Default.Log("snver " + rm.version);
                        Config.Default.StarterNewVersion = rm.version;
                    }
                }
            }
            if ((m != null) && m.ShouldStop()) { return; }
            
            // normal finish
            RemoteFile.Save(Config.Default.GetFileListPath(true), remoteFiles, this.remoteFileAsocs); // again
            SetLastCheckDate(lastTimeTicks);
            if (isStarter && (files.Count == 1))
            {
                //nothing new
            }
            else
            {
                Config.Default.NewVersionFileTag = true;
            }
            if (callStart)
            {
                if (beforeCallStart != null)
                {
                    beforeCallStart();
                }
                if (Local.Start())
                {
                    throw new Exception("Nothing to execute");
                }
                if (m != null) m.Log(string.Empty);
            }
            else
            {
                if (Config.Default.NewVersionFileTag)
                {
                    //if (m != null) m.Log(Str.Def.Get(Str.RestartNeeded));
                }
            }
        }

        public static void SetLastCheckDate(long lastRemoteFileTimeTicks)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            Config.Default.KeyStore.SetLong(KeyStoreIds.LastDateTicksL, now.Ticks, false);
            Config.Default.KeyStore.SetLong(KeyStoreIds.LastDateOffsetL, now.Offset.Ticks, false);
            Config.Default.KeyStore.SetLong(KeyStoreIds.RemoteFilesLastDateL, lastRemoteFileTimeTicks, true);
        }

        // false mean nothing new
        private bool GetRemoteVersion(Monitor m, ref long lastTimeTicks)
        {
            Stream s = null;
            HttpWebResponse response = null;
            remoteFiles.Clear();
            try
            {
                string url = Config.Default.AppUrl;
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request = HttpGetter.SetRequestDefaults(request);
                    response = (HttpWebResponse)request.GetResponse();
                    if (response == null) throw new Exception();
                }
                catch
                {
                    url = Config.Default.AppUrl2;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request = HttpGetter.SetRequestDefaults(request);
                    response = (HttpWebResponse)request.GetResponse();          
                }

                if (response == null) throw new Exception();

                // date
                try
                {
                    DateTime lastTime = new DateTime(lastTimeTicks);
                    DateTime remoteFilesDate = response.LastModified;
                    if (Config.Default.checkRemoteFileDate)
                    {
                        if (DateTime.Compare(lastTime, remoteFilesDate) == 0)
                        {
                            return false;
                        }
                    }
                    lastTimeTicks = remoteFilesDate.Ticks;
                }
                catch (Exception xx) { Utils.OnError(xx); }

                s = response.GetResponseStream();
                if ((m != null) && m.ShouldStop()) { return false; }
                if (s == null) throw new Exception(Str.Def.Get(Str.RemoteError));
                remoteFiles = RemoteFile.Load(s, ref this.remoteFileAsocs);
            }
            finally
            {
                if (s != null) try { s.Close(); }
                    catch {  }
                s = null;
                if (response != null) try { response.Close(); }
                    catch {  }
                response = null;
            }
            return true;
        }

    }//EOC
}
