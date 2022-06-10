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
            this.label2 = new System.Windows.Forms.Label();
            this.filterButton = new System.Windows.Forms.Button();
            this.filterTextBox = new System.Windows.Forms.TextBox();
            this.exploreNode = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // filteredNodes
            // 
            this.filteredNodes.FormattingEnabled = true;
            this.filteredNodes.ItemHeight = 30;
            this.filteredNodes.Location = new System.Drawing.Point(-1, 184);
            this.filteredNodes.Margin = new System.Windows.Forms.Padding(4);
            this.filteredNodes.Name = "filteredNodes";
            this.filteredNodes.Size = new System.Drawing.Size(1225, 694);
            this.filteredNodes.TabIndex = 0;
            this.filteredNodes.DoubleClick += new System.EventHandler(this.exploreNode_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.filterButton);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.exploreNode);
            this.groupBox1.Controls.Add(this.filterTextBox);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1212, 153);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Select and Explore Nodes";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 120);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(1166, 30);
            this.label2.TabIndex = 4;
            this.label2.Text = "Enter text to filter nodes with, then select \'Filter\'. Explore Depnedents and Dep" +
    "endees of a selected node with \'Explore Node\'";
            // 
            // filterButton
            // 
            this.filterButton.Location = new System.Drawing.Point(780, 26);
            this.filterButton.Margin = new System.Windows.Forms.Padding(4);
            this.filterButton.Name = "filterButton";
            this.filterButton.Size = new System.Drawing.Size(202, 71);
            this.filterButton.TabIndex = 0;
            this.filterButton.Text = "Filter";
            this.filterButton.UseVisualStyleBackColor = true;
            this.filterButton.Click += new System.EventHandler(this.filterButton_Click);
            // 
            // filterTextBox
            // 
            this.filterTextBox.AllowDrop = true;
            this.filterTextBox.Location = new System.Drawing.Point(7, 62);
            this.filterTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.filterTextBox.Name = "filterTextBox";
            this.filterTextBox.Size = new System.Drawing.Size(765, 35);
            this.filterTextBox.TabIndex = 1;
            // 
            // exploreNode
            // 
            this.exploreNode.Location = new System.Drawing.Point(990, 26);
            this.exploreNode.Margin = new System.Windows.Forms.Padding(4);
            this.exploreNode.Name = "exploreNode";
            this.exploreNode.Size = new System.Drawing.Size(212, 71);
            this.exploreNode.TabIndex = 2;
            this.exploreNode.Text = "Explore Node";
            this.exploreNode.UseVisualStyleBackColor = true;
            this.exploreNode.Click += new System.EventHandler(this.exploreNode_Click);
            // 
            // SingleDependencyGraphForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 30F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1227, 872);
            this.Controls.Add(this.filteredNodes);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "SingleDependencyGraphForm";
            this.Text = "SingleDependencyGraphForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ListBox filteredNodes;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button filterButton;
        private System.Windows.Forms.TextBox filterTextBox;
        private System.Windows.Forms.Button exploreNode;
    }
}
