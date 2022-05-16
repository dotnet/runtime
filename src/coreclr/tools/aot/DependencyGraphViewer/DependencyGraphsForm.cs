// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DependencyLogViewer
{
    public partial class DependencyGraphs : Form
    {
        public DependencyGraphs()
        {
            InitializeComponent();
        }

        private void explore_Click(object sender, EventArgs e)
        {
            Graph g = this.listBox1.SelectedItem as Graph;
            if (g != null)
            {
                SingleDependencyGraphForm singleGraphForm = new SingleDependencyGraphForm(g);
                singleGraphForm.Show();
            }
        }

        public void ForceRefresh()
        {
            Action refreshAction = () =>
            {
                lock (GraphCollection.Singleton)
                {
                    GraphCollection singleton = GraphCollection.Singleton;
                    this.listBox1.DataSource = singleton.Graphs.ToArray();
                }
            };

            this.BeginInvoke(refreshAction);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            GraphProcessing.Singleton.Stop();
            Application.Exit();
        }

        private void help_Click(object sender, EventArgs e)
        {
            string helpMessage = @"
Dependency Graph Viewer
This application allows viewing the dependency graph produced by the AOT compilation.

Usage instructions:
1. Launch the process as an administrator
2. Run the compiler
3. Explore through the graph

Graphs View
- Choose one of the graphs that appears in the Dependency Graphs view to explore. As compilers execute, new graphs will automatically appear here.
- The set of graphs loaded into the process is limited by available memory space. To clear the used memory, close all windows of the application.

Graph View
- In the Dependency Graph view, enter a regular expression in the text box, and then press ""Filter"". This will display a list of the nodes in the graph which have names which match the regular expression.
- Commonly, if there is a object file symbol associated with the node it should be used as part of the regular expression. See the various implementations of GetName in the compiler for naming behavior.
- Additionally, the event source marking mode assigns an Id to each node, and that is found as the mark object on the node, so if a specific id is known, just type that in, and it will appear in the window. (This is for use when using this tool in parallel with debugging the compiler.

Single Node Exploration
Once the interesting node(s) have been identified in the dependency graph window, select one of them, and then press Explore.
  - In the Node Explorer window, the Dependent nodes (the ones which dependend on the current node are the nodes displayed above, and the Dependee nodes (the nodes that this node depends on) are displayed below. Each node in the list is paired with a textual reason as to why that edge in the graph exists.
  - Select a node to explore further and press the corresponding button to make it happen.
";
            MessageBox.Show(helpMessage);
        }
    }
}
