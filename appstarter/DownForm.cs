using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ws
{
    public partial class DownForm : Form
    {
        public DownForm()
        {
            InitializeComponent();
            this.Text = Config.Default.AppName;
            this.Icon = null;
            Icon c = AppIcon.Icon;
            if (c == null)
            {
                this.notifyIcon.Visible = false;
            }
            else
            {
                //this.Icon = c;
                //this.ShowIcon = true;
                this.notifyIcon.Icon = c;
                this.notifyIcon.Text = Config.Default.AppName;
            }
        }       

        private void MakeInv()
        {
            try
            {
                this.Size = new Size(1, 1);
                if (this.Visible)
                {
                    this.Visible = false;
                }
            }
            catch (Exception xx) { Utils.OnError(xx); }
        }

        private bool invMode = false;
        public void SetInivisibleMode() 
        {
            invMode = true;
            UIVisible = false;
        }

        private bool allowVisible = true;
        private volatile bool withinVisibilityChange = false;
        private bool UIVisible
        {
            get
            {
                return this.Visible;
            }
            set
            {
                if (withinVisibilityChange) 
                {
                    return;
                }
                try
                {
                    withinVisibilityChange = true;
                    if (value && !allowVisible)
                    {
                        return;
                    }
                    this.Visible = value;
                    this.Opacity = value ? 100.0 : 0.0;
                    this.ShowInTaskbar = value ? true : false;
                    FormWindowState newState = value ? FormWindowState.Normal : FormWindowState.Minimized;
                    if (this.WindowState != newState)
                    {
                        this.WindowState = newState;
                    }
                    try
                    {
                        System.Diagnostics.ProcessPriorityClass newPrio = value ? System.Diagnostics.ProcessPriorityClass.Normal : System.Diagnostics.ProcessPriorityClass.BelowNormal;
                        if (System.Diagnostics.Process.GetCurrentProcess().PriorityClass != newPrio)
                        {
                            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = newPrio;
                        }
                    }
                    catch (Exception ex) { Utils.OnError(ex); }
                    Application.DoEvents();
                }
                catch (Exception xx) { Utils.OnError(xx); }
                finally
                {
                    withinVisibilityChange = false;
                }
            }
        }

        private void DownForm_Load(object sender, EventArgs e)
        {
            if (invMode)
            {
                UIVisible = false;
            }
            Application.DoEvents();
            if (!invMode 
                && Config.Default.HasLicense 
                && !Config.Default.KeyStore.GetBool(KeyStoreIds.LicenseShownB, false, true)) 
            {
                DLicense dl = new DLicense();
                DialogResult dr = dl.ShowDialog(this);
                dl = null;
                if (dr.Equals(DialogResult.Cancel))
                {
                    Application.DoEvents();
                    InnerClose();
                }
                else 
                {
                    Config.Default.KeyStore.SetBool(KeyStoreIds.LicenseShownB, true, true);
                }
            }
            DownThread.Default.Monitor.OnLog += Log;
            DownThread.Default.Monitor.OnLogProgress += LogProgress;
            DownThread.Default.doneAction = new DownThread.DDone(this.ThreadDone);
            DownThread.Default.beforeCallStart = new Remote.DBeforeCallStart(this.BeforeCallStart);
            DownThread.Default.Start(null, !invMode || Config.Default.updateBeforeStart);
        }

        volatile bool isErrorState = false;
        private void Log(string s, bool isError) 
        {
            if (isError) isErrorState = true;
            this.Invoke(new Monitor.DLog(_Log), new object[] { s, isError }); 
        }

        private void _Log(string s, bool isError)
        {
            try
            {
                if (s == null) s = "*";
                s = s.Replace("\r", string.Empty);
                s = s.Replace("\n", " ");
                s = s.Replace("\t", " ");
                if (isError) s = Str.Def.Get(Str.Error) + ": " + s;
                this.txtLog.Text = s;
                s = s + " " + Config.Default.AppName;
                if (s.Length > 40) s = s.Substring(0, 40) + "...";
                this.notifyIcon.Text = s;
            }
            catch (Exception xx) { Utils.OnError(xx); }
        }

        private void LogProgress(int p) 
        {
            this.Invoke(new Monitor.DLogProgress(_LogProgress), new object[] { p }); 
        }

        private void _LogProgress(int p) 
        {
            try
            {
                if (DownThread.Default.ShouldStop()) 
                {
                    return;
                }
                int percent = (int)p;
                if (percent < 0) percent = 0;
                if (percent > 100) percent = 100;
                if (this.progressBar.Value != percent)
                {
                    this.progressBar.Value = percent;
                    this.toolTip.SetToolTip(this.progressBar, percent.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%");
                }
            }
            catch (Exception xx) { Utils.OnError(xx); }
        }

        private void ThreadDone(bool errorState) 
        {
            this.Invoke(new DownThread.DDone(_ThreadDone), new object[]{ errorState });
        }

        private void _ThreadDone(bool errorState)
        {
            this.progressBar.Value = 0;
            if (!this.UIVisible) 
            {
                InnerClose();
            }
            if (isErrorState)
            {
                // do nothing
                if (Config.Default.updateBeforeStart)
                {
                    _BeforeCallStart();
                    if (!Local.Start(true))
                    {
                        InnerClose();
                    }
                }
            }
            else
            {
                InnerClose();
            }
        }

        private void BeforeCallStart()
        {
            this.Invoke(new MethodInvoker(_BeforeCallStart));
        }

        private void _BeforeCallStart()
        {
            allowVisible = false;
            UIVisible = false;
        }

        private bool innerClose = false;
        public void InnerClose() 
        {
            innerClose = true;
            Close();
        }

        private volatile bool withinClose = false;
        private void DownForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (withinClose) return;
            try
            {
                withinClose = true;

                if (!innerClose && (e.CloseReason == CloseReason.UserClosing)) 
                {
                    if (DownThread.Default.IsWorking)
                    {
                        try
                        {
                            if (MessageBox.Show(this, Str.Def.Get(Str.ConfirmClose), Config.Default.AppName, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
                            {
                                e.Cancel = true;
                                UIVisible = false;
                                return;
                            }
                        }
                        catch (Exception xx) { Utils.OnError(xx); }
                    }
                }
                
                allowVisible = false;
                UIVisible = false;
                try
                {
                    DownThread.Default.doneAction = null;
                    DownThread.Default.beforeCallStart = null;
                }
                catch (Exception xx) { Utils.OnError(xx); }
                DownThread.Default.Stop();
                try
                {
                    Application.Exit();
                }
                catch (Exception ex) { Utils.OnError(ex); }
            }
            finally
            {
                withinClose = false;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                Application.DoEvents();
                this.UIVisible = !this.UIVisible;
            }
            catch (Exception xx) { Utils.OnError(xx); }
        }
        private void DownForm_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.UIVisible = false;
            }
        }

    }//EOC
}
