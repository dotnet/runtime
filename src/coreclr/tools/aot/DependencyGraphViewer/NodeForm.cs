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
    public partial class NodeForm : Form
    {
        Graph _graph;
        Node _node;

        public NodeForm(Graph g, Node n)
        {
            _graph = g;
            _node = n;

            InitializeComponent();

            this.Text = "Graph Pid:" + _graph.PID + " Id:" + _graph.ID + " Node:" + _node.ToString();
            this.nodeTitle.Text = _node.ToString();

            lock (GraphCollection.Singleton)
            {
                this.dependentsListBox.DataSource = _node.Dependents.ToArray();
                this.dependeesListBox.DataSource = _node.Dependencies.ToArray();
            }
        }

        private static void ExploreSelectedItem(Graph graph, ListBox listbox)
        {
            if (listbox.SelectedItem == null)
                return;

            KeyValuePair<Node, string> pair = (KeyValuePair<Node, string>)listbox.SelectedItem;

            NodeForm nodeForm = new NodeForm(graph, pair.Key);
            nodeForm.Show();
        }

        private void exploreDependee_Click(object sender, EventArgs e)
        {
            ExploreSelectedItem(_graph, dependeesListBox);
        }

        private void exploreDependent_Click(object sender, EventArgs e)
        {
            ExploreSelectedItem(_graph, dependentsListBox);
        }
    }
}
