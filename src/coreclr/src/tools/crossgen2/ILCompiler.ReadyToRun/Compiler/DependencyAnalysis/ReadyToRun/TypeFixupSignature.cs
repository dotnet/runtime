// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

                EcmaModule targetModule = factory.SignatureContext.GetTargetModule(_typeDesc);
                SignatureContext innerContext = dataBuilder.EmitFixup(factory, _fixupKind, targetModule, factory.SignatureContext);
                dataBuilder.EmitTypeSignature(_typeDesc, innerContext);

                if (_fixupKind == ReadyToRunFixupKind.Check_TypeLayout)
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
            int alignment = GetClassAlignmentRequirement(defType);
            ReadyToRunTypeLayoutFlags flags = ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_Alignment | ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_GCLayout;
            if (alignment == pointerSize)
            {
                flags |= ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_Alignment_Native;
            }

            if (!defType.ContainsGCPointers)
            {
                flags |= ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_GCLayout_Empty;
            }

            if (defType.IsHfa)
            {
                flags |= ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_HFA;
            }

            dataBuilder.EmitUInt((uint)flags);
            dataBuilder.EmitUInt((uint)size);

            if (defType.IsHfa)
            {
                switch (defType.HfaElementType.Category)
                {
                    case TypeFlags.Single:
                        dataBuilder.EmitUInt((uint)CorElementType.ELEMENT_TYPE_R4);
                        break;
                    case TypeFlags.Double:
                        dataBuilder.EmitUInt((uint)CorElementType.ELEMENT_TYPE_R8);
                        break;
                }
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

        /// <summary>
        /// Managed implementation of CEEInfo::getClassAlignmentRequirementStatic
        /// </summary>
        private static int GetClassAlignmentRequirement(MetadataType type)
        {
            int alignment = type.Context.Target.PointerSize;

            if (type.HasLayout())
            {
                if (type.IsSequentialLayout || MarshalUtils.IsBlittableType(type))
                {
                    alignment = type.InstanceFieldAlignment.AsInt;
                }
            }

            if (type.Context.Target.Architecture == TargetArchitecture.ARM &&
                alignment < 8 && type.RequiresAlign8())
            {
                // If the structure contains 64-bit primitive fields and the platform requires 8-byte alignment for
                // such fields then make sure we return at least 8-byte alignment. Note that it's technically possible
                // to create unmanaged APIs that take unaligned structures containing such fields and this
                // unconditional alignment bump would cause us to get the calling convention wrong on platforms such
                // as ARM. If we see such cases in the future we'd need to add another control (such as an alignment
                // property for the StructLayout attribute or a marshaling directive attribute for p/invoke arguments)
                // that allows more precise control. For now we'll go with the likely scenario.
                alignment = 8;
            }

            return alignment;
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

            if (_typeDesc.HasInstantiation && !_typeDesc.IsGenericDefinition)
            {
                dependencies.Add(factory.AllMethodsOnType(_typeDesc), "Methods on generic type instantiation");
            }
            return dependencies;
        }
    }
}
