// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

using ILCompiler.DependencyAnalysis;

using Internal.Text;

namespace ILCompiler
{
    /// <summary>
    /// A root provider that provides a runtime configuration blob that influences runtime behaviors.
    /// See RhConfigValues.h for allowed values.
    /// </summary>
    public class RuntimeConfigurationRootProvider : ICompilationRootProvider
    {
        private readonly string _blobName;
        private readonly IReadOnlyCollection<string> _runtimeOptions;

        public RuntimeConfigurationRootProvider(string blobName, IReadOnlyCollection<string> runtimeOptions)
        {
            _blobName = blobName;
            _runtimeOptions = runtimeOptions;
        }

        void ICompilationRootProvider.AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            rootProvider.AddCompilationRoot(new RuntimeConfigurationBlobNode(_blobName, _runtimeOptions), "Runtime configuration");
        }

        private sealed class RuntimeConfigurationBlobNode : ObjectNode, ISymbolDefinitionNode
        {
            private readonly string _blobName;
            private readonly IReadOnlyCollection<string> _runtimeOptions;

            public RuntimeConfigurationBlobNode(string blobName, IReadOnlyCollection<string> runtimeOptions)
            {
                _blobName = blobName;
                _runtimeOptions = runtimeOptions;
            }

            public int Offset => 0;

            public override bool IsShareable => false;

            public override int ClassCode => 7864454;

            public override bool StaticDependenciesAreComputed => true;

            public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            {
                sb.Append(nameMangler.NodeMangler.ExternVariable(_blobName));
            }

            public override ObjectNodeSection GetSection(NodeFactory factory) =>
                factory.Target.IsWindows ? ObjectNodeSection.ReadOnlyDataSection : ObjectNodeSection.DataSection;

            protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

            public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
            {
                var builder = new ObjectDataBuilder(factory.TypeSystemContext.Target, relocsOnly);
                builder.RequireInitialPointerAlignment();
                builder.AddSymbol(this);

                var settings = new Dictionary<string, ISymbolNode>();

                // Put values in a dictionary - we expect many "true" strings, for example.
                var valueDict = new Dictionary<string, ISymbolNode>();
                int valueIndex = 0;
                foreach (string line in _runtimeOptions)
                {
                    int indexOfEquals = line.IndexOf("=");
                    if (indexOfEquals > 0)
                    {
                        string key = line.Substring(0, indexOfEquals);
                        string value = line.Substring(indexOfEquals + 1);

                        if (!valueDict.TryGetValue(value, out ISymbolNode valueNode))
                        {
                            valueNode = factory.ReadOnlyDataBlob(
                                new Utf8String(_blobName + "_value_" + valueIndex++),
                                Utf8NullTerminatedBytes(value),
                                alignment: 1);
                            valueDict.Add(value, valueNode);
                        }

                        settings[key] = valueNode;
                    }
                }

                // The format is:
                // * Number of entries (T)
                // * N times pointer to key
                // * N times pointer to value
                builder.EmitNaturalInt(settings.Count);

                int i = 0;
                foreach (string key in settings.Keys)
                {
                    ISymbolNode node = factory.ReadOnlyDataBlob(
                                new Utf8String(_blobName + "_key_" + i++),
                                Utf8NullTerminatedBytes(key),
                                alignment: 1);
                    builder.EmitPointerReloc(node);
                }

                foreach (ISymbolNode value in settings.Values)
                {
                    builder.EmitPointerReloc(value);
                }

                static byte[] Utf8NullTerminatedBytes(string s)
                {
                    byte[] result = new byte[Encoding.UTF8.GetByteCount(s) + 1];
                    Encoding.UTF8.GetBytes(s, result);
                    return result;
                }

                return builder.ToObjectData();
            }

            public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
            {
                return _blobName.CompareTo(((RuntimeConfigurationBlobNode)other)._blobName);
            }
        }
    }
}
