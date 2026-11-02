// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// BlobIdStackTraceMethodRvaToTokenMapping - list of 8-byte pairs (method RVA-method token)
    /// </summary>
    public sealed class StackTraceMethodMappingNode : ObjectNode, ISymbolDefinitionNode
    {

        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.StackTraceMethodMappingNode;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("_stacktrace_methodRVA_to_token_mapping"u8);
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // The dependency tracking of this node currently does nothing because the data emission relies
            // the set of compiled methods which has an incomplete state during dependency tracking.
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
            }

            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);
            objData.RequireInitialPointerAlignment();
            objData.AddSymbol(this);

            var mapping = new List<StackTraceMapping>(factory.MetadataManager.GetStackTraceMapping(factory));

            // The information is encoded as a set of commands: set current owning type, set current method name, etc.
            // Sort things so that we don't thrash the current entity too much.
            mapping.Sort((x, y) =>
            {
                // Group methods on the same generic type definition together
                int result = x.OwningTypeHandle.CompareTo(y.OwningTypeHandle);
                if (result != 0)
                    return result;

                // Overloads get grouped together too
                result = x.MethodNameHandle.CompareTo(y.MethodNameHandle);
                if (result != 0)
                    return result;

                // Methods that only differ in something generic get grouped too
                result = x.MethodSignatureHandle.CompareTo(y.MethodSignatureHandle);
                if (result != 0)
                    return result;

                // At this point the genericness should be the same
                Debug.Assert(x.MethodInstantiationArgumentCollectionHandle == y.MethodInstantiationArgumentCollectionHandle);

                // Compare by the method as a tie breaker to get stable sort
                return TypeSystemComparer.Instance.Compare(x.Method, y.Method);
            });

            int currentOwningType = 0;
            int currentSignature = 0;
            int currentName = 0;

            // The first int contains the number of entries
            objData.EmitInt(mapping.Count);

            foreach (var entry in mapping)
            {
                var commandReservation = objData.ReserveByte();

                byte command = 0;
                if (currentOwningType != entry.OwningTypeHandle)
                {
                    currentOwningType = entry.OwningTypeHandle;
                    command |= StackTraceDataCommand.UpdateOwningType;
                    objData.EmitInt(currentOwningType);
                }

                if (currentName != entry.MethodNameHandle)
                {
                    currentName = entry.MethodNameHandle;
                    command |= StackTraceDataCommand.UpdateName;
                    objData.EmitCompressedUInt((uint)(currentName & MetadataManager.MetadataOffsetMask));
                }

                if (currentSignature != entry.MethodSignatureHandle)
                {
                    currentSignature = entry.MethodSignatureHandle;
                    objData.EmitCompressedUInt((uint)(currentSignature & MetadataManager.MetadataOffsetMask));

                    if (entry.MethodInstantiationArgumentCollectionHandle != 0)
                    {
                        command |= StackTraceDataCommand.UpdateGenericSignature;
                        objData.EmitCompressedUInt((uint)(entry.MethodInstantiationArgumentCollectionHandle & MetadataManager.MetadataOffsetMask));
                    }
                    else
                    {
                        command |= StackTraceDataCommand.UpdateSignature;
                    }
                }

                if ((entry.Flags & StackTraceRecordFlags.IsHidden) != 0)
                {
                    command |= StackTraceDataCommand.IsStackTraceHidden;
                }

                // On WASM, record the function table index of the method if it can be observed at runtime: either
                // the method's address was taken, or the method is the target of an unboxing stub (so that results
                // of RuntimeAugments.GetCodeTarget can be mapped back to the method).
                ISymbolNode functionPointerNode = null;
                if (factory.Target.IsWasm)
                {
                    var addressTakenNode = factory.AddressTakenMethodEntrypoint(entry.Method);
                    if (addressTakenNode.Marked)
                        functionPointerNode = addressTakenNode;
                    else if (entry.Method.OwningType.IsValueType
                        && !entry.Method.Signature.IsStatic
                        && factory.MethodEntrypoint(entry.Method, unboxingStub: true).Marked)
                        functionPointerNode = factory.MethodEntrypoint(entry.Method);
                }

                if (functionPointerNode != null)
                {
                    command |= StackTraceDataCommand.HasFunctionPointer;
                }

                objData.EmitByte(commandReservation, command);
                objData.EmitReloc(factory.MethodEntrypoint(entry.Method),
                    factory.Target.IsWasm ? RelocType.WASM_METHOD_RELATIVE_VIRTUAL_IP_I32 : RelocType.IMAGE_REL_BASED_RELPTR32);
                if (functionPointerNode != null)
                {
                    objData.EmitReloc(functionPointerNode, RelocType.WASM_TABLE_INDEX_I32);
                }
            }

            return objData.ToObjectData();
        }
    }
}
