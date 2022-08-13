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

        private readonly FieldWithToken _fieldWithToken;

        public FieldFixupSignature(ReadyToRunFixupKind fixupKind, FieldWithToken fieldWithToken, NodeFactory factory)
        {
            _fixupKind = fixupKind;
            _fieldWithToken = fieldWithToken;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            ((CompilerTypeSystemContext)fieldWithToken.Field.Context).EnsureLoadableType(fieldWithToken.Field.OwningType);
        }

        public override int ClassCode => 271828182;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                IEcmaModule targetModule = _fieldWithToken.Token.Module;
                SignatureContext innerContext = dataBuilder.EmitFixup(factory, _fixupKind, targetModule, factory.SignatureContext);
                uint baseOffset = 0;
                uint fieldOffset = (uint)_fieldWithToken.Field.Offset.AsInt;

                if (_fixupKind == ReadyToRunFixupKind.Verify_FieldOffset)
                {
                    TypeDesc baseType = _fieldWithToken.Field.OwningType.BaseType;
                    if ((_fieldWithToken.Field.OwningType.BaseType != null)
                        && !_fieldWithToken.Field.IsStatic
                        && !_fieldWithToken.Field.OwningType.IsValueType)
                    {
                        MetadataType owningType = (MetadataType)_fieldWithToken.Field.OwningType;
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

                dataBuilder.EmitFieldSignature(_fieldWithToken, innerContext);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"FieldFixupSignature({_fixupKind.ToString()}): ");
            _fieldWithToken.AppendMangledName(nameMangler, sb);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            FieldFixupSignature otherNode = (FieldFixupSignature)other;
            int result = ((int)_fixupKind).CompareTo((int)otherNode._fixupKind);
            if (result != 0)
                return result;

            return _fieldWithToken.CompareTo(otherNode._fieldWithToken, comparer);
        }
    }
}
