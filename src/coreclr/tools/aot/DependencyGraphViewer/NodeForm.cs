// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DependencyLogViewer
{
    public partial class NodeForm : Form
    {
        private readonly Graph _graph;
        private Node _node;

        public NodeForm(Graph g, Node n)
        {
            _graph = g;
            InitializeComponent();
            SetNode(n);
            btnBack.Visible = btnForward.Visible = chkSameWindowNav.Checked;
            var fixControls = new Control[] {btnBack, btnForward, chkSameWindowNav, exploreDependent, dependentsListBox, exploreDependee, dependeesListBox, this, this.splitContainer1 };
            foreach (var cntrl in fixControls)
            {
                cntrl.PreviewKeyDown += Cntrl_PreviewKeyDown;
                cntrl.KeyDown += Cntrl_KeyDown;
            }
        }


        private void Cntrl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) {
            if (!chkSameWindowNav.Checked)
                return;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                e.IsInputKey = true;
        }

        private void Cntrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (!chkSameWindowNav.Checked)
                return;
            if (e.KeyCode == Keys.Left)
            {
                if (btnBack.Enabled)
                    btnBack.PerformClick();
            }
            else if (e.KeyCode == Keys.Right)
            {
                if (btnForward.Enabled)
                    btnForward.PerformClick();
            }
            else
                return;
            e.SuppressKeyPress = true;
            e.Handled = true;
        }

        public void SetNode(Node n)
        {

            _node = n;

            this.Text = $"Graph Pid: {_graph.PID}, ID: {_graph.ID}, Node: {_node.ToString}";
            var nodeStr = _node.ToString().Replace(", ", "\n");
            this.nodeTitle.Text = $"Current Node: {nodeStr}";

            lock (GraphCollection.Singleton)
            {
                List<BoxDisplay> sourceNodes = new();
                foreach (var pair in _node.Sources)
                {
                    sourceNodes.Add(new BoxDisplay(pair.Key, pair.Value));
                }

                List<BoxDisplay> targetNodes = new();
                foreach (var pair in _node.Targets)
                {
                    targetNodes.Add(new BoxDisplay(pair.Key, pair.Value));
                }

                this.dependentsListBox.DataSource = sourceNodes;
                this.dependeesListBox.DataSource = targetNodes;
            }
            if (CurSpotInHistory == -1 && chkSameWindowNav.Checked)//if we are in history we dont modify history
                AddSelfToHistory();
            SetNavButtonStates();
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
            if (chkSameWindowNav.Checked != true)
            {
                ExploreSelectedItem(_graph, dependeesListBox);
                return;
            }
            ClearHistoryIfIn();
            var selected = (BoxDisplay)dependeesListBox.SelectedItem;
            SetNode(selected.node);
        }

        private void exploreDependent_Click(object sender, EventArgs e)
        {
            if (chkSameWindowNav.Checked != true)
            {
                ExploreSelectedItem(_graph, dependentsListBox);
                return;
            }
            ClearHistoryIfIn();
            var selected = (BoxDisplay)dependentsListBox.SelectedItem;
            SetNode(selected.node);
        }
        private void AddSelfToHistory()
        {
            History.Add(_node);
        }
        private void ClearHistoryIfIn()
        {

            if (CurSpotInHistory != -1)
            {
                var removeAfter = CurSpotInHistory + 1;
                if (removeAfter != History.Count)
                    History.RemoveRange(removeAfter, History.Count - removeAfter);
                CurSpotInHistory = -1;
            }
        }
        public int CurSpotInHistory = -1;
        private List<Node> History = new();

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (CurSpotInHistory == -1)
                CurSpotInHistory = History.Count - 2;
            else if (CurSpotInHistory == 0) // should not get here
                return;
            else
                CurSpotInHistory--;
            SetNode(History[CurSpotInHistory]);
        }
        private void btnForward_Click(object sender, EventArgs e)
        {
            if (CurSpotInHistory == -1)// should not get here
                return;
            else if (CurSpotInHistory == History.Count - 1) // should not get here
                return;
            else
                CurSpotInHistory++;
            SetNode(History[CurSpotInHistory]);
        }
        private void SetNavButtonStates()
        {
            btnBack.Enabled = CurSpotInHistory != 0 && History.Count > 1;
            btnForward.Enabled = CurSpotInHistory != -1 && CurSpotInHistory != History.Count - 1;
        }
        private void ChkSameWindowNav_CheckedChanged(object sender, System.EventArgs e) => btnBack.Visible = btnForward.Visible = chkSameWindowNav.Checked;
        private void infoButton_LinkClicked(object sender, EventArgs e)
        {
            string dMessage = "Dependent nodes depend on the current node. The current node depends on the dependees.";
            MessageBox.Show(dMessage);
        }
    }
}
