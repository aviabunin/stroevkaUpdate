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
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.progressStatusLabel1 = new System.Windows.Forms.Label();
            this.progressStatusLabel2 = new System.Windows.Forms.Label();
            this.cataloglabel = new System.Windows.Forms.Label();
            this.подробно = new System.Windows.Forms.CheckBox();
            this.cmbDisk = new System.Windows.Forms.ComboBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.labelDisk = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(97, 9);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(246, 23);
            this.progressBar1.TabIndex = 1;
            // 
            // progressStatusLabel1
            // 
            this.progressStatusLabel1.BackColor = System.Drawing.Color.Silver;
            this.progressStatusLabel1.Location = new System.Drawing.Point(13, 8);
            this.progressStatusLabel1.Name = "progressStatusLabel1";
            this.progressStatusLabel1.Size = new System.Drawing.Size(78, 23);
            this.progressStatusLabel1.TabIndex = 6;
            this.progressStatusLabel1.Text = "Проценты";
            this.progressStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // progressStatusLabel2
            // 
            this.progressStatusLabel2.AutoSize = true;
            this.progressStatusLabel2.Location = new System.Drawing.Point(15, 49);
            this.progressStatusLabel2.Name = "progressStatusLabel2";
            this.progressStatusLabel2.Size = new System.Drawing.Size(40, 13);
            this.progressStatusLabel2.TabIndex = 7;
            this.progressStatusLabel2.Text = "Файл: ";
            // 
            // cataloglabel
            // 
            this.cataloglabel.AutoSize = true;
            this.cataloglabel.Location = new System.Drawing.Point(80, 49);
            this.cataloglabel.Name = "cataloglabel";
            this.cataloglabel.Size = new System.Drawing.Size(174, 13);
            this.cataloglabel.TabIndex = 8;
            this.cataloglabel.Text = "Копируем в каталог программы :";
            // 
            // подробно
            // 
            this.подробно.AutoSize = true;
            this.подробно.Location = new System.Drawing.Point(444, 45);
            this.подробно.Name = "подробно";
            this.подробно.Size = new System.Drawing.Size(76, 17);
            this.подробно.TabIndex = 9;
            this.подробно.Text = "Подробно";
            this.подробно.UseVisualStyleBackColor = true;
            // 
            // cmbDisk
            // 
            this.cmbDisk.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDisk.FormattingEnabled = true;
            this.cmbDisk.Items.AddRange(new object[] {
            "D:\\"});
            this.cmbDisk.Location = new System.Drawing.Point(80, 70);
            this.cmbDisk.Name = "cmbDisk";
            this.cmbDisk.Size = new System.Drawing.Size(100, 21);
            this.cmbDisk.TabIndex = 12;
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(440, 70);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(80, 23);
            this.btnUpdate.TabIndex = 13;
            this.btnUpdate.Text = "Обновить программу";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // labelDisk
            // 
            this.labelDisk.AutoSize = true;
            this.labelDisk.Location = new System.Drawing.Point(13, 76);
            this.labelDisk.Name = "labelDisk";
            this.labelDisk.Size = new System.Drawing.Size(36, 13);
            this.labelDisk.TabIndex = 11;
            this.labelDisk.Text = "Диск:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(532, 120);
            this.Controls.Add(this.labelDisk);
            this.Controls.Add(this.cmbDisk);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.подробно);
            this.Controls.Add(this.cataloglabel);
            this.Controls.Add(this.progressStatusLabel2);
            this.Controls.Add(this.progressStatusLabel1);
            this.Controls.Add(this.progressBar1);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Обновление строевки";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label progressStatusLabel1;
        private System.Windows.Forms.Label progressStatusLabel2;
        private System.Windows.Forms.Label cataloglabel;
        private System.Windows.Forms.CheckBox подробно;


        private System.Windows.Forms.ComboBox cmbDisk;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Label labelDisk;

    }
}

