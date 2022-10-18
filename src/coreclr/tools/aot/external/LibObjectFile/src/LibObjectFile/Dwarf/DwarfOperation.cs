// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class DwarfOperation : DwarfObject<DwarfExpression>
    {
        public DwarfOperationKindEx Kind { get; set; }

        public object Operand0 { get; set; }

        public DwarfInteger Operand1;

        public DwarfInteger Operand2;

        private string DebuggerDisplay => $"{Kind} {Operand1} {Operand2} {Operand0}";

        protected override void Read(DwarfReader reader)
        {
            Offset = reader.Offset;
            var kind = new DwarfOperationKindEx(reader.ReadU8());
            Kind = kind;

            switch (kind.Value)
            {
                case DwarfOperationKind.Addr:
                    Operand1.U64 = reader.ReadUInt();
                    break;
                case DwarfOperationKind.Const1u:
                    Operand1.U64 = reader.ReadU8();
                    break;
                case DwarfOperationKind.Const1s:
                    Operand1.I64 = reader.ReadI8();
                    break;
                case DwarfOperationKind.Const2u:
                    Operand1.U64 = reader.ReadU16();
                    break;
                case DwarfOperationKind.Const2s:
                    Operand1.I64 = reader.ReadI16();
                    break;

                case DwarfOperationKind.Const4u:
                    Operand1.U64 = reader.ReadU32();
                    break;
                case DwarfOperationKind.Const4s:
                    Operand1.I64 = reader.ReadU32();
                    break;

                case DwarfOperationKind.Const8u:
                    Operand1.U64 = reader.ReadU64();
                    break;

                case DwarfOperationKind.Const8s:
                    Operand1.I64 = reader.ReadI64();
                    break;

                case DwarfOperationKind.Constu:
                    Operand1.U64 = reader.ReadULEB128();
                    break;

                case DwarfOperationKind.Consts:
                    Operand1.I64 = reader.ReadILEB128();
                    break;

                case DwarfOperationKind.Deref:
                case DwarfOperationKind.Dup:
                case DwarfOperationKind.Drop:
                case DwarfOperationKind.Over:
                case DwarfOperationKind.Swap:
                case DwarfOperationKind.Rot:
                case DwarfOperationKind.Xderef:
                case DwarfOperationKind.Abs:
                case DwarfOperationKind.And:
                case DwarfOperationKind.Div:
                case DwarfOperationKind.Minus:
                case DwarfOperationKind.Mod:
                case DwarfOperationKind.Mul:
                case DwarfOperationKind.Neg:
                case DwarfOperationKind.Not:
                case DwarfOperationKind.Or:
                case DwarfOperationKind.Plus:
                case DwarfOperationKind.Shl:
                case DwarfOperationKind.Shr:
                case DwarfOperationKind.Shra:
                case DwarfOperationKind.Xor:
                case DwarfOperationKind.Eq:
                case DwarfOperationKind.Ge:
                case DwarfOperationKind.Gt:
                case DwarfOperationKind.Le:
                case DwarfOperationKind.Lt:
                case DwarfOperationKind.Ne:
                case DwarfOperationKind.Nop:
                case DwarfOperationKind.PushObjectAddress:
                case DwarfOperationKind.FormTlsAddress:
                case DwarfOperationKind.CallFrameCfa:
                    break;

                case DwarfOperationKind.Pick:
                    Operand1.U64 = reader.ReadU8();
                    break;

                case DwarfOperationKind.PlusUconst:
                    Operand1.U64 = reader.ReadULEB128();
                    break;

                case DwarfOperationKind.Bra:
                case DwarfOperationKind.Skip:
                    // TODO: resolve branches to DwarfOperation
                    Operand1.I64 = reader.ReadI16();
                    break;

                case DwarfOperationKind.Lit0:
                case DwarfOperationKind.Lit1:
                case DwarfOperationKind.Lit2:
                case DwarfOperationKind.Lit3:
                case DwarfOperationKind.Lit4:
                case DwarfOperationKind.Lit5:
                case DwarfOperationKind.Lit6:
                case DwarfOperationKind.Lit7:
                case DwarfOperationKind.Lit8:
                case DwarfOperationKind.Lit9:
                case DwarfOperationKind.Lit10:
                case DwarfOperationKind.Lit11:
                case DwarfOperationKind.Lit12:
                case DwarfOperationKind.Lit13:
                case DwarfOperationKind.Lit14:
                case DwarfOperationKind.Lit15:
                case DwarfOperationKind.Lit16:
                case DwarfOperationKind.Lit17:
                case DwarfOperationKind.Lit18:
                case DwarfOperationKind.Lit19:
                case DwarfOperationKind.Lit20:
                case DwarfOperationKind.Lit21:
                case DwarfOperationKind.Lit22:
                case DwarfOperationKind.Lit23:
                case DwarfOperationKind.Lit24:
                case DwarfOperationKind.Lit25:
                case DwarfOperationKind.Lit26:
                case DwarfOperationKind.Lit27:
                case DwarfOperationKind.Lit28:
                case DwarfOperationKind.Lit29:
                case DwarfOperationKind.Lit30:
                case DwarfOperationKind.Lit31:
                    Operand1.U64 = (ulong)((byte)kind.Value - (byte)DwarfOperationKind.Lit0);
                    break;

                case DwarfOperationKind.Reg0:
                case DwarfOperationKind.Reg1:
                case DwarfOperationKind.Reg2:
                case DwarfOperationKind.Reg3:
                case DwarfOperationKind.Reg4:
                case DwarfOperationKind.Reg5:
                case DwarfOperationKind.Reg6:
                case DwarfOperationKind.Reg7:
                case DwarfOperationKind.Reg8:
                case DwarfOperationKind.Reg9:
                case DwarfOperationKind.Reg10:
                case DwarfOperationKind.Reg11:
                case DwarfOperationKind.Reg12:
                case DwarfOperationKind.Reg13:
                case DwarfOperationKind.Reg14:
                case DwarfOperationKind.Reg15:
                case DwarfOperationKind.Reg16:
                case DwarfOperationKind.Reg17:
                case DwarfOperationKind.Reg18:
                case DwarfOperationKind.Reg19:
                case DwarfOperationKind.Reg20:
                case DwarfOperationKind.Reg21:
                case DwarfOperationKind.Reg22:
                case DwarfOperationKind.Reg23:
                case DwarfOperationKind.Reg24:
                case DwarfOperationKind.Reg25:
                case DwarfOperationKind.Reg26:
                case DwarfOperationKind.Reg27:
                case DwarfOperationKind.Reg28:
                case DwarfOperationKind.Reg29:
                case DwarfOperationKind.Reg30:
                case DwarfOperationKind.Reg31:
                    Operand1.U64 = (ulong)kind.Value - (ulong)DwarfOperationKind.Reg0;
                    break;

                case DwarfOperationKind.Breg0:
                case DwarfOperationKind.Breg1:
                case DwarfOperationKind.Breg2:
                case DwarfOperationKind.Breg3:
                case DwarfOperationKind.Breg4:
                case DwarfOperationKind.Breg5:
                case DwarfOperationKind.Breg6:
                case DwarfOperationKind.Breg7:
                case DwarfOperationKind.Breg8:
                case DwarfOperationKind.Breg9:
                case DwarfOperationKind.Breg10:
                case DwarfOperationKind.Breg11:
                case DwarfOperationKind.Breg12:
                case DwarfOperationKind.Breg13:
                case DwarfOperationKind.Breg14:
                case DwarfOperationKind.Breg15:
                case DwarfOperationKind.Breg16:
                case DwarfOperationKind.Breg17:
                case DwarfOperationKind.Breg18:
                case DwarfOperationKind.Breg19:
                case DwarfOperationKind.Breg20:
                case DwarfOperationKind.Breg21:
                case DwarfOperationKind.Breg22:
                case DwarfOperationKind.Breg23:
                case DwarfOperationKind.Breg24:
                case DwarfOperationKind.Breg25:
                case DwarfOperationKind.Breg26:
                case DwarfOperationKind.Breg27:
                case DwarfOperationKind.Breg28:
                case DwarfOperationKind.Breg29:
                case DwarfOperationKind.Breg30:
                case DwarfOperationKind.Breg31:
                    Operand1.U64 = (ulong)kind.Value - (ulong)DwarfOperationKind.Breg0;
                    Operand2.I64 = reader.ReadILEB128();
                    break;

                case DwarfOperationKind.Regx:
                    Operand1.U64 = reader.ReadULEB128();
                    break;

                case DwarfOperationKind.Fbreg:
                    Operand1.I64 = reader.ReadILEB128();
                    break;

                case DwarfOperationKind.Bregx:
                    Operand1.U64 = reader.ReadULEB128();
                    Operand2.I64 = reader.ReadILEB128();
                    break;

                case DwarfOperationKind.Piece:
                    Operand1.U64 = reader.ReadULEB128();
                    break;

                case DwarfOperationKind.DerefSize:
                    Operand1.U64 = reader.ReadU8();
                    break;

                case DwarfOperationKind.XderefSize:
                    Operand1.U64 = reader.ReadU8();
                    break;

                case DwarfOperationKind.Call2:
                {
                    var offset = reader.ReadU16();
                    var dieRef = new DwarfReader.DwarfDIEReference(offset, this, DwarfExpressionLocationDIEReferenceResolverInstance);
                    reader.ResolveAttributeReferenceWithinSection(dieRef, false);
                    break;
                }

                case DwarfOperationKind.Call4:
                {
                    var offset = reader.ReadU32();
                    var dieRef = new DwarfReader.DwarfDIEReference(offset, this, DwarfExpressionLocationDIEReferenceResolverInstance);
                    reader.ResolveAttributeReferenceWithinSection(dieRef, false);
                    break;
                }

                case DwarfOperationKind.CallRef:
                {
                    var offset = reader.ReadUIntFromEncoding();
                    var dieRef = new DwarfReader.DwarfDIEReference(offset, this, DwarfExpressionLocationDIEReferenceResolverInstance);
                    reader.ResolveAttributeReferenceWithinSection(dieRef, false);
                    break;
                }

                case DwarfOperationKind.BitPiece:
                    Operand1.U64 = reader.ReadULEB128();
                    Operand2.U64 = reader.ReadULEB128();
                    break;

                case DwarfOperationKind.ImplicitValue:
                {
                    var length = reader.ReadULEB128();
                    Operand0 = reader.ReadAsStream(length);
                    break;
                }

                case DwarfOperationKind.StackValue:
                    break;

                case DwarfOperationKind.ImplicitPointer:
                case DwarfOperationKind.GNUImplicitPointer:
                {
                    ulong offset;
                    //  a reference to a debugging information entry that describes the dereferenced object’s value
                    if (reader.CurrentUnit.Version == 2)
                    {
                        offset = reader.ReadUInt();
                    }
                    else
                    {
                        offset = reader.ReadUIntFromEncoding();
                    }
                    //  a signed number that is treated as a byte offset from the start of that value
                    Operand1.I64 = reader.ReadILEB128();

                    if (offset != 0)
                    {
                        var dieRef = new DwarfReader.DwarfDIEReference(offset, this, DwarfExpressionLocationDIEReferenceResolverInstance);
                        reader.ResolveAttributeReferenceWithinSection(dieRef, false);
                    }
                    break;
                }

                case DwarfOperationKind.Addrx:
                case DwarfOperationKind.GNUAddrIndex:
                case DwarfOperationKind.Constx:
                case DwarfOperationKind.GNUConstIndex:
                    Operand1.U64 = reader.ReadULEB128();
                    break;

                case DwarfOperationKind.EntryValue:
                case DwarfOperationKind.GNUEntryValue:
                {
                    var subExpression = new DwarfExpression();
                    subExpression.ReadInternal(reader);
                    Operand0 = subExpression;
                    break;
                }

                case DwarfOperationKind.ConstType:
                case DwarfOperationKind.GNUConstType:
                {
                    // The DW_OP_const_type operation takes three operands

                    // The first operand is an unsigned LEB128 integer that represents the offset
                    // of a debugging information entry in the current compilation unit, which
                    // must be a DW_TAG_base_type entry that provides the type of the constant provided
                    var offset = reader.ReadULEB128();
                    if (offset != 0)
                    {
                        var dieRef = new DwarfReader.DwarfDIEReference(offset, this, DwarfExpressionLocationDIEReferenceResolverInstance);
                        reader.ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                    }
                    Operand1.U64 = ReadEncodedValue(reader, kind, out var sizeOfEncodedValue);
                    // Encode size of encoded value in Operand1
                    Operand2.U64 = sizeOfEncodedValue;
                    break;
                }

                case DwarfOperationKind.RegvalType:
                case DwarfOperationKind.GNURegvalType:
                {
                    // The DW_OP_regval_type operation provides the contents of a given register
                    // interpreted as a value of a given type

                    // The first operand is an unsigned LEB128 number, which identifies a register
                    // whose contents is to be pushed onto the stack
                    Operand1.U64 = reader.ReadULEB128();

                    // The second operand is an unsigned LEB128 number that represents the offset
                    // of a debugging information entry in the current compilation unit
                    var offset = reader.ReadULEB128();
                    if (offset != 0)
                    {
                        var dieRef = new DwarfReader.DwarfDIEReference(offset, this, DwarfExpressionLocationDIEReferenceResolverInstance);
                        reader.ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                    }
                    break;
                }

                case DwarfOperationKind.DerefType:
                case DwarfOperationKind.GNUDerefType:
                case DwarfOperationKind.XderefType:
                {
                    // The DW_OP_deref_type operation behaves like the DW_OP_deref_size operation:
                    // it pops the top stack entry and treats it as an address.

                    // This operand is a 1-byte unsigned integral constant whose value which is the
                    // same as the size of the base type referenced by the second operand
                    Operand1.U64 = reader.ReadU8();

                    // The second operand is an unsigned LEB128 number that represents the offset
                    // of a debugging information entry in the current compilation unit
                    var offset = reader.ReadULEB128();
                    if (offset != 0)
                    {
                        var dieRef = new DwarfReader.DwarfDIEReference(offset, this, DwarfExpressionLocationDIEReferenceResolverInstance);
                        reader.ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                    }
                    break;
                }

                case DwarfOperationKind.Convert:
                case DwarfOperationKind.GNUConvert:
                case DwarfOperationKind.Reinterpret:
                case DwarfOperationKind.GNUReinterpret:
                {
                    ulong offset = reader.ReadULEB128();
                    if (offset != 0)
                    {
                        var dieRef = new DwarfReader.DwarfDIEReference(offset, this, DwarfExpressionLocationDIEReferenceResolverInstance);
                        reader.ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                    }
                    break;
                }

                case DwarfOperationKind.GNUPushTlsAddress:
                case DwarfOperationKind.GNUUninit:
                    break;

                case DwarfOperationKind.GNUEncodedAddr:
                {
                    Operand1.U64 = ReadEncodedValue(reader, kind, out var sizeOfEncodedValue);
                    Operand2.U64 = sizeOfEncodedValue;
                    break;
                }

                case DwarfOperationKind.GNUParameterRef:
                    Operand1.U64 = reader.ReadU32();
                    break;

                default:
                    throw new NotSupportedException($"The {nameof(DwarfOperationKind)} {kind} is not supported");
            }

            // Store the size of the current op
            Size = reader.Offset - Offset;
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            var endOffset = Offset;
            // 1 byte per opcode
            endOffset += 1;

            switch (Kind.Value)
            {
                case DwarfOperationKind.Addr:
                    endOffset += DwarfHelper.SizeOfUInt(layoutContext.CurrentUnit.AddressSize);
                    break;
                case DwarfOperationKind.Const1u:
                case DwarfOperationKind.Const1s:
                case DwarfOperationKind.Pick:
                case DwarfOperationKind.DerefSize:
                case DwarfOperationKind.XderefSize:
                    endOffset += 1;
                    break;
                case DwarfOperationKind.Const2u:
                case DwarfOperationKind.Const2s:
                case DwarfOperationKind.Bra:
                case DwarfOperationKind.Skip:
                case DwarfOperationKind.Call2:
                    endOffset += 2;
                    break;
                case DwarfOperationKind.Const4u:
                case DwarfOperationKind.Const4s:
                case DwarfOperationKind.Call4:
                    endOffset += 4;
                    break;
                case DwarfOperationKind.Const8u:
                case DwarfOperationKind.Const8s:
                    endOffset += 8;
                    break;

                case DwarfOperationKind.Constu:
                case DwarfOperationKind.PlusUconst:
                case DwarfOperationKind.Regx:
                case DwarfOperationKind.Piece:
                case DwarfOperationKind.Addrx:
                case DwarfOperationKind.GNUAddrIndex:
                case DwarfOperationKind.Constx:
                case DwarfOperationKind.GNUConstIndex:
                    endOffset += DwarfHelper.SizeOfULEB128(Operand1.U64);
                    break;

                case DwarfOperationKind.Consts:
                case DwarfOperationKind.Fbreg:
                    endOffset += DwarfHelper.SizeOfILEB128(Operand1.I64);
                    break;

                case DwarfOperationKind.Deref:
                case DwarfOperationKind.Dup:
                case DwarfOperationKind.Drop:
                case DwarfOperationKind.Over:
                case DwarfOperationKind.Swap:
                case DwarfOperationKind.Rot:
                case DwarfOperationKind.Xderef:
                case DwarfOperationKind.Abs:
                case DwarfOperationKind.And:
                case DwarfOperationKind.Div:
                case DwarfOperationKind.Minus:
                case DwarfOperationKind.Mod:
                case DwarfOperationKind.Mul:
                case DwarfOperationKind.Neg:
                case DwarfOperationKind.Not:
                case DwarfOperationKind.Or:
                case DwarfOperationKind.Plus:
                case DwarfOperationKind.Shl:
                case DwarfOperationKind.Shr:
                case DwarfOperationKind.Shra:
                case DwarfOperationKind.Xor:
                case DwarfOperationKind.Eq:
                case DwarfOperationKind.Ge:
                case DwarfOperationKind.Gt:
                case DwarfOperationKind.Le:
                case DwarfOperationKind.Lt:
                case DwarfOperationKind.Ne:
                case DwarfOperationKind.Nop:
                case DwarfOperationKind.PushObjectAddress:
                case DwarfOperationKind.FormTlsAddress:
                case DwarfOperationKind.CallFrameCfa:
                case DwarfOperationKind.Lit0:
                case DwarfOperationKind.Lit1:
                case DwarfOperationKind.Lit2:
                case DwarfOperationKind.Lit3:
                case DwarfOperationKind.Lit4:
                case DwarfOperationKind.Lit5:
                case DwarfOperationKind.Lit6:
                case DwarfOperationKind.Lit7:
                case DwarfOperationKind.Lit8:
                case DwarfOperationKind.Lit9:
                case DwarfOperationKind.Lit10:
                case DwarfOperationKind.Lit11:
                case DwarfOperationKind.Lit12:
                case DwarfOperationKind.Lit13:
                case DwarfOperationKind.Lit14:
                case DwarfOperationKind.Lit15:
                case DwarfOperationKind.Lit16:
                case DwarfOperationKind.Lit17:
                case DwarfOperationKind.Lit18:
                case DwarfOperationKind.Lit19:
                case DwarfOperationKind.Lit20:
                case DwarfOperationKind.Lit21:
                case DwarfOperationKind.Lit22:
                case DwarfOperationKind.Lit23:
                case DwarfOperationKind.Lit24:
                case DwarfOperationKind.Lit25:
                case DwarfOperationKind.Lit26:
                case DwarfOperationKind.Lit27:
                case DwarfOperationKind.Lit28:
                case DwarfOperationKind.Lit29:
                case DwarfOperationKind.Lit30:
                case DwarfOperationKind.Lit31:
                case DwarfOperationKind.Reg0:
                case DwarfOperationKind.Reg1:
                case DwarfOperationKind.Reg2:
                case DwarfOperationKind.Reg3:
                case DwarfOperationKind.Reg4:
                case DwarfOperationKind.Reg5:
                case DwarfOperationKind.Reg6:
                case DwarfOperationKind.Reg7:
                case DwarfOperationKind.Reg8:
                case DwarfOperationKind.Reg9:
                case DwarfOperationKind.Reg10:
                case DwarfOperationKind.Reg11:
                case DwarfOperationKind.Reg12:
                case DwarfOperationKind.Reg13:
                case DwarfOperationKind.Reg14:
                case DwarfOperationKind.Reg15:
                case DwarfOperationKind.Reg16:
                case DwarfOperationKind.Reg17:
                case DwarfOperationKind.Reg18:
                case DwarfOperationKind.Reg19:
                case DwarfOperationKind.Reg20:
                case DwarfOperationKind.Reg21:
                case DwarfOperationKind.Reg22:
                case DwarfOperationKind.Reg23:
                case DwarfOperationKind.Reg24:
                case DwarfOperationKind.Reg25:
                case DwarfOperationKind.Reg26:
                case DwarfOperationKind.Reg27:
                case DwarfOperationKind.Reg28:
                case DwarfOperationKind.Reg29:
                case DwarfOperationKind.Reg30:
                case DwarfOperationKind.Reg31:
                case DwarfOperationKind.StackValue:
                case DwarfOperationKind.GNUPushTlsAddress:
                case DwarfOperationKind.GNUUninit:
                    break;

                case DwarfOperationKind.Breg0:
                case DwarfOperationKind.Breg1:
                case DwarfOperationKind.Breg2:
                case DwarfOperationKind.Breg3:
                case DwarfOperationKind.Breg4:
                case DwarfOperationKind.Breg5:
                case DwarfOperationKind.Breg6:
                case DwarfOperationKind.Breg7:
                case DwarfOperationKind.Breg8:
                case DwarfOperationKind.Breg9:
                case DwarfOperationKind.Breg10:
                case DwarfOperationKind.Breg11:
                case DwarfOperationKind.Breg12:
                case DwarfOperationKind.Breg13:
                case DwarfOperationKind.Breg14:
                case DwarfOperationKind.Breg15:
                case DwarfOperationKind.Breg16:
                case DwarfOperationKind.Breg17:
                case DwarfOperationKind.Breg18:
                case DwarfOperationKind.Breg19:
                case DwarfOperationKind.Breg20:
                case DwarfOperationKind.Breg21:
                case DwarfOperationKind.Breg22:
                case DwarfOperationKind.Breg23:
                case DwarfOperationKind.Breg24:
                case DwarfOperationKind.Breg25:
                case DwarfOperationKind.Breg26:
                case DwarfOperationKind.Breg27:
                case DwarfOperationKind.Breg28:
                case DwarfOperationKind.Breg29:
                case DwarfOperationKind.Breg30:
                case DwarfOperationKind.Breg31:
                    endOffset += DwarfHelper.SizeOfILEB128(Operand2.I64);
                    break;

                case DwarfOperationKind.Bregx:
                    endOffset += DwarfHelper.SizeOfULEB128(Operand1.U64);
                    endOffset += DwarfHelper.SizeOfILEB128(Operand2.I64);
                    break;

                case DwarfOperationKind.CallRef:
                    endOffset += DwarfHelper.SizeOfUInt(layoutContext.CurrentUnit.AddressSize);
                    break;

                case DwarfOperationKind.BitPiece:
                    endOffset += DwarfHelper.SizeOfULEB128(Operand1.U64);
                    endOffset += DwarfHelper.SizeOfULEB128(Operand2.U64);
                    break;

                case DwarfOperationKind.ImplicitValue:
                    if (Operand0 == null)
                    {
                        layoutContext.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The object operand of implicit value operation {this} from DIE cannot be null.");
                    }
                    else if (Operand0 is Stream stream)
                    {
                        var streamSize = (ulong)stream.Length;
                        endOffset += DwarfHelper.SizeOfULEB128(streamSize);
                        endOffset += streamSize;
                    }
                    else
                    {
                        layoutContext.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The object operand of implicit value operation {this} must be a System.IO.Stream.");
                    }

                    break;

                case DwarfOperationKind.ImplicitPointer:
                case DwarfOperationKind.GNUImplicitPointer:
                    //  a reference to a debugging information entry that describes the dereferenced object’s value
                    if (layoutContext.CurrentUnit.Version == 2)
                    {
                        endOffset += DwarfHelper.SizeOfUInt(layoutContext.CurrentUnit.AddressSize);
                    }
                    else
                    {
                        endOffset += DwarfHelper.SizeOfUInt(layoutContext.CurrentUnit.Is64BitEncoding);
                    }

                    //  a signed number that is treated as a byte offset from the start of that value
                    endOffset += DwarfHelper.SizeOfILEB128(Operand1.I64);
                    break;

                case DwarfOperationKind.EntryValue:
                case DwarfOperationKind.GNUEntryValue:
                    if (Operand0 == null)
                    {
                        endOffset += DwarfHelper.SizeOfULEB128(0);
                    }
                    else if (Operand0 is DwarfExpression expr)
                    {
                        expr.Offset = endOffset;
                        expr.UpdateLayoutInternal(layoutContext);
                        endOffset += DwarfHelper.SizeOfULEB128(expr.Size);
                    }
                    else
                    {
                        layoutContext.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The object operand of EntryValue operation {this} must be a {nameof(DwarfExpression)} instead of {Operand0.GetType()}.");
                    }

                    break;

                case DwarfOperationKind.ConstType:
                case DwarfOperationKind.GNUConstType:
                    {
                        // The DW_OP_const_type operation takes three operands

                        // The first operand is an unsigned LEB128 integer that represents the offset
                        // of a debugging information entry in the current compilation unit, which
                        // must be a DW_TAG_base_type entry that provides the type of the constant provided

                        endOffset += SizeOfDIEReference(layoutContext);
                        endOffset += SizeOfEncodedValue(Kind, Operand1.U64, (byte)Operand2.U64, layoutContext.CurrentUnit.AddressSize);
                        break;
                    }

                case DwarfOperationKind.RegvalType:
                case DwarfOperationKind.GNURegvalType:
                    {
                        // The DW_OP_regval_type operation provides the contents of a given register
                        // interpreted as a value of a given type

                        // The first operand is an unsigned LEB128 number, which identifies a register
                        // whose contents is to be pushed onto the stack
                        endOffset += DwarfHelper.SizeOfULEB128(Operand1.U64);

                        // The second operand is an unsigned LEB128 number that represents the offset
                        // of a debugging information entry in the current compilation unit
                        endOffset += SizeOfDIEReference(layoutContext);
                        break;
                    }

                case DwarfOperationKind.DerefType:
                case DwarfOperationKind.GNUDerefType:
                case DwarfOperationKind.XderefType:
                    {
                        // The DW_OP_deref_type operation behaves like the DW_OP_deref_size operation:
                        // it pops the top stack entry and treats it as an address.

                        // This operand is a 1-byte unsigned integral constant whose value which is the
                        // same as the size of the base type referenced by the second operand
                        endOffset += 1;

                        // The second operand is an unsigned LEB128 number that represents the offset
                        // of a debugging information entry in the current compilation unit
                        endOffset += SizeOfDIEReference(layoutContext);
                        break;
                    }

                case DwarfOperationKind.Convert:
                case DwarfOperationKind.GNUConvert:
                case DwarfOperationKind.Reinterpret:
                case DwarfOperationKind.GNUReinterpret:
                    endOffset += SizeOfDIEReference(layoutContext);
                    break;

                case DwarfOperationKind.GNUEncodedAddr:
                    endOffset += SizeOfEncodedValue(Kind, Operand1.U64, (byte)Operand2.U64, layoutContext.CurrentUnit.AddressSize);
                    break;

                case DwarfOperationKind.GNUParameterRef:
                    endOffset += 4;
                    break;

                default:
                    throw new NotSupportedException($"The {nameof(DwarfOperationKind)} {Kind} is not supported");
            }

            Size = endOffset - Offset;
        }

        private ulong SizeOfDIEReference(DwarfLayoutContext context)
        {
            if (Operand0 == null)
            {
                return DwarfHelper.SizeOfULEB128(0);
            }
            else if (Operand0 is DwarfDIE die)
            {
                // TODO: check that die reference is within this section

                if (die.Offset < Offset)
                {
                    return DwarfHelper.SizeOfULEB128(die.Offset);
                }
                else
                {
                    // TODO: encode depending on Context.DefaultAttributeFormForReference
                    return DwarfHelper.SizeOfILEB128(uint.MaxValue);
                }
            }
            else
            {
                context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The object operand of {Kind} operation {this} must be a {nameof(DwarfDIE)} instead of {Operand0.GetType()}.");
            }

            return 0U;
        }

        private ulong SizeOfEncodedValue(DwarfOperationKindEx kind, ulong value, byte size, DwarfAddressSize addressSize)
        {
            switch (size)
            {
                case 0:
                    return 1 + DwarfHelper.SizeOfUInt(addressSize);
                case 1:
                    return 1 + 1;
                case 2:
                    return 1 + 2;
                case 4:
                    return 1 + 4;
                case 8:
                    return 1 + 8;
                default:
                    // TODO: report via diagnostics in Verify
                    throw new InvalidOperationException($"Invalid Encoded address size {size} for {kind}");
            }
        }

        protected override void Write(DwarfWriter writer)
        {
            var startOpOffset = Offset;
            Debug.Assert(startOpOffset == Offset);

            writer.WriteU8((byte)Kind);

            switch (Kind.Value)
            {
                case DwarfOperationKind.Addr:
                    writer.WriteAddress(DwarfRelocationTarget.Code, Operand1.U64);
                    break;
                case DwarfOperationKind.Const1u:
                case DwarfOperationKind.Const1s:
                case DwarfOperationKind.Pick:
                case DwarfOperationKind.DerefSize:
                case DwarfOperationKind.XderefSize:
                    writer.WriteU8((byte)Operand1.U64);
                    break;

                case DwarfOperationKind.Const2u:
                case DwarfOperationKind.Const2s:
                    writer.WriteU16((ushort)Operand1.U64);
                    break;

                case DwarfOperationKind.Const4u:
                case DwarfOperationKind.Const4s:
                    writer.WriteU32((uint)Operand1.U64);
                    break;

                case DwarfOperationKind.Const8u:
                case DwarfOperationKind.Const8s:
                    writer.WriteU64(Operand1.U64);
                    break;

                case DwarfOperationKind.Constu:
                case DwarfOperationKind.PlusUconst:
                case DwarfOperationKind.Regx:
                case DwarfOperationKind.Piece:
                case DwarfOperationKind.Addrx:
                case DwarfOperationKind.GNUAddrIndex:
                case DwarfOperationKind.Constx:
                case DwarfOperationKind.GNUConstIndex:
                    writer.WriteULEB128(Operand1.U64);
                    break;

                case DwarfOperationKind.Consts:
                case DwarfOperationKind.Fbreg:
                    writer.WriteILEB128(Operand1.I64);
                    break;

                case DwarfOperationKind.Deref:
                case DwarfOperationKind.Dup:
                case DwarfOperationKind.Drop:
                case DwarfOperationKind.Over:
                case DwarfOperationKind.Swap:
                case DwarfOperationKind.Rot:
                case DwarfOperationKind.Xderef:
                case DwarfOperationKind.Abs:
                case DwarfOperationKind.And:
                case DwarfOperationKind.Div:
                case DwarfOperationKind.Minus:
                case DwarfOperationKind.Mod:
                case DwarfOperationKind.Mul:
                case DwarfOperationKind.Neg:
                case DwarfOperationKind.Not:
                case DwarfOperationKind.Or:
                case DwarfOperationKind.Plus:
                case DwarfOperationKind.Shl:
                case DwarfOperationKind.Shr:
                case DwarfOperationKind.Shra:
                case DwarfOperationKind.Xor:
                case DwarfOperationKind.Eq:
                case DwarfOperationKind.Ge:
                case DwarfOperationKind.Gt:
                case DwarfOperationKind.Le:
                case DwarfOperationKind.Lt:
                case DwarfOperationKind.Ne:
                case DwarfOperationKind.Nop:
                case DwarfOperationKind.PushObjectAddress:
                case DwarfOperationKind.FormTlsAddress:
                case DwarfOperationKind.CallFrameCfa:
                    break;

                case DwarfOperationKind.Bra:
                case DwarfOperationKind.Skip:
                    writer.WriteU16((ushort)((long)Offset + 2 - (long)((DwarfOperation)Operand0).Offset));
                    break;

                case DwarfOperationKind.Lit0:
                case DwarfOperationKind.Lit1:
                case DwarfOperationKind.Lit2:
                case DwarfOperationKind.Lit3:
                case DwarfOperationKind.Lit4:
                case DwarfOperationKind.Lit5:
                case DwarfOperationKind.Lit6:
                case DwarfOperationKind.Lit7:
                case DwarfOperationKind.Lit8:
                case DwarfOperationKind.Lit9:
                case DwarfOperationKind.Lit10:
                case DwarfOperationKind.Lit11:
                case DwarfOperationKind.Lit12:
                case DwarfOperationKind.Lit13:
                case DwarfOperationKind.Lit14:
                case DwarfOperationKind.Lit15:
                case DwarfOperationKind.Lit16:
                case DwarfOperationKind.Lit17:
                case DwarfOperationKind.Lit18:
                case DwarfOperationKind.Lit19:
                case DwarfOperationKind.Lit20:
                case DwarfOperationKind.Lit21:
                case DwarfOperationKind.Lit22:
                case DwarfOperationKind.Lit23:
                case DwarfOperationKind.Lit24:
                case DwarfOperationKind.Lit25:
                case DwarfOperationKind.Lit26:
                case DwarfOperationKind.Lit27:
                case DwarfOperationKind.Lit28:
                case DwarfOperationKind.Lit29:
                case DwarfOperationKind.Lit30:
                case DwarfOperationKind.Lit31:
                case DwarfOperationKind.Reg0:
                case DwarfOperationKind.Reg1:
                case DwarfOperationKind.Reg2:
                case DwarfOperationKind.Reg3:
                case DwarfOperationKind.Reg4:
                case DwarfOperationKind.Reg5:
                case DwarfOperationKind.Reg6:
                case DwarfOperationKind.Reg7:
                case DwarfOperationKind.Reg8:
                case DwarfOperationKind.Reg9:
                case DwarfOperationKind.Reg10:
                case DwarfOperationKind.Reg11:
                case DwarfOperationKind.Reg12:
                case DwarfOperationKind.Reg13:
                case DwarfOperationKind.Reg14:
                case DwarfOperationKind.Reg15:
                case DwarfOperationKind.Reg16:
                case DwarfOperationKind.Reg17:
                case DwarfOperationKind.Reg18:
                case DwarfOperationKind.Reg19:
                case DwarfOperationKind.Reg20:
                case DwarfOperationKind.Reg21:
                case DwarfOperationKind.Reg22:
                case DwarfOperationKind.Reg23:
                case DwarfOperationKind.Reg24:
                case DwarfOperationKind.Reg25:
                case DwarfOperationKind.Reg26:
                case DwarfOperationKind.Reg27:
                case DwarfOperationKind.Reg28:
                case DwarfOperationKind.Reg29:
                case DwarfOperationKind.Reg30:
                case DwarfOperationKind.Reg31:
                case DwarfOperationKind.StackValue:
                    break;

                case DwarfOperationKind.Breg0:
                case DwarfOperationKind.Breg1:
                case DwarfOperationKind.Breg2:
                case DwarfOperationKind.Breg3:
                case DwarfOperationKind.Breg4:
                case DwarfOperationKind.Breg5:
                case DwarfOperationKind.Breg6:
                case DwarfOperationKind.Breg7:
                case DwarfOperationKind.Breg8:
                case DwarfOperationKind.Breg9:
                case DwarfOperationKind.Breg10:
                case DwarfOperationKind.Breg11:
                case DwarfOperationKind.Breg12:
                case DwarfOperationKind.Breg13:
                case DwarfOperationKind.Breg14:
                case DwarfOperationKind.Breg15:
                case DwarfOperationKind.Breg16:
                case DwarfOperationKind.Breg17:
                case DwarfOperationKind.Breg18:
                case DwarfOperationKind.Breg19:
                case DwarfOperationKind.Breg20:
                case DwarfOperationKind.Breg21:
                case DwarfOperationKind.Breg22:
                case DwarfOperationKind.Breg23:
                case DwarfOperationKind.Breg24:
                case DwarfOperationKind.Breg25:
                case DwarfOperationKind.Breg26:
                case DwarfOperationKind.Breg27:
                case DwarfOperationKind.Breg28:
                case DwarfOperationKind.Breg29:
                case DwarfOperationKind.Breg30:
                case DwarfOperationKind.Breg31:
                    writer.WriteILEB128(Operand2.I64);
                    break;

                case DwarfOperationKind.Bregx:
                    writer.WriteULEB128(Operand1.U64);
                    writer.WriteILEB128(Operand2.I64);
                    break;

                case DwarfOperationKind.Call2:
                    writer.WriteU16((ushort)((DwarfDIE)Operand0).Offset);
                    break;

                case DwarfOperationKind.Call4:
                    writer.WriteU32((uint)((DwarfDIE)Operand0).Offset);
                    break;

                case DwarfOperationKind.CallRef:
                    writer.WriteUInt(((DwarfDIE)Operand0).Offset);
                    break;

                case DwarfOperationKind.BitPiece:
                    writer.WriteULEB128(Operand1.U64);
                    writer.WriteULEB128(Operand2.U64);
                    break;

                case DwarfOperationKind.ImplicitValue:
                    {
                        var stream = (Stream)Operand0;
                        writer.WriteULEB128((ulong)stream.Position);
                        writer.Write(stream);
                        break;
                    }

                case DwarfOperationKind.ImplicitPointer:
                case DwarfOperationKind.GNUImplicitPointer:
                    {
                        ulong offset = ((DwarfDIE)Operand0).Offset;
                        //  a reference to a debugging information entry that describes the dereferenced object’s value
                        if (writer.CurrentUnit.Version == 2)
                        {
                            writer.WriteUInt(offset);
                        }
                        else
                        {
                            writer.WriteUIntFromEncoding(offset);
                        }
                        //  a signed number that is treated as a byte offset from the start of that value
                        writer.WriteILEB128(Operand1.I64);
                        break;
                    }

                case DwarfOperationKind.EntryValue:
                case DwarfOperationKind.GNUEntryValue:
                    {
                        var expression = (DwarfExpression)Operand0;
                        writer.WriteULEB128(expression.Size);
                        expression.WriteInternal(writer);
                        break;
                    }

                case DwarfOperationKind.ConstType:
                case DwarfOperationKind.GNUConstType:
                    {
                        // The DW_OP_const_type operation takes three operands

                        // The first operand is an unsigned LEB128 integer that represents the offset
                        // of a debugging information entry in the current compilation unit, which
                        // must be a DW_TAG_base_type entry that provides the type of the constant provided
                        writer.WriteULEB128(((DwarfDIE)Operand0).Offset);
                        WriteEncodedValue(writer, Kind, Operand1.U64, (byte)Operand2.U64);
                        break;
                    }

                case DwarfOperationKind.RegvalType:
                case DwarfOperationKind.GNURegvalType:
                    {
                        // The DW_OP_regval_type operation provides the contents of a given register
                        // interpreted as a value of a given type

                        // The first operand is an unsigned LEB128 number, which identifies a register
                        // whose contents is to be pushed onto the stack
                        writer.WriteULEB128(Operand1.U64);

                        // The second operand is an unsigned LEB128 number that represents the offset
                        // of a debugging information entry in the current compilation unit
                        writer.WriteULEB128(((DwarfDIE)Operand0).Offset);
                        break;
                    }

                case DwarfOperationKind.DerefType:
                case DwarfOperationKind.GNUDerefType:
                case DwarfOperationKind.XderefType:
                    {
                        // The DW_OP_deref_type operation behaves like the DW_OP_deref_size operation:
                        // it pops the top stack entry and treats it as an address.

                        // This operand is a 1-byte unsigned integral constant whose value which is the
                        // same as the size of the base type referenced by the second operand
                        writer.WriteU8((byte)Operand1.U64);

                        // The second operand is an unsigned LEB128 number that represents the offset
                        // of a debugging information entry in the current compilation unit
                        writer.WriteULEB128(((DwarfDIE)Operand0).Offset);
                        break;
                    }

                case DwarfOperationKind.Convert:
                case DwarfOperationKind.GNUConvert:
                case DwarfOperationKind.Reinterpret:
                case DwarfOperationKind.GNUReinterpret:
                    writer.WriteULEB128(((DwarfDIE)Operand0).Offset);
                    break;

                case DwarfOperationKind.GNUPushTlsAddress:
                case DwarfOperationKind.GNUUninit:
                    break;

                case DwarfOperationKind.GNUEncodedAddr:
                    WriteEncodedValue(writer, Kind, Operand1.U64, (byte)Operand2.U64);
                    break;

                case DwarfOperationKind.GNUParameterRef:
                    writer.WriteU32((uint)Operand1.U64);
                    break;

                default:
                    throw new NotSupportedException($"The {nameof(DwarfOperationKind)} {Kind} is not supported");
            }

            Debug.Assert(writer.Offset - startOpOffset == Size);
        }

        private static ulong ReadEncodedValue(DwarfReader reader, DwarfOperationKind kind, out byte size)
        {
            size = reader.ReadU8();
            switch (size)
            {
                case 0:
                    return reader.ReadUInt();
                case 1:
                    return reader.ReadU8();
                case 2:
                    return reader.ReadU16();
                case 4:
                    return reader.ReadU32();
                case 8:
                    return reader.ReadU64();
                default:
                    throw new InvalidOperationException($"Invalid Encoded address size {size} for {kind}");
            }
        }

        private static void WriteEncodedValue(DwarfWriter writer, DwarfOperationKindEx kind, ulong value, byte size)
        {
            writer.WriteU8(size);
            switch (size)
            {
                case 0:
                    writer.WriteUInt(value);
                    break;
                case 1:
                    writer.WriteU8((byte)value);
                    break;
                case 2:
                    writer.WriteU16((ushort)value);
                    break;
                case 4:
                    writer.WriteU32((uint)value);
                    break;
                case 8:
                    writer.WriteU64(value);
                    break;
                default:
                    // TODO: report via diagnostics in Verify
                    throw new InvalidOperationException($"Invalid Encoded address size {size} for {kind}");
            }
        }

        private static readonly DwarfReader.DwarfDIEReferenceResolver DwarfExpressionLocationDIEReferenceResolverInstance = DwarfExpressionLocationDIEReferenceResolver;

        private static void DwarfExpressionLocationDIEReferenceResolver(ref DwarfReader.DwarfDIEReference dieRef)
        {
            var op = (DwarfOperation)dieRef.DwarfObject;
            op.Operand0 = dieRef.Resolved;
        }
    }
}