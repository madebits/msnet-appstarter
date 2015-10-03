/* 
     * Example using ExtractIconEx
     * Created by Martin Hyldahl (alanadin@post8.tele.dk)
     * http://www.hyldahlnet.dk
     */

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ws
{
    class AppIcon
    {
        private static Icon appIcon = null;
        private static object iconLock = new object();
        public static Icon Icon
        {
            get
            {
                lock (iconLock)
                {
                    if (appIcon == null)
                    {
                        string exe = Application.ExecutablePath;
                        try
                        {
                            appIcon = ExtractIcon.ExtractIconFromExe(exe, false);
                        }
                        catch(Exception xx){ Utils.OnError(xx); }
                    }
                    return appIcon;
                }
            }
        }
    }


    /// <summary>
    /// Example using ExtractIconEx
    /// </summary>
    class ExtractIcon
    {
        [DllImport("shell32", CharSet = CharSet.Auto)]
        private static extern int ExtractIconEx(
            string lpszFile,
            int nIconIndex,
            IntPtr[] phIconLarge,
            IntPtr[] phIconSmall,
            int nIcons);

        [DllImport("user32", EntryPoint = "DestroyIcon", SetLastError = true)]
        private static extern int DestroyIcon(IntPtr hIcon);

        public static Icon ExtractIconFromExe(string file, bool large)
        {
            int readIconCount = 0;
            IntPtr[] hDummy = new IntPtr[1] { IntPtr.Zero };
            IntPtr[] hIconEx = new IntPtr[1] { IntPtr.Zero };
            try
            {
                if (large)
                {
                    readIconCount = ExtractIconEx(file, 0, hIconEx, hDummy, 1);
                }
                else
                {
                    readIconCount = ExtractIconEx(file, 0, hDummy, hIconEx, 1);
                }
                if ((readIconCount > 0) && (hIconEx[0] != IntPtr.Zero))
                {
                    Icon extractedIcon = (Icon)Icon.FromHandle(hIconEx[0]).Clone();
                    return extractedIcon;
                }
                else
                { // NO ICONS READ
                    return null;
                }
            }
            finally
            {
                // RELEASE RESOURCES
                foreach (IntPtr ptr in hIconEx)
                {
                    if (ptr != IntPtr.Zero)
                        DestroyIcon(ptr);
                }

                foreach (IntPtr ptr in hDummy)
                {
                    if (ptr != IntPtr.Zero)
                        DestroyIcon(ptr);
                }
            }

        }
    }
}