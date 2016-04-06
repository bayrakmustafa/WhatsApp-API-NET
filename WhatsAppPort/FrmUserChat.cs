using System;
using System.IO;
using System.Windows.Forms;
using WhatsAppApi;
using WhatsAppApi.Parser;
using WhatsAppApi.Response;

namespace WhatsAppPort
{
    public partial class FrmUserChat : Form
    {
        private User loginUser;
        private User user;
        private bool isTyping;

        public FrmUserChat(User loginUser, User user)
        {
            InitializeComponent();

            this.loginUser = loginUser;
            this.user = user;
            this.isTyping = false;

            WhatsEventHandler.MessageRecievedEvent += WhatsEventHandlerOnMessageRecievedEvent;
            WhatsEventHandler.IsTypingEvent += WhatsEventHandlerOnIsTypingEvent;
        }

        private void WhatsEventHandlerOnIsTypingEvent(string @from, bool value)
        {
            if (!this.user.WhatsUser.GetFullJid().Equals(from))
                return;

            this.lblIsTyping.Visible = value;
        }

        private void WhatsEventHandlerOnMessageRecievedEvent(FMessage mess)
        {
            if (!this.user.WhatsUser.GetFullJid().Equals(mess.identifier_key.remote_jid))
                return;
            string tmpMes = mess.data;
            this.AddNewText(this.user.UserName, tmpMes);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (this.txtBxSentText.Text.Length == 0)
                return;

            WhatSocket.Instance.SendMessage(this.user.WhatsUser.GetFullJid(), txtBxSentText.Text);
            this.AddNewText(this.loginUser.UserName, txtBxSentText.Text);
            txtBxSentText.Clear();
        }

        private void AddNewText(string from, string text)
        {
            this.txtBxChat.AppendText(string.Format("{0}: {1}{2}", from, text, Environment.NewLine));
        }

        private void txtBxSentText_TextChanged(object sender, EventArgs e)
        {
            if (!this.isTyping)
            {
                this.isTyping = true;
                WhatSocket.Instance.SendComposing(this.user.WhatsUser.GetFullJid());
                this.timerTyping.Start();
            }
        }

        private void timerTyping_Tick(object sender, EventArgs e)
        {
            if (this.isTyping)
            {
                this.isTyping = false;
                return;
            }
            WhatSocket.Instance.SendPaused(this.user.WhatsUser.GetFullJid());
            this.timerTyping.Stop();
        }

        private void btnSendPicture_Click(object sender, EventArgs e)
        {
            FileDialog pictureDialog = new OpenFileDialog();
            pictureDialog.Title = "Select Image to Send";
            pictureDialog.Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";

            if (pictureDialog.ShowDialog() == DialogResult.OK)
            {
                String picture = pictureDialog.FileName.ToString();
                byte[] imageData = File.ReadAllBytes(picture);

                FileInfo fileInfo = new FileInfo(picture);

                ApiBase.ImageType imageType = ApiBase.ImageType.JPEG;
                switch (fileInfo.Extension)
                {
                    case "jpeg":
                        imageType = ApiBase.ImageType.JPEG;
                        break;
                    case "jpg":
                        imageType = ApiBase.ImageType.JPEG;
                        break;
                    case "png":
                        imageType = ApiBase.ImageType.PNG;
                        break;
                    case "gif":
                        imageType = ApiBase.ImageType.GIF;
                        break;
                }

                WhatSocket.Instance.SendMessageImage(this.user.WhatsUser.GetFullJid(), imageData, imageType);
            }
        }

        private void FrmUserChat_Load(object sender, EventArgs e)
        {

        }
    }
}