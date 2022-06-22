// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using DependencyLogViewer;
using System.Windows.Forms.Design.Behavior;


namespace DependecyGraphViewer.Tests
{
    public class TestGraph
    {
        GraphCollection collection = GraphCollection.Singleton;
        public TestGraph()
        {
        }
        public static IEnumerable<object[]> GetDgml()
        {
            yield return new object[] { "<?xml version=\"1.0\" encoding=\"utf-8\"?><DirectedGraph Layout=\"ForceDirected\" xmlns=\"http://schemas.microsoft.com/vs/2009/dgml\"></DirectedGraph>" };
        }


        [Theory]
        [MemberData (nameof(GetDgml))]
        public void CreateGraph(string fileContents)
        {
            var stream = GenerateStreamFromString(fileContents);
            DGMLGraphProcessing testParser = new DGMLGraphProcessing(-1);
            testParser.FindXML(stream);
            Assert.Equal(testParser.g.Nodes.Count, 1);
        }

        static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
