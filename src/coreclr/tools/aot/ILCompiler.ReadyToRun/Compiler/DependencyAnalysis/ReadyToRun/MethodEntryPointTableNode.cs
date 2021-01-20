// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;

using Internal.JitInterface;
using Internal.NativeFormat;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class MethodEntryPointTableNode : HeaderTableNode
    {
        private readonly EcmaModule _module;

        private struct EntryPoint
        {
            public static EntryPoint Null = new EntryPoint(-1, null);

            public readonly int MethodIndex;
            public readonly MethodWithGCInfo Method;

            public bool IsNull => (MethodIndex < 0);
            
            public EntryPoint(int methodIndex, MethodWithGCInfo method)
            {
                MethodIndex = methodIndex;
                Method = method;
            }
        }

        public MethodEntryPointTableNode(EcmaModule module, TargetDetails target)
            : base(target)
        {
            _module = module;
        }
        
        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunMethodEntryPointTable__");
            sb.Append(_module.Assembly.GetName().Name);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());
            }

            List<EntryPoint> ridToEntryPoint = new List<EntryPoint>();

            foreach (MethodWithGCInfo method in factory.EnumerateCompiledMethods(_module, CompiledMethodCategory.NonInstantiated))
            {
                Debug.Assert(method.Method is EcmaMethod);
                EcmaMethod ecmaMethod = (EcmaMethod)method.Method;
                Debug.Assert(ecmaMethod.Module == _module);

                // Strip away the token type bits, keep just the low 24 bits RID
                uint rid = SignatureBuilder.RidFromToken((mdToken)MetadataTokens.GetToken(ecmaMethod.Handle));
                Debug.Assert(rid != 0);
                rid--;

                while (ridToEntryPoint.Count <= rid)
                {
                    ridToEntryPoint.Add(EntryPoint.Null);
                }

                int methodIndex = factory.RuntimeFunctionsTable.GetIndex(method);
                ridToEntryPoint[(int)rid] = new EntryPoint(methodIndex, method);
            }

            NativeWriter writer = new NativeWriter();

            Section arraySection = writer.NewSection();
            VertexArray vertexArray = new VertexArray(arraySection);
            arraySection.Place(vertexArray);

            Dictionary<byte[], BlobVertex> uniqueFixups = new Dictionary<byte[], BlobVertex>(ByteArrayComparer.Instance);

            for (int rid = 0; rid < ridToEntryPoint.Count; rid++)
            {
                EntryPoint entryPoint = ridToEntryPoint[rid];
                if (!entryPoint.IsNull)
                {
                    byte[] fixups = entryPoint.Method.GetFixupBlob(factory);

                    BlobVertex fixupBlobVertex = null;
                    if (fixups != null && !uniqueFixups.TryGetValue(fixups, out fixupBlobVertex))
                    {
                        fixupBlobVertex = new BlobVertex(fixups);
                        uniqueFixups.Add(fixups, fixupBlobVertex);
                    }
                    EntryPointVertex entryPointVertex = new EntryPointVertex((uint)entryPoint.MethodIndex, fixupBlobVertex);
                    vertexArray.Set(rid, entryPointVertex);
                }
            }

            vertexArray.ExpandLayout();

            MemoryStream arrayContent = new MemoryStream();
            writer.Save(arrayContent);
            return new ObjectData(
                data: arrayContent.ToArray(),
                relocs: null,
                alignment: 8,
                definedSymbols: new ISymbolDefinitionNode[] { this });
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            MethodEntryPointTableNode otherMethodEntryPointTable = (MethodEntryPointTableNode)other;
            return _module.Assembly.GetName().Name.CompareTo(otherMethodEntryPointTable._module.Assembly.GetName().Name);
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.MethodEntrypointTableNode;
    }
}
