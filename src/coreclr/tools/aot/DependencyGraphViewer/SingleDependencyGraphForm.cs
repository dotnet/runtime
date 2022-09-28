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
using System.Text.RegularExpressions;

namespace DependencyLogViewer
{
    public partial class SingleDependencyGraphForm : Form
    {
        private Graph _graph;

        public SingleDependencyGraphForm(Graph graph)
        {
            _graph = graph;
            InitializeComponent();
            this.Text = $"{this.Text}, {graph.ToString()}";
        }

        private void filterButton_Click(object sender, EventArgs e)
        {
            string filterString = this.filterTextBox.Text;
            lock (GraphCollection.Singleton)
            {
                Regex regex = new Regex(filterString, RegexOptions.Compiled);

                List<Node> filteredNodes = new List<Node>();
                foreach (var entry in _graph.Nodes)
                {
                    if (regex.IsMatch(entry.Value.ToString()))
                        filteredNodes.Add(entry.Value);
                }

                this.filteredNodes.DataSource = filteredNodes;
            }
        }

        private void exploreNode_Click(object sender, EventArgs e)
        {
            Node n = filteredNodes.SelectedItem as Node;
            if (n != null)
            {
                NodeForm nodeForm = new NodeForm(_graph, n);
                nodeForm.Show();
            }
        }
    }
}
