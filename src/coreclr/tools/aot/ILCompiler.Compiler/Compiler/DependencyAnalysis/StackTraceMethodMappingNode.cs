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
    public sealed class StackTraceMethodMappingNode : ObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        private int? _size;

        int INodeWithSize.Size => _size.Value;

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

                if (entry.IsHidden)
                {
                    command |= StackTraceDataCommand.IsStackTraceHidden;
                }

                objData.EmitByte(commandReservation, command);
                objData.EmitReloc(factory.MethodEntrypoint(entry.Method), RelocType.IMAGE_REL_BASED_RELPTR32);
            }

            _size = objData.CountBytes;
            return objData.ToObjectData();
        }
    }
}
