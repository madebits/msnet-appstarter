using System;
using System.Collections;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace ws
{
    class FileAssocWin
    {
        const uint SHCNE_ASSOCCHANGED = 0x8000000;
        const uint SHCNF_IDLIST = 0x0;

        [DllImport("shell32")]
        static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public static void NotifyShell()
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Registers a file type via it's extension. If the file type is already registered, nothing is changed.
        /// </summary>
        /// <param name="extension">The extension to register</param>
        /// <param name="progId">A unique identifier (GUID) for the program to work with the file type</param>
        /// <param name="description">A brief description of the file type</param>
        /// <param name="executeable">Where to find the executeable.</param>
        /// <param name="iconFile">Location of the icon.</param>
        /// <param name="iconIdx">Selects the icon within <paramref name="iconFile"/></param>
        public static void Register(string extension, string progId, string description, string executeable, string iconFile, int iconIdx)
        {
            if (!string.IsNullOrEmpty(extension) && !string.IsNullOrEmpty(progId))
            {
                progId = "WS" + progId;
                if (extension[0] != '.')
                {
                    extension = "." + extension;
                }

                // register the extension, if necessary
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(extension, true))
                {
                    if (key == null)
                    {
                        using (RegistryKey extKey = Registry.ClassesRoot.CreateSubKey(extension))
                        {
                            extKey.SetValue(string.Empty, progId);
                            SetOpenWithProgids(extKey, progId);
                        }
                    }
                    else
                    {
                        string currentPid = (string)key.GetValue(string.Empty);
                        if (string.IsNullOrEmpty(currentPid)) 
                        {
                            key.SetValue(string.Empty, progId);
                        }
                        SetOpenWithProgids(key, progId);
                    }
                }

                // register the progId, if necessary
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(progId))
                {
                    if (key == null)
                    {
                        using (RegistryKey progIdKey = Registry.ClassesRoot.CreateSubKey(progId))
                        {
                            progIdKey.SetValue(string.Empty, description);
                            using (RegistryKey defaultIcon = progIdKey.CreateSubKey("DefaultIcon"))
                            {
                                defaultIcon.SetValue(string.Empty, String.Format("\"{0}\",{1}", iconFile, iconIdx));
                            }

                            using (RegistryKey command = progIdKey.CreateSubKey("shell\\open\\command"))
                            {
                                command.SetValue(string.Empty, String.Format("\"{0}\" \"%1\"", executeable));
                            }
                        }

                        // mui cache ?! -- this seems to be require - it took me two days to find this out
                        try
                        {
                            using (RegistryKey muiCacheKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\ShellNoRoam\MUICache", true))
                            {
                                if (muiCacheKey != null)
                                {
                                    System.Diagnostics.FileVersionInfo fi = System.Diagnostics.FileVersionInfo.GetVersionInfo(executeable);
                                    if (fi != null)
                                    {
                                        string desc = fi.FileDescription;
                                        if (string.IsNullOrEmpty(desc))
                                        {
                                            string t = fi.FileName;
                                            if (!string.IsNullOrEmpty(t))
                                            {
                                                desc = System.IO.Path.GetFileNameWithoutExtension(fi.FileName);
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(desc))
                                        {
                                            muiCacheKey.SetValue(executeable, desc);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception xx) { Utils.OnError(xx); }
                    }
                }
            }
        }

        public static void UnRegister(string extension, string progId)
        {
            if (!string.IsNullOrEmpty(extension))
            {
                progId = "WS" + progId;
                if (extension[0] != '.')
                {
                    extension = "." + extension;
                }
                bool canDeleteFileKey = false;
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(extension, true))
                {
                    if (key != null)
                    {
                        string keyProgId = (string)key.GetValue(string.Empty);
                        if (!string.IsNullOrEmpty(keyProgId)
                            && (keyProgId.ToLower() == progId.ToLower()))
                        {
                            key.DeleteValue(string.Empty);
                            canDeleteFileKey = true;
                        }
                        RemoveOpenWithProgids(key, progId);
                    }
                }
                if (canDeleteFileKey)
                {
                    try
                    {
                        Registry.ClassesRoot.DeleteSubKey(extension, false);
                    }
                    catch (Exception xx){ Utils.OnError(xx); }
                }
            }
            if (!string.IsNullOrEmpty(progId))
            {
                bool exists = false;
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(progId))
                {
                    exists = (key != null);
                }
                if (exists)
                {
                    try
                    {
                        Registry.ClassesRoot.DeleteSubKeyTree(progId);
                    }
                    catch (Exception xx) { Utils.OnError(xx); }
                }
            }
        }

        private static void SetOpenWithProgids(RegistryKey key, string progId)
        {
            if (key == null) return;
            RegistryKey skey = key.OpenSubKey(OpenWithProgids, true);
            if (skey == null) skey = key.CreateSubKey(OpenWithProgids);
            if (skey == null) return;
            skey.SetValue(progId, string.Empty);
            skey.Close();
        }

        private static void RemoveOpenWithProgids(RegistryKey key, string progId)
        {
            if (key == null) return;
            RegistryKey skey = key.OpenSubKey(OpenWithProgids, true);
            if (skey == null) return;
            skey.DeleteValue(progId, false);
            string[] skn = skey.GetSubKeyNames();
            string[] skv = skey.GetValueNames();
            if (((skn == null) || (skn.Length <= 0)) && ((skv == null) || (skv.Length <= 0))) 
            {
                skey.Close();
                skey = null;
                try
                {
                    key.DeleteSubKey(OpenWithProgids, false);
                }
                catch (Exception xx) { Utils.OnError(xx); }
            }
            if (skey != null)
            {
                skey.Close();
            }
        }

        private const string OpenWithProgids = "OpenWithProgids";

    }//EOC
}
