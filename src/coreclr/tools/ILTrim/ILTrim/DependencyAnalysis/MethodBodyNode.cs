// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ILCompiler.DependencyAnalysisFramework;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents method body bytes emitted into the executable.
    /// </summary>
    public class MethodBodyNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaModule _module;
        private readonly MethodDefinitionHandle _methodHandle;

        public MethodBodyNode(EcmaModule module, MethodDefinitionHandle methodHandle)
        {
            _module = module;
            _methodHandle = methodHandle;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            // RVA = 0 is an extern method, such as a DllImport
            int rva = _module.MetadataReader.GetMethodDefinition(_methodHandle).RelativeVirtualAddress;
            if (rva == 0)
                yield break;

            MethodBodyBlock bodyBlock = _module.PEReader.GetMethodBody(rva);

            if (!bodyBlock.LocalSignature.IsNil)
                yield return new DependencyListEntry(factory.StandaloneSignature(_module, bodyBlock.LocalSignature), "Signatures of local variables");

            // TODO: we'll need to scan the method body and figure out what it depends on
        }

        public int Write(ModuleWritingContext writeContext)
        {
            int rva = _module.MetadataReader.GetMethodDefinition(_methodHandle).RelativeVirtualAddress;
            if (rva == 0)
                return -1;

            writeContext.ILStream.Align(4);

            MethodBodyBlock bodyBlock = _module.PEReader.GetMethodBody(rva);

            // TODO: need to rewrite token references in the method body and exception regions

            byte[] bodyBytes = bodyBlock.GetILBytes();

            MethodBodyStreamEncoder bodyStreamEncoder = new MethodBodyStreamEncoder(writeContext.ILStream);
            var bodyEncoder = bodyStreamEncoder.AddMethodBody(
                bodyBytes.Length,
                bodyBlock.MaxStack,
                exceptionRegionCount: 0,
                hasSmallExceptionRegions: false,
                (StandaloneSignatureHandle)writeContext.TokenMap.MapToken(bodyBlock.LocalSignature),
                bodyBlock.LocalVariablesInitialized ? MethodBodyAttributes.InitLocals : MethodBodyAttributes.None);
            new BlobWriter(bodyEncoder.Instructions).WriteBytes(bodyBytes);

            return bodyEncoder.Offset;
        }

        protected override string GetName(NodeFactory factory)
        {
            // TODO: would be nice to have a common formatter we can call into that also includes owning type
            MetadataReader reader = _module.MetadataReader;
            return "Method body for " + reader.GetString(reader.GetMethodDefinition(_methodHandle).Name);
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
