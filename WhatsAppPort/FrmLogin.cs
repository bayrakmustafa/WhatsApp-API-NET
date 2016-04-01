using System;
using System.Windows.Forms;
using WhatsAppPasswordExtractor;

namespace WhatsAppPort
{
    public partial class FrmLogin : Form
    {
        public FrmLogin()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (!this.CheckLogin(this.textBoxPhone.Text, this.textBoxPass.Text))
            {
                MessageBox.Show(this, "Login failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var frm = new FrmForm(this.textBoxPhone.Text, this.textBoxPass.Text, this.textBoxNick.Text))
            {
                this.Visible = false;
                frm.ShowDialog();

                this.Visible = true;
                this.BringToFront();
            }
        }

        private bool CheckLogin(string user, string pass)
        {
            try
            {
                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                    return false;

                WhatSocket.Create(user, pass, this.textBoxNick.Text, true);
                WhatSocket.Instance.Connect();
                WhatSocket.Instance.Login();

                //Check Login Status
                if (WhatSocket.Instance.ConnectionStatus == WhatsAppApi.WhatsApp.CONNECTION_STATUS.LOGGEDIN)
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            FrmRegister regForm = new FrmRegister(this.textBoxPhone.Text);
            DialogResult regResult = regForm.ShowDialog(this);
            if (regResult == System.Windows.Forms.DialogResult.OK)
            {
                this.textBoxPass.Text = regForm.password;
                this.textBoxPhone.Text = regForm.number;
            }
        }

        private void btnGetPassword_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxPhone.Text))
            {
                MessageBox.Show(this, "Please enter your phone number", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                String password = PwExtractor.ExtractPassword(textBoxPhone.Text);
                textBoxPass.Text = password;
            }
        }
    }
}