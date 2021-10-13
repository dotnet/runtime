// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

using ILCompiler.DependencyAnalysisFramework;
using Internal.IL;

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

            ILReader ilReader = new(bodyBlock.GetILBytes());
            while (ilReader.HasNext)
            {
                ILOpcode opcode = ilReader.ReadILOpcode();

                switch (opcode)
                {
                    case ILOpcode.sizeof_:
                    case ILOpcode.newarr:
                    case ILOpcode.stsfld:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldsflda:
                    case ILOpcode.stfld:
                    case ILOpcode.ldfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.call:
                    case ILOpcode.calli:
                    case ILOpcode.callvirt:
                    case ILOpcode.newobj:
                    case ILOpcode.ldtoken:
                    case ILOpcode.ldftn:
                    case ILOpcode.ldvirtftn:
                    case ILOpcode.initobj:
                    case ILOpcode.stelem:
                    case ILOpcode.ldelem:
                    case ILOpcode.ldelema:
                    case ILOpcode.box:
                    case ILOpcode.unbox:
                    case ILOpcode.unbox_any:
                    case ILOpcode.jmp:
                    case ILOpcode.cpobj:
                    case ILOpcode.ldobj:
                    case ILOpcode.castclass:
                    case ILOpcode.isinst:
                    case ILOpcode.stobj:
                    case ILOpcode.refanyval:
                    case ILOpcode.mkrefany:
                    case ILOpcode.constrained:
                        yield return new DependencyListEntry(factory.GetNodeForToken(
                            _module,
                            MetadataTokens.EntityHandle(ilReader.ReadILToken())),
                            $"Instruction {opcode.ToString()} operand");
                        break;

                    default:
                        ilReader.Skip(opcode);
                        break;
                }
            }
        }

        public int Write(ModuleWritingContext writeContext)
        {
            int rva = _module.MetadataReader.GetMethodDefinition(_methodHandle).RelativeVirtualAddress;
            if (rva == 0)
                return -1;

            MethodBodyBlock bodyBlock = _module.PEReader.GetMethodBody(rva);

            // TODO: need to rewrite token references in the exception regions
            // This would need ControlFlowBuilder and setting up labels and such.
            // All doable, just more code.

            BlobBuilder outputBodyBuilder = writeContext.GetSharedBlobBuilder();
            byte[] bodyBytes = bodyBlock.GetILBytes();
            ILReader ilReader = new ILReader(bodyBytes);
            while (ilReader.HasNext)
            {
                int offset = ilReader.Offset;
                ILOpcode opcode = ilReader.ReadILOpcode();

                switch (opcode)
                {
                    case ILOpcode.sizeof_:
                    case ILOpcode.newarr:
                    case ILOpcode.stsfld:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldsflda:
                    case ILOpcode.stfld:
                    case ILOpcode.ldfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.call:
                    case ILOpcode.calli:
                    case ILOpcode.callvirt:
                    case ILOpcode.newobj:
                    case ILOpcode.ldtoken:
                    case ILOpcode.ldftn:
                    case ILOpcode.ldvirtftn:
                    case ILOpcode.initobj:
                    case ILOpcode.stelem:
                    case ILOpcode.ldelem:
                    case ILOpcode.ldelema:
                    case ILOpcode.box:
                    case ILOpcode.unbox:
                    case ILOpcode.unbox_any:
                    case ILOpcode.jmp:
                    case ILOpcode.cpobj:
                    case ILOpcode.ldobj:
                    case ILOpcode.castclass:
                    case ILOpcode.isinst:
                    case ILOpcode.stobj:
                    case ILOpcode.refanyval:
                    case ILOpcode.mkrefany:
                    case ILOpcode.constrained:
                        if (opcode > ILOpcode.prefix1)
                        {
                            outputBodyBuilder.WriteByte((byte)ILOpcode.prefix1);
                            outputBodyBuilder.WriteByte((byte)(((int)opcode) & 0xff));
                        }
                        else
                        {
                            Debug.Assert(opcode != ILOpcode.prefix1);
                            outputBodyBuilder.WriteByte((byte)opcode);
                        }
                        outputBodyBuilder.WriteInt32(MetadataTokens.GetToken(writeContext.TokenMap.MapToken(MetadataTokens.EntityHandle(ilReader.ReadILToken()))));
                        break;

                    case ILOpcode.ldstr:
                        outputBodyBuilder.WriteByte((byte)opcode);
                        outputBodyBuilder.WriteInt32(
                            MetadataTokens.GetToken(
                                writeContext.MetadataBuilder.GetOrAddUserString(
                                    _module.MetadataReader.GetUserString(
                                        MetadataTokens.UserStringHandle(ilReader.ReadILToken())))));
                        break;

                    default:
                        outputBodyBuilder.WriteBytes(bodyBytes, offset, ILOpcodeHelper.GetSize(opcode));
                        ilReader.Skip(opcode);
                        break;
                }
            }

            MethodBodyStreamEncoder.MethodBody bodyEncoder = writeContext.MethodBodyEncoder.AddMethodBody(
                outputBodyBuilder.Count,
                bodyBlock.MaxStack,
                exceptionRegionCount: 0,
                hasSmallExceptionRegions: false,
                (StandaloneSignatureHandle)writeContext.TokenMap.MapToken(bodyBlock.LocalSignature),
                bodyBlock.LocalVariablesInitialized ? MethodBodyAttributes.InitLocals : MethodBodyAttributes.None);
            BlobWriter instructionsWriter = new(bodyEncoder.Instructions);
            outputBodyBuilder.WriteContentTo(ref instructionsWriter);

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
