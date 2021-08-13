// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class FieldFixupSignature : Signature
    {
        public const int MaxCheckableOffset = 0x1FFFFFFF;
        private readonly ReadyToRunFixupKind _fixupKind;

        private readonly FieldDesc _fieldDesc;

        public FieldFixupSignature(ReadyToRunFixupKind fixupKind, FieldDesc fieldDesc, NodeFactory factory)
        {
            _fixupKind = fixupKind;
            _fieldDesc = fieldDesc;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            ((CompilerTypeSystemContext)fieldDesc.Context).EnsureLoadableType(fieldDesc.OwningType);
            Debug.Assert(factory.SignatureContext.GetTargetModule(_fieldDesc) != null);
        }

        public override int ClassCode => 271828182;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                EcmaModule targetModule = factory.SignatureContext.GetTargetModule(_fieldDesc);
                SignatureContext innerContext = dataBuilder.EmitFixup(factory, _fixupKind, targetModule, factory.SignatureContext);
                uint baseOffset = 0;
                uint fieldOffset = (uint)_fieldDesc.Offset.AsInt;

                if (_fixupKind == ReadyToRunFixupKind.Verify_FieldOffset)
                {
                    TypeDesc baseType = _fieldDesc.OwningType.BaseType;
                    if ((_fieldDesc.OwningType.BaseType != null)
                        && !_fieldDesc.IsStatic
                        && !_fieldDesc.OwningType.IsValueType)
                    {
                        MetadataType owningType = (MetadataType)_fieldDesc.OwningType;
                        baseOffset = (uint)owningType.FieldBaseOffset().AsInt;
                        if (factory.CompilationModuleGroup.NeedsAlignmentBetweenBaseTypeAndDerived((MetadataType)baseType, owningType))
                        {
                            fieldOffset -= baseOffset;
                            baseOffset = 0;
                        }
                    }
                    dataBuilder.EmitUInt(baseOffset);
                }

                if ((_fixupKind == ReadyToRunFixupKind.Check_FieldOffset) ||
                    (_fixupKind == ReadyToRunFixupKind.Verify_FieldOffset))
                {
                    dataBuilder.EmitUInt(fieldOffset);
                }

                dataBuilder.EmitFieldSignature(_fieldDesc, innerContext);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"FieldFixupSignature({_fixupKind.ToString()}): ");
            sb.Append(nameMangler.GetMangledFieldName(_fieldDesc));
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            FieldFixupSignature otherNode = (FieldFixupSignature)other;
            int result = ((int)_fixupKind).CompareTo((int)otherNode._fixupKind);
            if (result != 0)
                return result;

            return comparer.Compare(_fieldDesc, otherNode._fieldDesc);
        }
    }
}
