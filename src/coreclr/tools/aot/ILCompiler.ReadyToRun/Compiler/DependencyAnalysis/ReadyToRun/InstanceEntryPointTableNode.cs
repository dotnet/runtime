// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

using Internal.JitInterface;
using Internal.NativeFormat;
using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class InstanceEntryPointTableNode : HeaderTableNode
    {
        private readonly NodeFactory _factory;

        public InstanceEntryPointTableNode(NodeFactory factory)
            : base(factory.Target)
        {
            _factory = factory;
        }
        
        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunInstanceEntryPointTable");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());
            }

            NativeWriter hashtableWriter = new NativeWriter();

            Section hashtableSection = hashtableWriter.NewSection();
            VertexHashtable vertexHashtable = new VertexHashtable();
            hashtableSection.Place(vertexHashtable);

            Dictionary<byte[], BlobVertex> uniqueFixups = new Dictionary<byte[], BlobVertex>(ByteArrayComparer.Instance);
            Dictionary<byte[], BlobVertex> uniqueSignatures = new Dictionary<byte[], BlobVertex>(ByteArrayComparer.Instance);

            foreach (MethodWithGCInfo method in factory.EnumerateCompiledMethods(null, CompiledMethodCategory.Instantiated))
            {
                Debug.Assert(method.Method.HasInstantiation || method.Method.OwningType.HasInstantiation);

                int methodIndex = factory.RuntimeFunctionsTable.GetIndex(method);

                // In composite R2R format, always enforce owning type to let us share generic instantiations among modules
                EcmaMethod typicalMethod = (EcmaMethod)method.Method.GetTypicalMethodDefinition();
                ModuleToken moduleToken = new ModuleToken(typicalMethod.Module, typicalMethod.Handle);

                ArraySignatureBuilder signatureBuilder = new ArraySignatureBuilder();
                signatureBuilder.EmitMethodSignature(
                    new MethodWithToken(method.Method, moduleToken, constrainedType: null, unboxing: false, context: null),
                    enforceDefEncoding: true,
                    enforceOwningType: _factory.CompilationModuleGroup.EnforceOwningType(moduleToken.Module),
                    factory.SignatureContext,
                    isInstantiatingStub: false);
                byte[] signature = signatureBuilder.ToArray();
                BlobVertex signatureBlob;
                if (!uniqueSignatures.TryGetValue(signature, out signatureBlob))
                {
                    signatureBlob = new BlobVertex(signature);
                    hashtableSection.Place(signatureBlob);
                    uniqueSignatures.Add(signature, signatureBlob);
                }

                byte[] fixup = method.GetFixupBlob(factory);
                BlobVertex fixupBlob = null;
                if (fixup != null && !uniqueFixups.TryGetValue(fixup, out fixupBlob))
                {
                    fixupBlob = new BlobVertex(fixup);
                    hashtableSection.Place(fixupBlob);
                    uniqueFixups.Add(fixup, fixupBlob);
                }

                EntryPointVertex entryPointVertex = new EntryPointWithBlobVertex((uint)methodIndex, fixupBlob, signatureBlob);
                hashtableSection.Place(entryPointVertex);
                vertexHashtable.Append(unchecked((uint)ReadyToRunHashCode.MethodHashCode(method.Method)), entryPointVertex);
            }

            MemoryStream hashtableContent = new MemoryStream();
            hashtableWriter.Save(hashtableContent);
            return new ObjectData(
                data: hashtableContent.ToArray(),
                relocs: null,
                alignment: 8,
                definedSymbols: new ISymbolDefinitionNode[] { this });
        }

        public override int ClassCode => -348722540;
    }
}
