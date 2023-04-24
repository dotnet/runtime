// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DependencyLogViewer
{
    partial class SingleDependencyGraphForm
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
            this.filteredNodes = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.filterButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.exploreNode = new System.Windows.Forms.Button();
            this.filterTextBox = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // filteredNodes
            // 
            this.filteredNodes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.filteredNodes.FormattingEnabled = true;
            this.filteredNodes.HorizontalScrollbar = true;
            this.filteredNodes.ItemHeight = 30;
            this.filteredNodes.Location = new System.Drawing.Point(3, 31);
            this.filteredNodes.Margin = new System.Windows.Forms.Padding(4);
            this.filteredNodes.Name = "filteredNodes";
            this.filteredNodes.Size = new System.Drawing.Size(1098, 608);
            this.filteredNodes.TabIndex = 0;
            this.filteredNodes.DoubleClick += new System.EventHandler(this.exploreNode_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.AutoSize = true;
            this.groupBox1.Controls.Add(this.filterButton);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.exploreNode);
            this.groupBox1.Controls.Add(this.filterTextBox);
            this.groupBox1.Location = new System.Drawing.Point(3, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1119, 165);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Select and Explore Nodes";
            // 
            // filterButton
            // 
            this.filterButton.Location = new System.Drawing.Point(733, 29);
            this.filterButton.Margin = new System.Windows.Forms.Padding(4);
            this.filterButton.Name = "filterButton";
            this.filterButton.Size = new System.Drawing.Size(184, 71);
            this.filterButton.TabIndex = 1;
            this.filterButton.Text = "Filter";
            this.filterButton.UseVisualStyleBackColor = true;
            this.filterButton.Click += new System.EventHandler(this.filterButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(0, 104);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(1031, 30);
            this.label2.TabIndex = 4;
            this.label2.Text = "Enter text to filter nodes with, then select \'Filter\'. Explore dependencies of a " +
    "selected node with \'Explore Node\'";
            // 
            // exploreNode
            // 
            this.exploreNode.Location = new System.Drawing.Point(925, 29);
            this.exploreNode.Margin = new System.Windows.Forms.Padding(4);
            this.exploreNode.Name = "exploreNode";
            this.exploreNode.Size = new System.Drawing.Size(184, 71);
            this.exploreNode.TabIndex = 2;
            this.exploreNode.Text = "Explore Node";
            this.exploreNode.UseVisualStyleBackColor = true;
            this.exploreNode.Click += new System.EventHandler(this.exploreNode_Click);
            // 
            // filterTextBox
            // 
            this.filterTextBox.AllowDrop = true;
            this.filterTextBox.Location = new System.Drawing.Point(3, 47);
            this.filterTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.filterTextBox.Name = "filterTextBox";
            this.filterTextBox.Size = new System.Drawing.Size(718, 35);
            this.filterTextBox.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.filteredNodes);
            this.groupBox2.Location = new System.Drawing.Point(10, 165);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1104, 642);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Filtered Nodes";
            // 
            // SingleDependencyGraphForm
            // 
            this.AcceptButton = this.filterButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 30F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1126, 836);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "SingleDependencyGraphForm";
            this.Text = "SingleDependencyGraphForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox filteredNodes;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button filterButton;
        private System.Windows.Forms.TextBox filterTextBox;
        private System.Windows.Forms.Button exploreNode;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}
