// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DependencyLogViewer
{
    partial class NodeForm
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
            this.nodeTitle = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dependentsListBox = new System.Windows.Forms.ListBox();
            this.exploreDependent = new System.Windows.Forms.Button();
            this.dependeesListBox = new System.Windows.Forms.ListBox();
            this.exploreDependee = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // nodeTitle
            // 
            this.nodeTitle.AutoSize = true;
            this.nodeTitle.BackColor = System.Drawing.SystemColors.Info;
            this.nodeTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.nodeTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.nodeTitle.Location = new System.Drawing.Point(0, 0);
            this.nodeTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.nodeTitle.Name = "nodeTitle";
            this.nodeTitle.Size = new System.Drawing.Size(116, 30);
            this.nodeTitle.TabIndex = 0;
            this.nodeTitle.Text = "Node Title";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 30);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dependentsListBox);
            this.splitContainer1.Panel1.Controls.Add(this.exploreDependent);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dependeesListBox);
            this.splitContainer1.Panel2.Controls.Add(this.exploreDependee);
            this.splitContainer1.Size = new System.Drawing.Size(1126, 806);
            this.splitContainer1.SplitterDistance = 419;
            this.splitContainer1.SplitterWidth = 6;
            this.splitContainer1.TabIndex = 1;
            // 
            // dependentsListBox
            // 
            this.dependentsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dependentsListBox.FormattingEnabled = true;
            this.dependentsListBox.HorizontalScrollbar = true;
            this.dependentsListBox.ItemHeight = 30;
            this.dependentsListBox.Location = new System.Drawing.Point(0, 0);
            this.dependentsListBox.Margin = new System.Windows.Forms.Padding(4);
            this.dependentsListBox.Name = "dependentsListBox";
            this.dependentsListBox.Size = new System.Drawing.Size(1126, 361);
            this.dependentsListBox.TabIndex = 0;
            this.dependentsListBox.DoubleClick += new System.EventHandler(this.exploreDependent_Click);
            // 
            // exploreDependent
            // 
            this.exploreDependent.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.exploreDependent.Location = new System.Drawing.Point(0, 361);
            this.exploreDependent.Margin = new System.Windows.Forms.Padding(4);
            this.exploreDependent.Name = "exploreDependent";
            this.exploreDependent.Size = new System.Drawing.Size(1126, 58);
            this.exploreDependent.TabIndex = 1;
            this.exploreDependent.Text = "Explore Source Node";
            this.exploreDependent.UseVisualStyleBackColor = true;
            this.exploreDependent.Click += new System.EventHandler(this.exploreDependent_Click);
            // 
            // dependeesListBox
            // 
            this.dependeesListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dependeesListBox.FormattingEnabled = true;
            this.dependeesListBox.HorizontalScrollbar = true;
            this.dependeesListBox.ItemHeight = 30;
            this.dependeesListBox.Location = new System.Drawing.Point(0, 0);
            this.dependeesListBox.Margin = new System.Windows.Forms.Padding(4);
            this.dependeesListBox.Name = "dependeesListBox";
            this.dependeesListBox.Size = new System.Drawing.Size(1126, 329);
            this.dependeesListBox.TabIndex = 0;
            this.dependeesListBox.DoubleClick += new System.EventHandler(this.exploreDependee_Click);
            // 
            // exploreDependee
            // 
            this.exploreDependee.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.exploreDependee.Location = new System.Drawing.Point(0, 329);
            this.exploreDependee.Margin = new System.Windows.Forms.Padding(4);
            this.exploreDependee.Name = "exploreDependee";
            this.exploreDependee.Size = new System.Drawing.Size(1126, 52);
            this.exploreDependee.TabIndex = 1;
            this.exploreDependee.Text = "Explore Target Node";
            this.exploreDependee.UseVisualStyleBackColor = true;
            this.exploreDependee.Click += new System.EventHandler(this.exploreDependee_Click);
            // 
            // NodeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 30F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1126, 836);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.nodeTitle);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(1090, 170);
            this.Name = "NodeForm";
            this.Text = "NodeForm";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label nodeTitle;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button exploreDependee;
        private System.Windows.Forms.ListBox dependeesListBox;
        private System.Windows.Forms.Button exploreDependent;
        private System.Windows.Forms.ListBox dependentsListBox;
    }
}
