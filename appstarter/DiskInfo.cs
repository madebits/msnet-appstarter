using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ws
{
    class DiskInfo
    {
        /*
        [DllImport("kernel32")]
        internal static extern int GetDiskFreeSpaceEx(
            string lpDirectoryName,                 // directory name
            ref ulong lpFreeBytesAvailable,    // bytes available to caller
            ref ulong lpTotalNumberOfBytes,    // bytes on disk
            ref ulong lpTotalNumberOfFreeBytes // free bytes on disk
            ); */

        internal static long GetFreeDiskSpace(string dir)
        {
            System.IO.DriveInfo drive = Config.GetDrive(dir);
            if (drive != null)
            {
                return drive.AvailableFreeSpace;
            }
            return -1;
        }

        internal static bool EnoughFreeDiskSpace(string dir, long size)
        {
            if (size <= 0) return true;
            long freeDisk = GetFreeDiskSpace(dir);
            if (freeDisk < 0) return true;
            RawLog.Default.Log("Free: " + Utils.SizeStr(freeDisk));
            if (size > freeDisk)
            {
                return false;
            }
            return true;
        }

    }//EOC
}
