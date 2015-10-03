using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ws
{
    public partial class DLicense : Form
    {
        public DLicense()
        {
            InitializeComponent();
            this.Icon = null;
            this.Text = Str.Def.Get(Str.LicenseTitle);
            this.btnOk.Text = Str.Def.Get(Str.LicenseOk);
            this.btnCancel.Text = Str.Def.Get(Str.LicenseCancel);
        }

        private void DLicense_Load(object sender, EventArgs e)
        {
            try
            {
                if (!isShowTextMode 
                    && ((Config.Default.licenseWinW > 32) || (Config.Default.licenseWinH > 32)))
                {
                    Size s = new Size(this.Width, this.Height);
                    if (Config.Default.licenseWinW > 0) s.Width = Config.Default.licenseWinW;
                    if (Config.Default.licenseWinH > 0) s.Width = Config.Default.licenseWinH;
                    this.Size = s;
                }
                Application.DoEvents();
                this.txtText.BackColor = SystemColors.Window;
                if (!isShowTextMode)
                {
                    this.txtText.Text = Config.Default.license;
                }
            }
            catch (Exception ex) { Utils.OnError(ex); }
            
        }

        private void txtText_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                string l = e.LinkText;
                if(string.IsNullOrEmpty(l)) return;
                if (l.ToLower().Equals("http://")) return;
                if (l.ToLower().Equals("file://")) return;
                if (l.ToLower().Equals("news://")) return;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(l);
                if (p != null)
                {
                    p.Close();
                    p = null;
                }
            }
            catch(Exception ex)
            {
                Utils.OnError(ex);
            }
            this.txtText.Focus();
        }

        private bool isShowTextMode = false;
        public void SetShowText(string text) 
        {
            isShowTextMode = true;
            if (text == null) text = string.Empty;
            this.Text = "Info - " + Config.Default.AppName;
            this.btnCancel.Visible = false;
            this.btnOk.Text = "&Close";
            this.btnOk.Location = new Point((this.Size.Width - this.btnOk.Width) / 2, btnOk.Location.Y);
            this.txtText.Text = text;
            this.txtText.WordWrap = false;
            this.txtText.DetectUrls = false;
            this.txtText.ScrollBars = RichTextBoxScrollBars.Both;
        }

        public static void ShowText(string text)
        {
            DLicense dl = new DLicense();
            dl.SetShowText(text);
            dl.ShowDialog(null);
        }
    }//EOC
}
