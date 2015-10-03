using System;
using System.Threading;

namespace ws
{
    // this is cross process
    class ExclusiveLocker : IDisposable
    {
        public static ExclusiveLocker Default = new ExclusiveLocker();
        private Mutex aloneMutex = null;
        private int lockCount = 0;
        private bool disposed = false;
        public string id = null;

        public void Dispose() 
        {
            lock (this)
            {
                if (disposed) return;
                disposed = true;
                lockCount = 0;
                if (aloneMutex != null)
                {
                    aloneMutex.Close();
                    aloneMutex = null;
                }
            }
        }

        public int LockCount { get { lock(this){ return lockCount; } } }

        public bool Lock()
        {
            return Lock(0);
        }

        public bool Lock(int waitTime)
        {
            lock (this) 
            {
                if (disposed) return false;
                bool createdNew = true;
                if(aloneMutex == null)
                {
                    if (id == null) 
                    {
                        id = Config.WStarter + "." + Config.Default.AppId;
                    }
                    aloneMutex = new Mutex(false, id, out createdNew);
                }
                bool ok = false;
                try
                {
                    ok = aloneMutex.WaitOne(waitTime);
                }
                catch (AbandonedMutexException ex)
                {
                    ok = true; // we own mutext here
                    Utils.OnError(ex);
                }
                if (ok) 
                {
                    lockCount++;
                }
                return ok;
            }        
        }

        public void Unlock() 
        {
            lock (this)
            {
                if (disposed) return;
                if (aloneMutex == null) return;
                if (lockCount > 0)
                {
                    aloneMutex.ReleaseMutex();
                    lockCount--;
                }
            }
        }

    }//EOC
}
