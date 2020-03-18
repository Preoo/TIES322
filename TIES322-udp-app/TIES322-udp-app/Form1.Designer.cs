namespace TIES322_udp_app
{
    partial class Form1
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
                client.Dispose();
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.button_cancel = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_Client = new System.Windows.Forms.TextBox();
            this.numericUpDown_propbiterror = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_delaypacket = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_droppacket = new System.Windows.Forms.NumericUpDown();
            this.groupBoxProtocolBox = new System.Windows.Forms.GroupBox();
            this.radioButton_selective = new System.Windows.Forms.RadioButton();
            this.radioButton_gobackn = new System.Windows.Forms.RadioButton();
            this.radioButton_rdt_v3 = new System.Windows.Forms.RadioButton();
            this.radioButton_neqACK = new System.Windows.Forms.RadioButton();
            this.radioButton_posACK = new System.Windows.Forms.RadioButton();
            this.radioButton_posneqACK = new System.Windows.Forms.RadioButton();
            this.sendButton = new System.Windows.Forms.Button();
            this.textBox_msg = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox_serverport = new System.Windows.Forms.TextBox();
            this.textBox_clientport = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Server = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_propbiterror)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_delaypacket)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_droppacket)).BeginInit();
            this.groupBoxProtocolBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.button_cancel);
            this.splitContainer1.Panel1.Controls.Add(this.label3);
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.textBox_Client);
            this.splitContainer1.Panel1.Controls.Add(this.numericUpDown_propbiterror);
            this.splitContainer1.Panel1.Controls.Add(this.numericUpDown_delaypacket);
            this.splitContainer1.Panel1.Controls.Add(this.numericUpDown_droppacket);
            this.splitContainer1.Panel1.Controls.Add(this.groupBoxProtocolBox);
            this.splitContainer1.Panel1.Controls.Add(this.sendButton);
            this.splitContainer1.Panel1.Controls.Add(this.textBox_msg);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.button2);
            this.splitContainer1.Panel2.Controls.Add(this.button1);
            this.splitContainer1.Panel2.Controls.Add(this.textBox_serverport);
            this.splitContainer1.Panel2.Controls.Add(this.textBox_clientport);
            this.splitContainer1.Panel2.Controls.Add(this.label4);
            this.splitContainer1.Panel2.Controls.Add(this.textBox_Server);
            this.splitContainer1.Size = new System.Drawing.Size(745, 427);
            this.splitContainer1.SplitterDistance = 379;
            this.splitContainer1.TabIndex = 0;
            // 
            // button_cancel
            // 
            this.button_cancel.Enabled = false;
            this.button_cancel.Location = new System.Drawing.Point(299, 382);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(69, 23);
            this.button_cancel.TabIndex = 10;
            this.button_cancel.Text = "cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(219, 335);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(86, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Biterror % in data";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(222, 308);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Delay up to (s)";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(229, 281);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Packet drop %";
            // 
            // textBox_Client
            // 
            this.textBox_Client.Location = new System.Drawing.Point(13, 13);
            this.textBox_Client.Multiline = true;
            this.textBox_Client.Name = "textBox_Client";
            this.textBox_Client.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Client.Size = new System.Drawing.Size(340, 237);
            this.textBox_Client.TabIndex = 6;
            // 
            // numericUpDown_propbiterror
            // 
            this.numericUpDown_propbiterror.Location = new System.Drawing.Point(311, 333);
            this.numericUpDown_propbiterror.Name = "numericUpDown_propbiterror";
            this.numericUpDown_propbiterror.Size = new System.Drawing.Size(57, 20);
            this.numericUpDown_propbiterror.TabIndex = 5;
            this.numericUpDown_propbiterror.ValueChanged += new System.EventHandler(this.numericUpDown_propbiterror_ValueChanged);
            // 
            // numericUpDown_delaypacket
            // 
            this.numericUpDown_delaypacket.Location = new System.Drawing.Point(311, 306);
            this.numericUpDown_delaypacket.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown_delaypacket.Name = "numericUpDown_delaypacket";
            this.numericUpDown_delaypacket.Size = new System.Drawing.Size(57, 20);
            this.numericUpDown_delaypacket.TabIndex = 4;
            this.numericUpDown_delaypacket.ValueChanged += new System.EventHandler(this.numericUpDown_delaypacket_ValueChanged);
            // 
            // numericUpDown_droppacket
            // 
            this.numericUpDown_droppacket.Location = new System.Drawing.Point(311, 279);
            this.numericUpDown_droppacket.Name = "numericUpDown_droppacket";
            this.numericUpDown_droppacket.Size = new System.Drawing.Size(57, 20);
            this.numericUpDown_droppacket.TabIndex = 3;
            this.numericUpDown_droppacket.ValueChanged += new System.EventHandler(this.numericUpDown_droppacket_ValueChanged);
            // 
            // groupBoxProtocolBox
            // 
            this.groupBoxProtocolBox.Controls.Add(this.radioButton_selective);
            this.groupBoxProtocolBox.Controls.Add(this.radioButton_gobackn);
            this.groupBoxProtocolBox.Controls.Add(this.radioButton_rdt_v3);
            this.groupBoxProtocolBox.Controls.Add(this.radioButton_neqACK);
            this.groupBoxProtocolBox.Controls.Add(this.radioButton_posACK);
            this.groupBoxProtocolBox.Controls.Add(this.radioButton_posneqACK);
            this.groupBoxProtocolBox.Enabled = false;
            this.groupBoxProtocolBox.Location = new System.Drawing.Point(12, 279);
            this.groupBoxProtocolBox.Name = "groupBoxProtocolBox";
            this.groupBoxProtocolBox.Size = new System.Drawing.Size(200, 100);
            this.groupBoxProtocolBox.TabIndex = 2;
            this.groupBoxProtocolBox.TabStop = false;
            this.groupBoxProtocolBox.Text = "Protocol selection";
            this.groupBoxProtocolBox.Enter += new System.EventHandler(this.groupBoxProtocolBox_Enter);
            // 
            // radioButton_selective
            // 
            this.radioButton_selective.AutoSize = true;
            this.radioButton_selective.Location = new System.Drawing.Point(110, 68);
            this.radioButton_selective.Name = "radioButton_selective";
            this.radioButton_selective.Size = new System.Drawing.Size(69, 17);
            this.radioButton_selective.TabIndex = 5;
            this.radioButton_selective.TabStop = true;
            this.radioButton_selective.Text = "Selective";
            this.radioButton_selective.UseVisualStyleBackColor = true;
            this.radioButton_selective.CheckedChanged += new System.EventHandler(this.radioButton_selective_CheckedChanged);
            // 
            // radioButton_gobackn
            // 
            this.radioButton_gobackn.AutoSize = true;
            this.radioButton_gobackn.Location = new System.Drawing.Point(110, 44);
            this.radioButton_gobackn.Name = "radioButton_gobackn";
            this.radioButton_gobackn.Size = new System.Drawing.Size(78, 17);
            this.radioButton_gobackn.TabIndex = 4;
            this.radioButton_gobackn.TabStop = true;
            this.radioButton_gobackn.Text = "Go-Back-N";
            this.radioButton_gobackn.UseVisualStyleBackColor = true;
            this.radioButton_gobackn.CheckedChanged += new System.EventHandler(this.radioButton_gobackn_CheckedChanged);
            // 
            // radioButton_rdt_v3
            // 
            this.radioButton_rdt_v3.AutoSize = true;
            this.radioButton_rdt_v3.Location = new System.Drawing.Point(110, 20);
            this.radioButton_rdt_v3.Name = "radioButton_rdt_v3";
            this.radioButton_rdt_v3.Size = new System.Drawing.Size(52, 17);
            this.radioButton_rdt_v3.TabIndex = 3;
            this.radioButton_rdt_v3.TabStop = true;
            this.radioButton_rdt_v3.Text = "rdt3.0";
            this.radioButton_rdt_v3.UseVisualStyleBackColor = true;
            this.radioButton_rdt_v3.CheckedChanged += new System.EventHandler(this.radioButton_rdt_v3_CheckedChanged);
            // 
            // radioButton_neqACK
            // 
            this.radioButton_neqACK.AutoSize = true;
            this.radioButton_neqACK.Location = new System.Drawing.Point(7, 68);
            this.radioButton_neqACK.Name = "radioButton_neqACK";
            this.radioButton_neqACK.Size = new System.Drawing.Size(98, 17);
            this.radioButton_neqACK.TabIndex = 2;
            this.radioButton_neqACK.TabStop = true;
            this.radioButton_neqACK.Text = "NACK with Seq";
            this.radioButton_neqACK.UseVisualStyleBackColor = true;
            this.radioButton_neqACK.CheckedChanged += new System.EventHandler(this.radioButton_neqACK_CheckedChanged);
            // 
            // radioButton_posACK
            // 
            this.radioButton_posACK.AutoSize = true;
            this.radioButton_posACK.Location = new System.Drawing.Point(7, 44);
            this.radioButton_posACK.Name = "radioButton_posACK";
            this.radioButton_posACK.Size = new System.Drawing.Size(90, 17);
            this.radioButton_posACK.TabIndex = 1;
            this.radioButton_posACK.TabStop = true;
            this.radioButton_posACK.Text = "ACK with Seq";
            this.radioButton_posACK.UseVisualStyleBackColor = true;
            this.radioButton_posACK.CheckedChanged += new System.EventHandler(this.radioButton_posACK_CheckedChanged);
            // 
            // radioButton_posneqACK
            // 
            this.radioButton_posneqACK.AutoSize = true;
            this.radioButton_posneqACK.Location = new System.Drawing.Point(7, 20);
            this.radioButton_posneqACK.Name = "radioButton_posneqACK";
            this.radioButton_posneqACK.Size = new System.Drawing.Size(76, 17);
            this.radioButton_posneqACK.TabIndex = 0;
            this.radioButton_posneqACK.TabStop = true;
            this.radioButton_posneqACK.Text = "Ack/NAck";
            this.radioButton_posneqACK.UseVisualStyleBackColor = true;
            this.radioButton_posneqACK.CheckedChanged += new System.EventHandler(this.radioButton_posneqACK_CheckedChanged);
            // 
            // sendButton
            // 
            this.sendButton.Enabled = false;
            this.sendButton.Location = new System.Drawing.Point(218, 382);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(75, 23);
            this.sendButton.TabIndex = 1;
            this.sendButton.Text = "send";
            this.sendButton.UseVisualStyleBackColor = true;
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // textBox_msg
            // 
            this.textBox_msg.Location = new System.Drawing.Point(12, 385);
            this.textBox_msg.Name = "textBox_msg";
            this.textBox_msg.Size = new System.Drawing.Size(200, 20);
            this.textBox_msg.TabIndex = 0;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(289, 387);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(60, 23);
            this.button2.TabIndex = 5;
            this.button2.Text = "Swap";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(208, 387);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "Start";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox_serverport
            // 
            this.textBox_serverport.Location = new System.Drawing.Point(164, 387);
            this.textBox_serverport.Name = "textBox_serverport";
            this.textBox_serverport.Size = new System.Drawing.Size(38, 20);
            this.textBox_serverport.TabIndex = 3;
            this.textBox_serverport.Text = "8080";
            // 
            // textBox_clientport
            // 
            this.textBox_clientport.Location = new System.Drawing.Point(113, 387);
            this.textBox_clientport.Name = "textBox_clientport";
            this.textBox_clientport.Size = new System.Drawing.Size(45, 20);
            this.textBox_clientport.TabIndex = 2;
            this.textBox_clientport.Text = "8081";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 391);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(91, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "Ports client server";
            // 
            // textBox_Server
            // 
            this.textBox_Server.Location = new System.Drawing.Point(4, 13);
            this.textBox_Server.Multiline = true;
            this.textBox_Server.Name = "textBox_Server";
            this.textBox_Server.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Server.Size = new System.Drawing.Size(337, 366);
            this.textBox_Server.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(745, 427);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.Text = "Reliability over UDP test";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_propbiterror)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_delaypacket)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_droppacket)).EndInit();
            this.groupBoxProtocolBox.ResumeLayout(false);
            this.groupBoxProtocolBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox groupBoxProtocolBox;
        private System.Windows.Forms.RadioButton radioButton_selective;
        private System.Windows.Forms.RadioButton radioButton_gobackn;
        private System.Windows.Forms.RadioButton radioButton_rdt_v3;
        private System.Windows.Forms.RadioButton radioButton_neqACK;
        private System.Windows.Forms.RadioButton radioButton_posACK;
        private System.Windows.Forms.RadioButton radioButton_posneqACK;
        private System.Windows.Forms.Button sendButton;
        private System.Windows.Forms.TextBox textBox_msg;
        private System.Windows.Forms.NumericUpDown numericUpDown_delaypacket;
        private System.Windows.Forms.NumericUpDown numericUpDown_droppacket;
        private System.Windows.Forms.TextBox textBox_Client;
        private System.Windows.Forms.NumericUpDown numericUpDown_propbiterror;
        private System.Windows.Forms.TextBox textBox_Server;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_serverport;
        private System.Windows.Forms.TextBox textBox_clientport;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button_cancel;
    }
}

