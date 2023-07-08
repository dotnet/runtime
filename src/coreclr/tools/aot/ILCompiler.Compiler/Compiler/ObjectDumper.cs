// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;

using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler
{
    public abstract class ObjectDumper : IObjectDumper
    {
        internal abstract void Begin();
        internal abstract void End();
        void IObjectDumper.DumpObjectNode(NodeFactory factory, ObjectNode node, ObjectData objectData) => DumpObjectNode(factory, node, objectData);
        protected abstract void DumpObjectNode(NodeFactory factory, ObjectNode node, ObjectData objectData);

        protected static string GetObjectNodeName(ObjectNode node)
        {
            string name = node.GetType().Name;

            // Some nodes are generic and their type name contains "`". Strip it.
            int indexOfAccent = name.LastIndexOf('`');
            if (indexOfAccent > 0)
                name = name.Substring(0, indexOfAccent);

            // Node type names generally end with "Node", but that's redundant.
            if (name.EndsWith("Node"))
                name = name.Substring(0, name.Length - 4);

            return name;
        }

        public static ObjectDumper Compose(IEnumerable<ObjectDumper> dumpers)
        {
            var dumpersList = default(ArrayBuilder<ObjectDumper>);

            foreach (var dumper in dumpers)
                dumpersList.Add(dumper);

            return dumpersList.Count switch
            {
                0 => null,
                1 => dumpersList[0],
                _ => new ComposedObjectDumper(dumpersList.ToArray()),
            };
        }

        private sealed class ComposedObjectDumper : ObjectDumper
        {
            private readonly ObjectDumper[] _dumpers;

            public ComposedObjectDumper(ObjectDumper[] dumpers) => _dumpers = dumpers;

            protected override void DumpObjectNode(NodeFactory factory, ObjectNode node, ObjectData objectData)
            {
                foreach (var d in _dumpers)
                    d.DumpObjectNode(factory, node, objectData);
            }
            internal override void Begin()
            {
                foreach (var d in _dumpers)
                    d.Begin();
            }
            internal override void End()
            {
                foreach (var d in _dumpers)
                    d.End();
            }
        }
    }
}
