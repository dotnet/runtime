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

            this.Text = $"Graph Pid: {_graph.PID}, ID: {_graph.ID}, Node: {_node.ToString}";
            this.nodeTitle.Text = $"Current Node: {_node}";

            lock (GraphCollection.Singleton)
            {
                List<BoxDisplay> sourceNodes = new ();
                foreach (var pair in _node.Sources)
                {
                    sourceNodes.Add(new BoxDisplay(pair.Key, pair.Value));
                }

                List<BoxDisplay> targetNodes = new ();
                foreach (var pair in _node.Targets)
                {
                    targetNodes.Add(new BoxDisplay(pair.Key, pair.Value));
                }

                this.dependentsListBox.DataSource = sourceNodes;
                this.dependeesListBox.DataSource = targetNodes;
            }
        }

        private static void ExploreSelectedItem(Graph graph, ListBox listbox)
        {
            if (listbox.SelectedItem == null)
                return;

            BoxDisplay selected = (BoxDisplay)listbox.SelectedItem;

            NodeForm nodeForm = new NodeForm(graph, selected.node);
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

        private void infoButton_LinkClicked(object sender, EventArgs e)
        {
            string dMessage = "Dependent nodes depend on the current node. The current node depends on the dependees.";
            MessageBox.Show(dMessage);
        }
    }
    public class BoxDisplay
    {
        public Node node;
        public List<string> reason;

        public BoxDisplay(Node node, List<string> reason)
        {
            this.node = node;
            this.reason = reason;
        }

        public override string ToString()
        {
            return $"Index: {node.Index}, Name: {node.Name}, {reason.Count} Reason(s): {String.Join(", ", reason.ToArray())}";
        }
    }
}
