// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem.Interop;
using Internal.ReadyToRunConstants;
using Internal.CorConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class TypeFixupSignature : Signature
    {
        private readonly ReadyToRunFixupKind _fixupKind;

        private readonly TypeDesc _typeDesc;

        public TypeFixupSignature(ReadyToRunFixupKind fixupKind, TypeDesc typeDesc)
        {
            _fixupKind = fixupKind;
            _typeDesc = typeDesc;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            ((CompilerTypeSystemContext)typeDesc.Context).EnsureLoadableType(typeDesc);
        }

        public override int ClassCode => 255607008;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                IEcmaModule targetModule = factory.SignatureContext.GetTargetModule(_typeDesc);
                SignatureContext innerContext = dataBuilder.EmitFixup(factory, _fixupKind, targetModule, factory.SignatureContext);
                dataBuilder.EmitTypeSignature(_typeDesc, innerContext);

                if ((_fixupKind == ReadyToRunFixupKind.Check_TypeLayout) ||
                    (_fixupKind == ReadyToRunFixupKind.Verify_TypeLayout))
                {
                    EncodeTypeLayout(dataBuilder, _typeDesc);
                }
            }

            return dataBuilder.ToObjectData();
        }

        private static void EncodeTypeLayout(ObjectDataSignatureBuilder dataBuilder, TypeDesc type)
        {
            Debug.Assert(type.IsValueType);
            MetadataType defType = (MetadataType)type;

            int pointerSize = type.Context.Target.PointerSize;
            int size = defType.InstanceFieldSize.AsInt;
            int alignment = Internal.JitInterface.CorInfoImpl.GetClassAlignmentRequirementStatic(defType);
            ReadyToRunTypeLayoutFlags flags = ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_Alignment | ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_GCLayout;
            if (alignment == pointerSize)
            {
                flags |= ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_Alignment_Native;
            }

            if (!defType.ContainsGCPointers)
            {
                flags |= ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_GCLayout_Empty;
            }

            if (defType.IsHomogeneousAggregate)
            {
                flags |= ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_HFA;
            }

            dataBuilder.EmitUInt((uint)flags);
            dataBuilder.EmitUInt((uint)size);

            if (defType.IsHomogeneousAggregate)
            {
                ReadyToRunHFAElemType hfaElementType = (defType.ValueTypeShapeCharacteristics & ValueTypeShapeCharacteristics.AggregateMask) switch
                {
                    ValueTypeShapeCharacteristics.Float32Aggregate => ReadyToRunHFAElemType.Float32,
                    ValueTypeShapeCharacteristics.Float64Aggregate => ReadyToRunHFAElemType.Float64,
                    ValueTypeShapeCharacteristics.Vector64Aggregate => ReadyToRunHFAElemType.Vector64,
                    // See MethodTable::GetHFAType
                    ValueTypeShapeCharacteristics.Vector128Aggregate => ReadyToRunHFAElemType.Vector128,
                    _ => throw new NotSupportedException()
                };
                dataBuilder.EmitUInt((uint)hfaElementType);
            }
            
            if (alignment != pointerSize)
            {
                dataBuilder.EmitUInt((uint)alignment);
            }

            if (defType.ContainsGCPointers)
            {
                // Encode the GC pointer map
                GCPointerMap gcMap = GCPointerMap.FromInstanceLayout(defType);

                byte[] encodedGCRefMap = new byte[(size / pointerSize + 7) / 8];
                int bitIndex = 0;
                foreach (bool bit in gcMap)
                {
                    if (bit)
                    {
                        encodedGCRefMap[bitIndex / 8] |= (byte)(1 << (bitIndex & 7));
                    }

                    ++bitIndex;
                }

                dataBuilder.EmitBytes(encodedGCRefMap);
            }
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"TypeFixupSignature({_fixupKind.ToString()}): ");
            sb.Append(nameMangler.GetMangledTypeName(_typeDesc));
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            TypeFixupSignature otherNode = (TypeFixupSignature)other;
            int result = ((int)_fixupKind).CompareTo((int)otherNode._fixupKind);
            if (result != 0)
                return result;

            return comparer.Compare(_typeDesc, otherNode._typeDesc);
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();

            if (_typeDesc.HasInstantiation &&
                !_typeDesc.IsGenericDefinition &&
                (factory.CompilationCurrentPhase == 0) &&
                factory.CompilationModuleGroup.VersionsWithType(_typeDesc))
            {
                dependencies.Add(factory.AllMethodsOnType(_typeDesc), "Methods on generic type instantiation");
            }

            if (_fixupKind == ReadyToRunFixupKind.TypeHandle)
            {
                AddDependenciesForAsyncStateMachineBox(ref dependencies, factory, _typeDesc);
            }
            return dependencies;
        }

        public static void AddDependenciesForAsyncStateMachineBox(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            ReadyToRunCompilerContext context = (ReadyToRunCompilerContext)type.Context;
            // If adding a typehandle to the AsyncStateMachineBox, pre-compile the most commonly used methods.
            // As long as we haven't already reached compilation phase 7, which is an arbitrary number of phases of compilation chosen so that
            // simple examples of async will get compiled
            if (factory.OptimizationFlags.OptimizeAsyncMethods && type.GetTypeDefinition() == context.AsyncStateMachineBoxType && !type.IsGenericDefinition && factory.CompilationCurrentPhase <= 7)
            {
                if (dependencies == null)
                    dependencies = new DependencyList();

                // This is the async state machine box, compile the cctor, and the MoveNext method.
                foreach (MethodDesc method in type.ConvertToCanonForm(CanonicalFormKind.Specific).GetAllMethods())
                {
                    if (!method.IsGenericMethodDefinition &&
                        factory.CompilationModuleGroup.ContainsMethodBody(method, false))
                    {
                        switch (method.Name)
                        {
                            case "MoveNext":
                            case ".cctor":
                                dependencies.Add(factory.CompiledMethodNode(method), $"AsyncStateMachineBox Method on type {type.ToString()}");
                                break;
                        }
                    }
                }
            }
        }
    }
}
