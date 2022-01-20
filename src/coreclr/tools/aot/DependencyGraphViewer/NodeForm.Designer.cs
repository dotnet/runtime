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
            this.exploreDependee = new System.Windows.Forms.Button();
            this.dependeesListBox = new System.Windows.Forms.ListBox();
            this.exploreDependent = new System.Windows.Forms.Button();
            this.dependentsListBox = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // nodeTitle
            // 
            this.nodeTitle.AutoSize = true;
            this.nodeTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.nodeTitle.Location = new System.Drawing.Point(0, 0);
            this.nodeTitle.Name = "nodeTitle";
            this.nodeTitle.Size = new System.Drawing.Size(51, 20);
            this.nodeTitle.TabIndex = 0;
            this.nodeTitle.Text = "label1";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 20);
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
            this.splitContainer1.Size = new System.Drawing.Size(1497, 892);
            this.splitContainer1.SplitterDistance = 464;
            this.splitContainer1.TabIndex = 1;
            // 
            // exploreDependee
            // 
            this.exploreDependee.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.exploreDependee.Location = new System.Drawing.Point(0, 389);
            this.exploreDependee.Name = "exploreDependee";
            this.exploreDependee.Size = new System.Drawing.Size(1497, 35);
            this.exploreDependee.TabIndex = 1;
            this.exploreDependee.Text = "ExploreDependee";
            this.exploreDependee.UseVisualStyleBackColor = true;
            this.exploreDependee.Click += new System.EventHandler(this.exploreDependee_Click);
            // 
            // dependeesListBox
            // 
            this.dependeesListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dependeesListBox.FormattingEnabled = true;
            this.dependeesListBox.ItemHeight = 20;
            this.dependeesListBox.Location = new System.Drawing.Point(0, 0);
            this.dependeesListBox.Name = "dependeesListBox";
            this.dependeesListBox.Size = new System.Drawing.Size(1497, 424);
            this.dependeesListBox.TabIndex = 0;
            this.dependeesListBox.DoubleClick += new System.EventHandler(this.exploreDependee_Click);
            // 
            // exploreDependent
            // 
            this.exploreDependent.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.exploreDependent.Location = new System.Drawing.Point(0, 425);
            this.exploreDependent.Name = "exploreDependent";
            this.exploreDependent.Size = new System.Drawing.Size(1497, 39);
            this.exploreDependent.TabIndex = 1;
            this.exploreDependent.Text = "Explore Dependent";
            this.exploreDependent.UseVisualStyleBackColor = true;
            this.exploreDependent.Click += new System.EventHandler(this.exploreDependent_Click);
            // 
            // dependentsListBox
            // 
            this.dependentsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dependentsListBox.FormattingEnabled = true;
            this.dependentsListBox.ItemHeight = 20;
            this.dependentsListBox.Location = new System.Drawing.Point(0, 0);
            this.dependentsListBox.Name = "dependentsListBox";
            this.dependentsListBox.Size = new System.Drawing.Size(1497, 425);
            this.dependentsListBox.TabIndex = 0;
            this.dependentsListBox.DoubleClick += new System.EventHandler(this.exploreDependent_Click);
            // 
            // NodeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1497, 912);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.nodeTitle);
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
