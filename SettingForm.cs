using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TouchPadPCServer
{
    public partial class SettingForm : Form
    {
        public SettingForm()
        {
            InitializeComponent();
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            txbIP.Text = Properties.Settings.Default.LocalIP;
            txbPort.Text = Properties.Settings.Default.LocalPort.ToString();
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.LocalIP = txbIP.Text.Trim();
            Properties.Settings.Default.LocalPort = int.Parse(txbPort.Text.Trim());

            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
