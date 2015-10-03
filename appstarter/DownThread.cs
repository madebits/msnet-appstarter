using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Threading;

namespace ws
{
    class DownThread : IDisposable
    {
        public static DownThread Default = new DownThread();
        
        private Monitor monitor = new Monitor();
        private Thread thread = null;
        private ManualResetEvent shouldStop = new ManualResetEvent(false);
        private bool callStart = false;
                
        public delegate void DDone(bool errorState);
        public DDone doneAction = null;
        public Remote.DBeforeCallStart beforeCallStart = null;

        public void Start(Monitor m, bool callStart) 
        {
            Stop();
            this.callStart = callStart;
            if (m != null) 
            {
                this.monitor = m;
            }
            if (this.monitor.shouldStop == null) 
            {
                this.monitor.shouldStop = new Monitor.DShouldStop(this.ShouldStop);
            }
            thread = new Thread(new ThreadStart(DoWork));
            thread.Name = Config.WStarter;
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.BelowNormal;
            shouldStop.Reset();
            thread.Start();
        }

        public Monitor Monitor { get { return this.monitor;  } }

        public bool ShouldStop()
        {
            return ShouldStop(0);
        }

        public bool ShouldStop(int waitTimeMls)
        {
            return shouldStop.WaitOne(waitTimeMls);
        }


        private bool SetShouldStop()
        {
            return shouldStop.Set();
        }

        public bool IsWorking
        {
            get
            {
                try
                {
                    return (thread != null);
                }
                catch { }
                return false;
            }
        }

        public void Stop() 
        {
            SetShouldStop();
            if (thread != null) 
            {
                try
                {
                    //Thread.Sleep(2000); // .Join
                    for (int i = 0; i < 3; i++) 
                    {
                        if (thread == null) return;
                        Thread.Sleep(250);
                    }
                    if (thread != null)
                    {
                        RawLog.Default.Log("abort");
                        thread.Abort();
                    }
                }
                catch { }
            }
            thread = null;
        }

        public void Dispose() 
        {
            Stop();
        }

        ~DownThread() { Dispose(); }

        private void DoWork() 
        {
            try
            {
                bool errorState = false;
                try
                {
                    Remote.Default.DoWork(this.monitor, this.callStart, beforeCallStart);
                }
                catch (Exception ex)
                {
                    errorState = true;
                    if (Thread.CurrentThread.ThreadState.Equals(ThreadState.Running)
                        ||
                        Thread.CurrentThread.ThreadState.Equals(ThreadState.Background))
                    {
                        if (monitor != null)
                        {
                            monitor.Log(ex.Message, true);
                        }
                        else 
                        {
                            RawLog.Default.Log(ex.Message, true);
                        }
                    }
                }
                finally
                {
                    thread = null;
                    try
                    {
                        if (doneAction != null)
                        {
                            doneAction(errorState);
                        }
                    }
                    catch (Exception xx) { Utils.OnError(xx); }
                }
            }
            catch (Exception xx) { Utils.OnError(xx); }
        }

    }//EOC
}
