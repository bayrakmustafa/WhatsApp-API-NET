using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using WhatsAppApi;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;
using WhatsAppApi.Response;

namespace WhatsAppPort
{
    public partial class FrmForm : Form
    {
        //private WhatsMessageHandler _MessageHandler;

        private BackgroundWorker bgWorker;
        private volatile bool isRunning;
        private Dictionary<string, User> userList;

        private string phoneNum;
        private string phonePass;
        private string phoneNick;

        private User phoneUser;

        public FrmForm(string num, string pass, string nick)
        {
            this.phoneNum = num;
            this.phonePass = pass;
            this.phoneNick = nick;

            this.phoneUser = new User(this.phoneNum, this.phoneNick);

            InitializeComponent();

            this.userList = new Dictionary<string, User>();
            this.isRunning = true;

            this.bgWorker = new BackgroundWorker();
            this.bgWorker.DoWork += ProcessMessages;
            this.bgWorker.ProgressChanged += NewMessageArrived;
            this.bgWorker.RunWorkerCompleted += RunWorkerCompleted;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.WorkerReportsProgress = true;

            //this._MessageHandler = WhatsMessageHandler.Instance;
        }

        private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.isRunning = false;
        }

        private void btnAddContact_Click(object sender, EventArgs e)
        {
            using (FrmAddUser tmpAddUser = new FrmAddUser())
            {
                tmpAddUser.ShowDialog(this);
                if (tmpAddUser.DialogResult != DialogResult.OK)
                    return;
                if (tmpAddUser.Tag == null || !(tmpAddUser.Tag is User))
                    return;

                User tmpUser = tmpAddUser.Tag as User;
                this.userList.Add(tmpUser.PhoneNumber, tmpUser);

                ListViewItem tmpListUser = new ListViewItem(tmpUser.UserName)
                {
                    Tag = tmpUser
                };
                this.listViewContacts.Items.Add(tmpListUser);
            }
        }

        private void FrmForm_Load(object sender, EventArgs e)
        {
            WhatSocket.Instance.SendGetServerProperties();

            WhatSocket.Instance.Connect();
            WhatSocket.Instance.Login();     

            //Run Worker
            this.bgWorker.RunWorkerAsync();
        }

        private void ProcessMessages(object sender, DoWorkEventArgs args)
        {
            if (sender == null)
                return;

            while (this.isRunning)
            {
                if (!WhatSocket.Instance.HasMessages())
                {
                    WhatSocket.Instance.PollMessage();
                    Thread.Sleep(100);
                    continue;
                }

                ProtocolTreeNode[] tmpMessages = WhatSocket.Instance.GetAllMessages();
                (sender as BackgroundWorker).ReportProgress(1, tmpMessages);
            }
        }

        private void NewMessageArrived(object sender, ProgressChangedEventArgs args)
        {
            if (args.UserState == null || !(args.UserState is ProtocolTreeNode[]))
                return;

            ProtocolTreeNode[] tmpMessages = args.UserState as ProtocolTreeNode[];
            foreach (ProtocolTreeNode msg in tmpMessages)
            {
                ProtocolTreeNode contentNode = msg.GetChild("body") ?? msg.GetChild("enc");
                byte[] contentData = contentNode.GetData();
                String message = WhatsApp.SysEncoding.GetString(contentData);

                FMessage tmpMessage = new FMessage(new FMessage.FMessageIdentifierKey(msg.GetAttribute("from"), false, msg.GetAttribute("id")));
                tmpMessage.binary_data = contentData;
                tmpMessage.data = message;

                WhatsEventHandler.OnMessageRecievedEventHandler(tmpMessage);
            }
        }

        private void FrmForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.isRunning = false;
            this.bgWorker.CancelAsync();
            this.bgWorker = null;
        }

        private void listViewContacts_DoubleClick(object sender, EventArgs e)
        {
            if (sender == null || !(sender is ListView))
                return;

            ListView tmpListView = sender as ListView;
            if (tmpListView.SelectedItems.Count == 0)
                return;

            ListViewItem selItem = tmpListView.SelectedItems[0];
            User tmpUser = selItem.Tag as User;

            FrmUserChat tmpDialog = new FrmUserChat(phoneUser, tmpUser);
            tmpDialog.Show();
        }

        private void timerCheck_Tick(object sender, EventArgs e)
        {
            if (this.bgWorker != null && !this.isRunning)
            {
                this.bgWorker.RunWorkerAsync();
            }
        }
    }
}