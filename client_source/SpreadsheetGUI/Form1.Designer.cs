namespace SpreadsheetGUI
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
            this.SpreadsheetPanel = new SS.SpreadsheetPanel();
            this.CellContents = new System.Windows.Forms.Label();
            this.ContentsBox = new System.Windows.Forms.TextBox();
            this.ConfirmButton = new System.Windows.Forms.Button();
            this.CellNameLabel = new System.Windows.Forms.Label();
            this.fileMenu = new System.Windows.Forms.MenuStrip();
            this.menu = new System.Windows.Forms.ToolStripMenuItem();
            this.closeButton = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CellSelectionHelpButton = new System.Windows.Forms.ToolStripMenuItem();
            this.ChangeContentsHelpButton = new System.Windows.Forms.ToolStripMenuItem();
            this.SpecialFeatureHelpButton = new System.Windows.Forms.ToolStripMenuItem();
            this.SeeUsers = new System.Windows.Forms.ToolStripMenuItem();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.Revert = new System.Windows.Forms.Button();
            this.Undo = new System.Windows.Forms.Button();
            this.fileMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // SpreadsheetPanel
            // 
            this.SpreadsheetPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SpreadsheetPanel.BackColor = System.Drawing.Color.LightBlue;
            this.SpreadsheetPanel.Font = new System.Drawing.Font("Bahnschrift", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SpreadsheetPanel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.SpreadsheetPanel.Location = new System.Drawing.Point(0, 124);
            this.SpreadsheetPanel.Margin = new System.Windows.Forms.Padding(4);
            this.SpreadsheetPanel.Name = "SpreadsheetPanel";
            this.SpreadsheetPanel.Size = new System.Drawing.Size(1067, 430);
            this.SpreadsheetPanel.TabIndex = 0;
            // 
            // CellContents
            // 
            this.CellContents.AutoSize = true;
            this.CellContents.Font = new System.Drawing.Font("Bahnschrift Light Condensed", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CellContents.Location = new System.Drawing.Point(65, 81);
            this.CellContents.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.CellContents.Name = "CellContents";
            this.CellContents.Size = new System.Drawing.Size(100, 24);
            this.CellContents.TabIndex = 1;
            this.CellContents.Text = "Cell Contents:";
            // 
            // ContentsBox
            // 
            this.ContentsBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ContentsBox.Font = new System.Drawing.Font("Bahnschrift", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ContentsBox.Location = new System.Drawing.Point(216, 79);
            this.ContentsBox.Margin = new System.Windows.Forms.Padding(4);
            this.ContentsBox.Name = "ContentsBox";
            this.ContentsBox.Size = new System.Drawing.Size(684, 32);
            this.ContentsBox.TabIndex = 2;
            // 
            // ConfirmButton
            // 
            this.ConfirmButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ConfirmButton.BackColor = System.Drawing.Color.LightBlue;
            this.ConfirmButton.Font = new System.Drawing.Font("Bahnschrift Light Condensed", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConfirmButton.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ConfirmButton.Location = new System.Drawing.Point(931, 76);
            this.ConfirmButton.Margin = new System.Windows.Forms.Padding(4);
            this.ConfirmButton.Name = "ConfirmButton";
            this.ConfirmButton.Size = new System.Drawing.Size(120, 39);
            this.ConfirmButton.TabIndex = 3;
            this.ConfirmButton.Text = "Confirm";
            this.ConfirmButton.UseVisualStyleBackColor = false;
            this.ConfirmButton.Click += new System.EventHandler(this.ConfirmButton_Click);
            // 
            // CellNameLabel
            // 
            this.CellNameLabel.AutoSize = true;
            this.CellNameLabel.Font = new System.Drawing.Font("Bahnschrift Light Condensed", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CellNameLabel.Location = new System.Drawing.Point(17, 81);
            this.CellNameLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.CellNameLabel.Name = "CellNameLabel";
            this.CellNameLabel.Size = new System.Drawing.Size(24, 24);
            this.CellNameLabel.TabIndex = 4;
            this.CellNameLabel.Text = "A1";
            // 
            // fileMenu
            // 
            this.fileMenu.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.fileMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menu,
            this.helpToolStripMenuItem,
            this.SeeUsers});
            this.fileMenu.Location = new System.Drawing.Point(0, 0);
            this.fileMenu.Name = "fileMenu";
            this.fileMenu.Padding = new System.Windows.Forms.Padding(4, 1, 0, 1);
            this.fileMenu.Size = new System.Drawing.Size(1067, 26);
            this.fileMenu.TabIndex = 5;
            this.fileMenu.Text = "File";
            // 
            // menu
            // 
            this.menu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.closeButton});
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(46, 24);
            this.menu.Text = "File";
            // 
            // closeButton
            // 
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(128, 26);
            this.closeButton.Text = "Close";
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CellSelectionHelpButton,
            this.ChangeContentsHelpButton,
            this.SpecialFeatureHelpButton});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(55, 24);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // CellSelectionHelpButton
            // 
            this.CellSelectionHelpButton.Name = "CellSelectionHelpButton";
            this.CellSelectionHelpButton.Size = new System.Drawing.Size(203, 26);
            this.CellSelectionHelpButton.Text = "Cell selection";
            this.CellSelectionHelpButton.Click += new System.EventHandler(this.CellSelectionHelpButton_Click);
            // 
            // ChangeContentsHelpButton
            // 
            this.ChangeContentsHelpButton.Name = "ChangeContentsHelpButton";
            this.ChangeContentsHelpButton.Size = new System.Drawing.Size(203, 26);
            this.ChangeContentsHelpButton.Text = "Change contents";
            this.ChangeContentsHelpButton.Click += new System.EventHandler(this.ChangeContentsHelpButton_Click);
            // 
            // SpecialFeatureHelpButton
            // 
            this.SpecialFeatureHelpButton.Name = "SpecialFeatureHelpButton";
            this.SpecialFeatureHelpButton.Size = new System.Drawing.Size(203, 26);
            this.SpecialFeatureHelpButton.Text = "Special feature!!!";
            this.SpecialFeatureHelpButton.Click += new System.EventHandler(this.SpecialFeatureHelpButton_Click);
            // 
            // SeeUsers
            // 
            this.SeeUsers.Name = "SeeUsers";
            this.SeeUsers.Size = new System.Drawing.Size(84, 24);
            this.SeeUsers.Text = "See users";
            this.SeeUsers.Click += new System.EventHandler(this.seeUsers_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Bahnschrift SemiBold Condensed", 22.125F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(13, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(173, 45);
            this.label1.TabIndex = 6;
            this.label1.Text = "Spreadsheet";
            // 
            // Revert
            // 
            this.Revert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Revert.BackColor = System.Drawing.Color.LightBlue;
            this.Revert.Font = new System.Drawing.Font("Bahnschrift Light Condensed", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Revert.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.Revert.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Revert.Location = new System.Drawing.Point(780, 36);
            this.Revert.Margin = new System.Windows.Forms.Padding(4);
            this.Revert.Name = "Revert";
            this.Revert.Size = new System.Drawing.Size(120, 39);
            this.Revert.TabIndex = 7;
            this.Revert.Text = "Revert";
            this.Revert.UseVisualStyleBackColor = false;
            this.Revert.Click += new System.EventHandler(this.Revert_Click);
            // 
            // Undo
            // 
            this.Undo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Undo.BackColor = System.Drawing.Color.LightBlue;
            this.Undo.Font = new System.Drawing.Font("Bahnschrift Light Condensed", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Undo.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.Undo.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Undo.Location = new System.Drawing.Point(652, 36);
            this.Undo.Margin = new System.Windows.Forms.Padding(4);
            this.Undo.Name = "Undo";
            this.Undo.Size = new System.Drawing.Size(120, 39);
            this.Undo.TabIndex = 8;
            this.Undo.Text = "Undo";
            this.Undo.UseVisualStyleBackColor = false;
            this.Undo.Click += new System.EventHandler(this.Undo_Click);
            // 
            // Form1
            // 
            this.AcceptButton = this.ConfirmButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ClientSize = new System.Drawing.Size(1067, 554);
            this.Controls.Add(this.Undo);
            this.Controls.Add(this.Revert);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CellNameLabel);
            this.Controls.Add(this.ConfirmButton);
            this.Controls.Add(this.ContentsBox);
            this.Controls.Add(this.CellContents);
            this.Controls.Add(this.SpreadsheetPanel);
            this.Controls.Add(this.fileMenu);
            this.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.MainMenuStrip = this.fileMenu;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "Form1";
            this.fileMenu.ResumeLayout(false);
            this.fileMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private SS.SpreadsheetPanel SpreadsheetPanel;
        private System.Windows.Forms.Label CellContents;
        private System.Windows.Forms.TextBox ContentsBox;
        private System.Windows.Forms.Button ConfirmButton;
        private System.Windows.Forms.Label CellNameLabel;
        private System.Windows.Forms.MenuStrip fileMenu;
        private System.Windows.Forms.ToolStripMenuItem menu;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ToolStripMenuItem CellSelectionHelpButton;
        private System.Windows.Forms.ToolStripMenuItem ChangeContentsHelpButton;
        private System.Windows.Forms.ToolStripMenuItem SpecialFeatureHelpButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button Revert;
        private System.Windows.Forms.Button Undo;
        private System.Windows.Forms.ToolStripMenuItem SeeUsers;
    }
}

