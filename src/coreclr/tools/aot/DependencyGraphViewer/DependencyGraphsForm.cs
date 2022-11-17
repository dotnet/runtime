// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Diagnostics.Tracing.Session;

namespace DependencyLogViewer
{
    public partial class DependencyGraphs : Form
    {
        private int _fileCount = -1;

#nullable enable
        public DependencyGraphs(string? argPath)
        {
            InitializeComponent();
            if (argPath != null)
            {
                OpenFile(argPath);
            }
        }
#nullable restore

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
            if (ETWGraphProcessing.Singleton is not null)
            {
                ETWGraphProcessing.Singleton.Stop();
            }
            Application.Exit();
        }

        private void help_Click(object sender, EventArgs e)
        {
            string helpMessage = """
                                    Dependency Graph Viewer

                                    This application allows viewing the dependency graph produced by
                                    the AOT compilation or the IL linker.

                                    Usage instructions:
                                      1. Launch the process
                                      2. Select a .dgml/.xml file to load by selecting ""Browse Files"".
                                         Alternatively, you can opt to use ETW events by selecting ""Use
                                         ETW Events"".
                                        a. If ETW events are wanted, the process must be run as an
                                           administrator.
                                      3. Explore through the graph

                                    Graphs Select
                                      - Choose one of the graphs that appears in the Dependency Graphs
                                        view to explore. As compilers execute and files are uploaded,
                                        new graphs will automatically appear here.
                                      - The set of graphs loaded into the process is limited by
                                        available memory space. To clear the used memory, close all
                                        windows of the application.

                                    Nodes View
                                      - In the Dependency Graph view, enter a regular expression in
                                        the text box, and then press ""Filter"".
                                      - Commonly, if there is a object file symbol associated with the
                                        node it should be used as part of the regular expression. See
                                        the various implementations of GetName in the compiler for
                                        naming behavior.
                                      - Additionally, an ID is assigned to each node, and nodes can be
                                        filtered by ID if a specific ID is known. (This is for use when
                                        using this tool in parallel with debugging the compiler).

                                    Single Node Exploration
                                      - Once the interesting node(s) have been identified in the dependency
                                        graph window, select one of them, and then select Explore.
                                      - In the Node Explorer window, the Dependent nodes (the ones which
                                        depend on the current node) are displayed above, and the Dependee
                                        nodes (the nodes that this node depends on) are displayed below.
                                        Each node in the list is paired with a textual reason as to why that
                                        edge in the graph exists.
                                      - Select a node to explore further.
                                    """;
            MessageBox.Show(helpMessage);
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

#nullable enable
        private void OpenFile(string? argPath = null)
        {
            GraphCollection collection = GraphCollection.Singleton;

            if (DGMLGraphProcessing.StartProcess(_fileCount, argPath))
            {
                _fileCount -= 1;
            }
        }

#nullable restore
        private void ETWButton_Click(object sender, EventArgs e)
        {
            // Today you have to be Admin to collect ETW events (anyone can write ETW events). If user elects to use ETW events, they must be Admin.
            if (!(TraceEventSession.IsElevated() ?? false))
            {
                MessageBox.Show("Error: Relaunch program as an adminstrator to collect ETW Events.");
            }
            else
            {
                ETWGraphProcessing.Singleton = new ETWGraphProcessing();
                MessageBox.Show("ETW Events have been enabled");
            }
        }
       public static void showError(string msg)
        {
            MessageBox.Show($"Invalid file upload: {msg}");
        }
    }
}
