namespace Converter {
    partial class FrmConverter {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.tbxUnitSystem = new System.Windows.Forms.TextBox();
            this.btLoad = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.cbxDomains = new System.Windows.Forms.ComboBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lbxFrom = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.lbxTo = new System.Windows.Forms.ListBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbxVal = new System.Windows.Forms.TextBox();
            this.lbResult = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tbxFrom = new System.Windows.Forms.TextBox();
            this.tbxTo = new System.Windows.Forms.TextBox();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Unit System:";
            // 
            // tbxUnitSystem
            // 
            this.tbxUnitSystem.Location = new System.Drawing.Point(84, 36);
            this.tbxUnitSystem.Name = "tbxUnitSystem";
            this.tbxUnitSystem.ReadOnly = true;
            this.tbxUnitSystem.Size = new System.Drawing.Size(289, 20);
            this.tbxUnitSystem.TabIndex = 10;
            this.tbxUnitSystem.Text = "<Not loaded>";
            // 
            // btLoad
            // 
            this.btLoad.Location = new System.Drawing.Point(384, 34);
            this.btLoad.Name = "btLoad";
            this.btLoad.Size = new System.Drawing.Size(67, 23);
            this.btLoad.TabIndex = 1;
            this.btLoad.Text = "Load";
            this.toolTip1.SetToolTip(this.btLoad, "Load units system from XML file");
            this.btLoad.UseVisualStyleBackColor = true;
            this.btLoad.Click += new System.EventHandler(this.btLoad_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Domain:";
            // 
            // cbxDomains
            // 
            this.cbxDomains.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxDomains.FormattingEnabled = true;
            this.cbxDomains.Location = new System.Drawing.Point(84, 71);
            this.cbxDomains.Name = "cbxDomains";
            this.cbxDomains.Size = new System.Drawing.Size(141, 21);
            this.cbxDomains.TabIndex = 4;
            this.cbxDomains.SelectedIndexChanged += new System.EventHandler(this.cbxDomains_SelectedIndexChanged);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(463, 24);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadToolStripMenuItem
            // 
            this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            this.loadToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.loadToolStripMenuItem.Text = "Load";
            this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // lbxFrom
            // 
            this.lbxFrom.FormattingEnabled = true;
            this.lbxFrom.Location = new System.Drawing.Point(15, 143);
            this.lbxFrom.Name = "lbxFrom";
            this.lbxFrom.Size = new System.Drawing.Size(200, 199);
            this.lbxFrom.Sorted = true;
            this.lbxFrom.TabIndex = 6;
            this.lbxFrom.SelectedIndexChanged += new System.EventHandler(this.lbxFrom_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 114);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "From:";
            // 
            // lbxTo
            // 
            this.lbxTo.FormattingEnabled = true;
            this.lbxTo.Location = new System.Drawing.Point(251, 143);
            this.lbxTo.Name = "lbxTo";
            this.lbxTo.Size = new System.Drawing.Size(200, 199);
            this.lbxTo.Sorted = true;
            this.lbxTo.TabIndex = 8;
            this.lbxTo.SelectedIndexChanged += new System.EventHandler(this.lbxTo_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(248, 114);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(23, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "To:";
            // 
            // tbxVal
            // 
            this.tbxVal.Location = new System.Drawing.Point(55, 362);
            this.tbxVal.Name = "tbxVal";
            this.tbxVal.Size = new System.Drawing.Size(63, 20);
            this.tbxVal.TabIndex = 2;
            this.toolTip1.SetToolTip(this.tbxVal, "Value to be converted");
            this.tbxVal.TextChanged += new System.EventHandler(this.tbxVal_TextChanged);
            // 
            // lbResult
            // 
            this.lbResult.AutoSize = true;
            this.lbResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbResult.Location = new System.Drawing.Point(141, 363);
            this.lbResult.Name = "lbResult";
            this.lbResult.Size = new System.Drawing.Size(17, 16);
            this.lbResult.TabIndex = 11;
            this.lbResult.Text = "...";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 365);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(37, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Value:";
            // 
            // tbxFrom
            // 
            this.tbxFrom.Location = new System.Drawing.Point(51, 111);
            this.tbxFrom.Name = "tbxFrom";
            this.tbxFrom.Size = new System.Drawing.Size(164, 20);
            this.tbxFrom.TabIndex = 13;
            this.tbxFrom.TextChanged += new System.EventHandler(this.tbxFrom_TextChanged);
            // 
            // tbxTo
            // 
            this.tbxTo.Location = new System.Drawing.Point(277, 111);
            this.tbxTo.Name = "tbxTo";
            this.tbxTo.Size = new System.Drawing.Size(174, 20);
            this.tbxTo.TabIndex = 14;
            this.tbxTo.TextChanged += new System.EventHandler(this.tbxTo_TextChanged);
            // 
            // FrmConverter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(463, 461);
            this.Controls.Add(this.tbxTo);
            this.Controls.Add(this.tbxFrom);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lbResult);
            this.Controls.Add(this.tbxVal);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lbxTo);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lbxFrom);
            this.Controls.Add(this.cbxDomains);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btLoad);
            this.Controls.Add(this.tbxUnitSystem);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FrmConverter";
            this.Text = "Free UOM Converter";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbxUnitSystem;
        private System.Windows.Forms.Button btLoad;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbxDomains;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ListBox lbxFrom;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox lbxTo;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbxVal;
        private System.Windows.Forms.Label lbResult;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TextBox tbxFrom;
        private System.Windows.Forms.TextBox tbxTo;
    }
}

