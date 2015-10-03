using System;
using System.Collections;
using System.IO;

namespace ws
{
    class Local
    {
        public static ArrayList localFiles = new ArrayList();
        public static ArrayList localFileAsocs = new ArrayList();

        public static string ReplaceLockId
        {
            get { return Config.WStarter + "." + Config.Default.AppId + ".rep"; }
        }

        public static bool Start() 
        {
            bool noGui = true;
            try
            {
                noGui = Start(true);
            }
            catch (Exception ex) 
            {
                RawLog.Default.Log(Str.Def.Get(Str.Error) + " # " + ex.Message);
                ClearCacheNoError();
            }
            return noGui;
        }

        public static bool Start(bool callReplaceFiles)
        {
            if (callReplaceFiles)
            {
                Local.ReplaceFiles();
            }
            else
            {
                ExclusiveLocker replaceLock = new ExclusiveLocker();
                replaceLock.id = Local.ReplaceLockId;
                try
                {
                    if (!replaceLock.Lock(2000))
                    {
                        return false;
                    }
                }
                finally
                {
                    replaceLock.Dispose();
                }
            }
            InitLocalFiles();
            int startCount = RemoteFile.Start(localFiles);
            return (startCount <= 0); // true for gui
        }

        private static void InitLocalFiles()
        {
            localFiles = null;
            localFileAsocs = new ArrayList();
            try
            {
                string path = Config.Default.GetFileListPath(false);
                if (File.Exists(path))
                {
                    localFiles = RemoteFile.Load(path, ref localFileAsocs);
                    if ((localFileAsocs != null) && (localFileAsocs.Count > 0))
                    {
                        string currentStarterExePath = System.Windows.Forms.Application.ExecutablePath;
                        string np = Config.Default.KeyStore.GetString(KeyStoreIds.StarterLastPathS, null, false);
                        if (np == null)
                        {
                            Config.Default.KeyStore.SetString(KeyStoreIds.StarterLastPathS, currentStarterExePath, true);
                        }
                        else if (!np.Equals(currentStarterExePath))
                        {
                            Config.Default.KeyStore.SetString(KeyStoreIds.StarterLastPathS, currentStarterExePath, true);
                            RawLog.Default.Log("!path");
                            FileAssoc.Register(localFileAsocs, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                localFiles = new ArrayList();
                throw ex;
            }
        }

        // false if replace needed but could not get lock
        public static bool ReplaceFiles()
        {
            ExclusiveLocker replaceLock = new ExclusiveLocker();
            replaceLock.id = Local.ReplaceLockId;
            try
            {
                if (!replaceLock.Lock(0))
                {
                    return false;
                }
                if (!Config.Default.NewVersionFileTag)
                {
                    return true;
                }
                RawLog.Default.Log("rep>");
                string tempDir = Config.Default.GetPath(null, true, true);
                string workingDir = Config.Default.GetPath(null, false, true);
                if (!Directory.Exists(tempDir))
                {
                    Config.Default.NewVersionFileTag = false;
                    return true;
                }
                if (!Utils.CheckFullAccess(workingDir))
                {
                    RawLog.Default.Log("!work");
                    return true;
                }
                if (!Utils.CheckFullAccess(tempDir))
                {
                    RawLog.Default.Log("!temp");
                    return true;
                }
                            
                ArrayList localFilesTmp = null;
                ArrayList remoteFilesTmp = null;
                ArrayList localFilesAsocTmp = new ArrayList();
                ArrayList remoteFilesAsocTmp = new ArrayList();

                string localFileTmp = Config.Default.GetFileListPath(false);
                string remoteFileTmp = Config.Default.GetFileListPath(true);
                bool localExits = File.Exists(localFileTmp);
                bool remoteExits = File.Exists(remoteFileTmp);
                try
                {
                    if (localExits && remoteExits)
                    {
                        localFilesTmp = RemoteFile.Load(localFileTmp, ref localFilesAsocTmp);
                        remoteFilesTmp = RemoteFile.Load(remoteFileTmp, ref remoteFilesAsocTmp);
                    }
                }
                catch (Exception xx) { Utils.OnError(xx); }


                // copy / replace new files
                //Utils.DeleteDir(workingDir);
                //Directory.Move(tempDir, workingDir);
                Utils.CopyReplaceFiles(tempDir, workingDir);
                InitLocalFiles();

                // delete removed files
                try
                {
                    if (localExits && remoteExits)
                    {
                        ArrayList toDelete = RemoteFile.GetRemovedFiles(localFilesTmp, remoteFilesTmp, false);
                        RemoteFile.DeleteFiles(toDelete, false);
                    }
                }
                catch (Exception xx) { Utils.OnError(xx); }
                // set file associations
                if (!FileAssoc.AreEqual(localFilesAsocTmp, localFileAsocs))
                {
                    try
                    {
                        FileAssoc.Register(localFilesAsocTmp, false);
                    }
                    catch (Exception xx) { Utils.OnError(xx); }
                    try
                    {
                        FileAssoc.Register(localFileAsocs, true);
                    }
                    catch (Exception xx) { Utils.OnError(xx); }
                }

                RawLog.Default.Log("rep<");
                Config.Default.NewVersionFileTag = false;
            }
            finally
            {
                replaceLock.Dispose();
            }
            return true;
        }

        public static void ClearCacheNoError()
        {
            try
            {
                Local.ClearCache();
            }
            catch (Exception ce) { RawLog.Default.Log(Str.Def.Get(Str.Error) + " # " + ce.Message); }
        }

        public static void ClearCache()
        {
            RawLog.Default.Log("-cache");
            RawLog.Default.Dispose();
            string path = Config.Default.AppFolder;
            if (!Utils.CheckFullAccess(path))
            {
                throw new Exception(Str.Def.Get(Str.CannotClearCache));
            }
            try
            {
                Utils.DeleteDir(path);
            }
            catch
            {
                System.Threading.Thread.Sleep(250);
                if (!Utils.CheckFullAccess(path))
                {
                    throw new Exception(Str.Def.Get(Str.CannotClearCache));
                }
                Utils.DeleteDir(path);
            }
            try
            {
                try
                {
                    Remote.SetLastCheckDate(0);
                }
                catch (Exception xx) { Utils.OnError(xx); }
                Utils.DeleteEmptyDir(Config.Default.WStarterPath, true);
            }
            catch (Exception xx) { Utils.OnError(xx); }
            
        }

    }//EOC
}
