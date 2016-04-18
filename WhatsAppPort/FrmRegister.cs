using System;
using System.Windows.Forms;

namespace WhatsAppPort
{
    public partial class FrmRegister : Form
    {
        public string number;
        protected string cc;
        protected string phone;
        public string password;

        public FrmRegister(string number)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(number))
            {
                this.number = number;
                this.cc = this.number.Substring(0, 2);
                this.phone = this.number.Substring(2);
                this.txtPhoneNumber.Text = number;
            }
        }

        private void btnCodeRequest_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(this.txtPhoneNumber.Text))
            {
                string method = "sms";
                if (this.radVoice.Checked)
                {
                    method = "voice";
                }
                this.number = this.txtPhoneNumber.Text;
                this.cc = this.number.Substring(0, 2);
                this.phone = this.number.Substring(2);

                String response = String.Empty;
                String request = String.Empty;

                if (WhatsAppApi.Register.WhatsRegisterV2.RequestCode(this.number, out this.password, out request, out response, method))
                {
                    if (!string.IsNullOrEmpty(this.password))
                    {
                        //Password Received
                        this.OnReceivePassword();
                    }
                    else
                    {
                        this.grpStep1.Enabled = false;
                        this.grpStep2.Enabled = true;
                    }
                }
                else
                {
                    txtOutput.Text = response;
                }
            }
        }

        private void btnRegisterCode_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(this.txtCode.Text) && this.txtCode.Text.Length == 6)
            {
                String code = this.txtCode.Text;
                String response = String.Empty;

                this.password = WhatsAppApi.Register.WhatsRegisterV2.RegisterCode(this.number, code, out response);
                if (!String.IsNullOrEmpty(this.password))
                {
                    this.OnReceivePassword();
                }
            }
        }

        private void OnReceivePassword()
        {
            this.txtOutput.Text = String.Format("Found password:\r\n{0}\r\n\r\nWrite it down and exit the program", this.password);
            this.grpStep1.Enabled = false;
            this.grpStep2.Enabled = false;
            this.grpResult.Enabled = true;
        }

        private void btnDone_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}