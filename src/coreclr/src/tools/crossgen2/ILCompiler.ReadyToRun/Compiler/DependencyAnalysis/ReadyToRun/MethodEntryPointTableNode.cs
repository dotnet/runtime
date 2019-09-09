// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public MethodEntryPointTableNode(TargetDetails target)
            : base(target)
        {
        }
        
        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunMethodEntryPointTable");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());
            }

            ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;
            List<EntryPoint> ridToEntryPoint = new List<EntryPoint>();

            foreach (MethodWithGCInfo method in r2rFactory.EnumerateCompiledMethods())
            {
                if (method.Method is EcmaMethod ecmaMethod)
                {
                    // Strip away the token type bits, keep just the low 24 bits RID
                    uint rid = SignatureBuilder.RidFromToken((mdToken)MetadataTokens.GetToken(ecmaMethod.Handle));
                    Debug.Assert(rid != 0);
                    rid--;

                    while (ridToEntryPoint.Count <= rid)
                    {
                        ridToEntryPoint.Add(EntryPoint.Null);
                    }

                    int methodIndex = r2rFactory.RuntimeFunctionsTable.GetIndex(method);
                    ridToEntryPoint[(int)rid] = new EntryPoint(methodIndex, method);
                }
            }

            NativeWriter writer = new NativeWriter();

            Section arraySection = writer.NewSection();
            VertexArray vertexArray = new VertexArray(arraySection);
            arraySection.Place(vertexArray);

            Section fixupSection = writer.NewSection();

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
                        fixupSection.Place(fixupBlobVertex);
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

        public override int ClassCode => 787556329;
    }
}
