namespace WhatsAppPort
{
    partial class FrmAddUser
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtBxPhoneNum = new System.Windows.Forms.TextBox();
            this.lblPhoneNum = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.buttonAbort = new System.Windows.Forms.Button();
            this.lblNick = new System.Windows.Forms.Label();
            this.txtBxNick = new System.Windows.Forms.TextBox();
            this.groupBoxUserInfo = new System.Windows.Forms.GroupBox();
            this.groupBoxUserInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtBxPhoneNum
            // 
            this.txtBxPhoneNum.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBxPhoneNum.Location = new System.Drawing.Point(104, 19);
            this.txtBxPhoneNum.Name = "txtBxPhoneNum";
            this.txtBxPhoneNum.Size = new System.Drawing.Size(218, 20);
            this.txtBxPhoneNum.TabIndex = 0;
            // 
            // lblPhoneNum
            // 
            this.lblPhoneNum.AutoSize = true;
            this.lblPhoneNum.Location = new System.Drawing.Point(11, 22);
            this.lblPhoneNum.Name = "lblPhoneNum";
            this.lblPhoneNum.Size = new System.Drawing.Size(87, 13);
            this.lblPhoneNum.TabIndex = 1;
            this.lblPhoneNum.Text = "Mobile Number : ";
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(265, 94);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // buttonAbort
            // 
            this.buttonAbort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonAbort.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonAbort.Location = new System.Drawing.Point(12, 94);
            this.buttonAbort.Name = "buttonAbort";
            this.buttonAbort.Size = new System.Drawing.Size(75, 23);
            this.buttonAbort.TabIndex = 2;
            this.buttonAbort.Text = "Cancel";
            this.buttonAbort.UseVisualStyleBackColor = true;
            this.buttonAbort.Click += new System.EventHandler(this.buttonAbort_Click);
            // 
            // lblNick
            // 
            this.lblNick.AutoSize = true;
            this.lblNick.Location = new System.Drawing.Point(31, 48);
            this.lblNick.Name = "lblNick";
            this.lblNick.Size = new System.Drawing.Size(63, 13);
            this.lblNick.TabIndex = 1;
            this.lblNick.Text = "NickName :";
            // 
            // txtBxNick
            // 
            this.txtBxNick.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBxNick.Location = new System.Drawing.Point(104, 45);
            this.txtBxNick.Name = "txtBxNick";
            this.txtBxNick.Size = new System.Drawing.Size(218, 20);
            this.txtBxNick.TabIndex = 1;
            // 
            // groupBoxUserInfo
            // 
            this.groupBoxUserInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxUserInfo.Controls.Add(this.lblPhoneNum);
            this.groupBoxUserInfo.Controls.Add(this.txtBxPhoneNum);
            this.groupBoxUserInfo.Controls.Add(this.txtBxNick);
            this.groupBoxUserInfo.Controls.Add(this.lblNick);
            this.groupBoxUserInfo.Location = new System.Drawing.Point(12, 12);
            this.groupBoxUserInfo.Name = "groupBoxUserInfo";
            this.groupBoxUserInfo.Size = new System.Drawing.Size(328, 76);
            this.groupBoxUserInfo.TabIndex = 0;
            this.groupBoxUserInfo.TabStop = false;
            this.groupBoxUserInfo.Text = "User Info";
            // 
            // FrmAddUser
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonAbort;
            this.ClientSize = new System.Drawing.Size(352, 129);
            this.Controls.Add(this.groupBoxUserInfo);
            this.Controls.Add(this.buttonAbort);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "FrmAddUser";
            this.Text = "Add User";
            this.groupBoxUserInfo.ResumeLayout(false);
            this.groupBoxUserInfo.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox txtBxPhoneNum;
        private System.Windows.Forms.Label lblPhoneNum;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button buttonAbort;
        private System.Windows.Forms.Label lblNick;
        private System.Windows.Forms.TextBox txtBxNick;
        private System.Windows.Forms.GroupBox groupBoxUserInfo;
    }
}