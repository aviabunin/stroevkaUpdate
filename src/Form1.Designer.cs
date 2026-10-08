namespace stroevkaUpdate
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
            this.labelDisk = new System.Windows.Forms.Label();
            this.cmbDisk = new System.Windows.Forms.ComboBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.cataloglabel = new System.Windows.Forms.Label();
            this.progressStatusLabel1 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.progressStatusLabel2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.labelDisk);
            this.splitContainer1.Panel1.Controls.Add(this.cmbDisk);
            this.splitContainer1.Panel1.Controls.Add(this.btnUpdate);
            this.splitContainer1.Panel1.Controls.Add(this.cataloglabel);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.progressStatusLabel1);
            this.splitContainer1.Panel2.Controls.Add(this.progressBar1);
            this.splitContainer1.Panel2.Controls.Add(this.progressStatusLabel2);
            this.splitContainer1.Size = new System.Drawing.Size(431, 181);
            this.splitContainer1.SplitterDistance = 85;
            this.splitContainer1.TabIndex = 14;
            // 
            // labelDisk
            // 
            this.labelDisk.AutoSize = true;
            this.labelDisk.Location = new System.Drawing.Point(12, 40);
            this.labelDisk.Name = "labelDisk";
            this.labelDisk.Size = new System.Drawing.Size(51, 13);
            this.labelDisk.TabIndex = 15;
            this.labelDisk.Text = "На диск:";
            // 
            // cmbDisk
            // 
            this.cmbDisk.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDisk.FormattingEnabled = true;
            this.cmbDisk.Items.AddRange(new object[] {
            "D:\\"});
            this.cmbDisk.Location = new System.Drawing.Point(69, 37);
            this.cmbDisk.Name = "cmbDisk";
            this.cmbDisk.Size = new System.Drawing.Size(100, 21);
            this.cmbDisk.TabIndex = 16;
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(265, 37);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(80, 23);
            this.btnUpdate.TabIndex = 17;
            this.btnUpdate.Text = "Обновить программу";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // cataloglabel
            // 
            this.cataloglabel.AutoSize = true;
            this.cataloglabel.Location = new System.Drawing.Point(12, 9);
            this.cataloglabel.Name = "cataloglabel";
            this.cataloglabel.Size = new System.Drawing.Size(46, 13);
            this.cataloglabel.TabIndex = 14;
            this.cataloglabel.Text = "Откуда";
            // 
            // progressStatusLabel1
            // 
            this.progressStatusLabel1.BackColor = System.Drawing.Color.Silver;
            this.progressStatusLabel1.Location = new System.Drawing.Point(12, 22);
            this.progressStatusLabel1.Name = "progressStatusLabel1";
            this.progressStatusLabel1.Size = new System.Drawing.Size(71, 23);
            this.progressStatusLabel1.TabIndex = 9;
            this.progressStatusLabel1.Text = "Проценты";
            this.progressStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(96, 23);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(248, 23);
            this.progressBar1.TabIndex = 8;
            // 
            // progressStatusLabel2
            // 
            this.progressStatusLabel2.AutoSize = true;
            this.progressStatusLabel2.Location = new System.Drawing.Point(12, 58);
            this.progressStatusLabel2.Name = "progressStatusLabel2";
            this.progressStatusLabel2.Size = new System.Drawing.Size(40, 13);
            this.progressStatusLabel2.TabIndex = 10;
            this.progressStatusLabel2.Text = "Файл: ";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(431, 181);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Обновление строевки";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private SplitContainer splitContainer1;
        private Label labelDisk;
        private ComboBox cmbDisk;
        private Button btnUpdate;
        private Label cataloglabel;
        private Label progressStatusLabel1;
        private ProgressBar progressBar1;
        private Label progressStatusLabel2;
    }
}

