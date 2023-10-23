// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfAttribute : DwarfObject<DwarfDIE>, IComparable<DwarfAttribute>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong _valueAsU64;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _valueAsObject;

        public DwarfAttributeKindEx Kind { get; set; }

        public bool ValueAsBoolean
        {
            get => _valueAsU64 != 0;
            set => _valueAsU64 = value ? 1U : 0;
        }

        public int ValueAsI32
        {
            get => (int)_valueAsU64;
            set => _valueAsU64 = (ulong)(long)value;
        }

        public uint ValueAsU32
        {
            get => (uint)_valueAsU64;
            set => _valueAsU64 = value;
        }

        public long ValueAsI64
        {
            get => (long)_valueAsU64;
            set => _valueAsU64 = (ulong)value;
        }

        public ulong ValueAsU64
        {
            get => _valueAsU64;
            set => _valueAsU64 = value;
        }

        /// <summary>
        /// Gets or sets the encoding used for this attribute. Default is <c>null</c> meaning that the encoding
        /// is detected automatically. Some attributes may require to explicitly set this encoding to disambiguate
        /// between different encoding form (e.g boolean => <see cref="DwarfAttributeEncoding.Flag"/> instead of <see cref="DwarfAttributeEncoding.Constant"/>)
        /// </summary>
        public DwarfAttributeEncoding? Encoding { get; set; }

        public DwarfAttributeFormEx Form { get; internal set; }

        public object ValueAsObject
        {
            get => _valueAsObject;
            set
            {
                if (_valueAsObject is DwarfExpression oldExpression)
                {
                    oldExpression.Parent = null;
                }
                _valueAsObject = value;

                if (value is DwarfExpression newExpression)
                {
                    if (newExpression.Parent != null) throw new InvalidOperationException($"Cannot set the {newExpression.GetType()} as it already belongs to another {newExpression.Parent.GetType()} instance");
                    newExpression.Parent = this;
                }
            }
        }
        
        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            // Check DwarfDIE reference
            if (ValueAsObject is DwarfDIE attrDIE)
            {
                var thisSection = this.GetParentSection();
                var attrSection = attrDIE.GetParentSection();

                if (thisSection != attrSection)
                {
                    diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidParentForDIE, $"Invalid parent for the DIE {attrDIE} referenced by the attribute {this}. It must be within the same parent {thisSection.GetType()}.");
                }
            }
            else if (ValueAsObject is DwarfExpression expr)
            {
                expr.Verify(diagnostics);
            }
            else if (ValueAsObject is DwarfLocationList locationList)
            {
                var thisSection = this.GetParentFile();
                var locationListSection = locationList.GetParentFile();

                if (thisSection != locationListSection)
                {
                    diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidParentForLocationList, $"Invalid parent for the LocationList {locationList} referenced by the attribute {this}. It must be within the same parent {thisSection.GetType()}.");
                }
            }
        }
        
        public int CompareTo(DwarfAttribute other)
        {
            return ((uint)Kind).CompareTo((uint)other.Kind);
        }

        public override string ToString()
        {
            if (ValueAsObject != null)
            {
                return ValueAsU64 != 0 ? $"{nameof(Kind)}: {Kind}, Value: {ValueAsObject} Offset: {ValueAsU64}" : $"{nameof(Kind)}: {Kind}, Value: {ValueAsObject}";
            }
            else
            {
                return $"{nameof(Kind)}: {Kind}, Value: {ValueAsU64}";
            }
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            var addressSize = layoutContext.CurrentUnit.AddressSize;
            var is64BitEncoding = layoutContext.CurrentUnit.Is64BitEncoding;

            var endOffset = Offset;
            switch (Form.Value)
            {
                case DwarfAttributeForm.Addr:
                    endOffset += DwarfHelper.SizeOfUInt(addressSize); // WriteUInt(ValueAsU64);
                    break;
                case DwarfAttributeForm.Data1:
                    endOffset += 1; // WriteU8((byte)ValueAsU64);
                    break;
                case DwarfAttributeForm.Data2:
                    endOffset += 2; // WriteU16((ushort)ValueAsU64);
                    break;
                case DwarfAttributeForm.Data4:
                    endOffset += 4; // WriteU32((uint)ValueAsU64);
                    break;
                case DwarfAttributeForm.Data8:
                    endOffset += 8; // WriteU64(ValueAsU64);
                    break;
                case DwarfAttributeForm.String:
                    endOffset += DwarfHelper.SizeOfStringUTF8NullTerminated((string)ValueAsObject);
                    break;
                case DwarfAttributeForm.Block:
                    {
                        var stream = (Stream)ValueAsObject;
                        endOffset += DwarfHelper.SizeOfULEB128((ulong)stream.Length);
                        endOffset += (ulong)stream.Length;
                        break;
                    }
                case DwarfAttributeForm.Block1:
                    {
                        var stream = (Stream)ValueAsObject;
                        endOffset += 1;
                        endOffset += (ulong)stream.Length;
                        break;
                    }
                case DwarfAttributeForm.Block2:
                    {
                        var stream = (Stream)ValueAsObject;
                        endOffset += 2;
                        endOffset += (ulong)stream.Length;
                        break;
                    }
                case DwarfAttributeForm.Block4:
                    {
                        var stream = (Stream)ValueAsObject;
                        endOffset += 4;
                        endOffset += (ulong)stream.Length;
                        break;
                    }
                case DwarfAttributeForm.Flag:
                    endOffset += 1; // WriteU8((byte)(ValueAsU64 != 0 ? 1 : 0));
                    break;
                case DwarfAttributeForm.Sdata:
                    endOffset += DwarfHelper.SizeOfILEB128(ValueAsI64); // WriteILEB128(ValueAsI64);
                    break;
                case DwarfAttributeForm.Strp:
                    endOffset += DwarfHelper.SizeOfUInt(is64BitEncoding); // WriteUIntFromEncoding(offset);
                    break;
                case DwarfAttributeForm.Udata:
                    endOffset += DwarfHelper.SizeOfULEB128(ValueAsU64); // ReadULEB128();
                    break;
                case DwarfAttributeForm.RefAddr:
                    endOffset += DwarfHelper.SizeOfUInt(is64BitEncoding); // WriteUIntFromEncoding(dieRef.Offset);
                    break;
                case DwarfAttributeForm.Ref1:
                    endOffset += 1; // WriteU8((byte)(dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.Ref2:
                    endOffset += 2; // WriteU16((ushort)(dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.Ref4:
                    endOffset += 4; // WriteU32((uint)(dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.Ref8:
                    endOffset += 8; // WriteU64((dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.RefUdata:
                    {
                        var dieRef = (DwarfDIE)ValueAsObject;
                        endOffset += DwarfHelper.SizeOfULEB128(dieRef.Offset - layoutContext.CurrentUnit.Offset); // WriteULEB128((dieRef.Offset - _currentUnit.Offset));
                        break;
                    }

                //case DwarfAttributeForm.indirect:
                //{
                //    attributeForm = ReadLEB128As<DwarfAttributeForm>();
                //    goto indirect;
                //}

                // addptr
                // lineptr
                // loclist
                // loclistptr
                // macptr
                // rnglist
                // rngrlistptr
                // stroffsetsptr
                case DwarfAttributeForm.SecOffset:
                    endOffset += DwarfHelper.SizeOfUInt(is64BitEncoding);
                    break;

                case DwarfAttributeForm.Exprloc:
                    var expr = (DwarfExpression)ValueAsObject;
                    expr.Offset = endOffset;
                    expr.UpdateLayoutInternal(layoutContext);
                    endOffset += expr.Size;
                    break;

                case DwarfAttributeForm.FlagPresent:
                    break;

                case DwarfAttributeForm.RefSig8:
                    endOffset += 8; // WriteU64(ValueAsU64);
                    break;

                case DwarfAttributeForm.Strx: throw new NotSupportedException("DW_FORM_strx - DWARF5");
                case DwarfAttributeForm.Addrx: throw new NotSupportedException("DW_FORM_addrx - DWARF5");
                case DwarfAttributeForm.RefSup4: throw new NotSupportedException("DW_FORM_ref_sup4 - DWARF5");
                case DwarfAttributeForm.StrpSup: throw new NotSupportedException("DW_FORM_strp_sup - DWARF5");
                case DwarfAttributeForm.Data16: throw new NotSupportedException("DW_FORM_data16 - DWARF5");
                case DwarfAttributeForm.LineStrp: throw new NotSupportedException("DW_FORM_line_strp - DWARF5");
                case DwarfAttributeForm.ImplicitConst: throw new NotSupportedException("DW_FORM_implicit_const - DWARF5");
                case DwarfAttributeForm.Loclistx: throw new NotSupportedException("DW_FORM_loclistx - DWARF5");
                case DwarfAttributeForm.Rnglistx: throw new NotSupportedException("DW_FORM_rnglistx - DWARF5");
                case DwarfAttributeForm.RefSup8: throw new NotSupportedException("DW_FORM_ref_sup8 - DWARF5");
                case DwarfAttributeForm.Strx1: throw new NotSupportedException("DW_FORM_strx1 - DWARF5");
                case DwarfAttributeForm.Strx2: throw new NotSupportedException("DW_FORM_strx2 - DWARF5");
                case DwarfAttributeForm.Strx3: throw new NotSupportedException("DW_FORM_strx3 - DWARF5");
                case DwarfAttributeForm.Strx4: throw new NotSupportedException("DW_FORM_strx4 - DWARF5");
                case DwarfAttributeForm.Addrx1: throw new NotSupportedException("DW_FORM_addrx1 - DWARF5");
                case DwarfAttributeForm.Addrx2: throw new NotSupportedException("DW_FORM_addrx2 - DWARF5");
                case DwarfAttributeForm.Addrx3: throw new NotSupportedException("DW_FORM_addrx3 - DWARF5");
                case DwarfAttributeForm.Addrx4: throw new NotSupportedException("DW_FORM_addrx4 - DWARF5");
                case DwarfAttributeForm.GNUAddrIndex: throw new NotSupportedException("DW_FORM_GNU_addr_index - GNU extension in debug_info.dwo.");
                case DwarfAttributeForm.GNUStrIndex: throw new NotSupportedException("DW_FORM_GNU_str_index - GNU extension, somewhat like DW_FORM_strp");
                case DwarfAttributeForm.GNURefAlt: throw new NotSupportedException("DW_FORM_GNU_ref_alt - GNU extension. Offset in .debug_info.");
                case DwarfAttributeForm.GNUStrpAlt: throw new NotSupportedException("DW_FORM_GNU_strp_alt - GNU extension. Offset in .debug_str of another object file.");
                default:
                    throw new NotSupportedException($"Unknown {nameof(DwarfAttributeForm)}: {Form}");
            }

            Size = endOffset - Offset;
        }

        protected override void Read(DwarfReader reader)
        {
            var descriptor = reader.CurrentAttributeDescriptor;

            Kind = descriptor.Kind;
            Form = descriptor.Form;

            ReadAttributeFormRawValue(reader);

            Size = reader.Offset - Offset;
            
            ResolveAttributeValue(reader);
        }

        private void ResolveAttributeValue(DwarfReader reader)
        {
            switch (Kind.Value)
            {
                case DwarfAttributeKind.DeclFile:
                {
                    var currentLineProgramTable = reader.CurrentLineProgramTable;
                    if (currentLineProgramTable == null)
                    {
                        // Log and error
                    }
                    else
                    {
                        var file = currentLineProgramTable.FileNames[ValueAsI32 - 1];
                        ValueAsU64 = 0;
                        ValueAsObject = file;
                    }
                    break;
                }

                case DwarfAttributeKind.StmtList:
                {
                    if (ValueAsU64 == 0) return;

                    if (reader.File.LineSection != null)
                    {
                        if (reader.OffsetToLineProgramTable.TryGetValue(ValueAsU64, out var lineProgramTable))
                        {
                            ValueAsU64 = 0;
                            ValueAsObject = lineProgramTable;
                            reader.PushLineProgramTable(lineProgramTable);
                        }
                        else
                        {
                            // Log and error
                        }
                    }
                    else
                    {

                        // Log an error
                    }

                    break;

                }

                case DwarfAttributeKind.Location:
                {
                    if (Form == DwarfAttributeFormEx.SecOffset)
                    {
                        if (reader.OffsetToLocationList.TryGetValue(ValueAsU64, out var locationList))
                        {
                            ValueAsU64 = 0;
                            ValueAsObject = locationList;
                        }
                        else
                        {
                            // Log and error
                        }
                    }
                    break;
                }
            }
        }


        private void ReadAttributeFormRawValue(DwarfReader reader)
        {
            var attributeForm = Form;

            indirect:
            switch (attributeForm.Value)
            {
                case DwarfAttributeForm.Addr:
                {
                    ValueAsU64 = reader.ReadUInt();
                    break;
                }

                case DwarfAttributeForm.Data1:
                {
                    ValueAsU64 = reader.ReadU8();
                    break;
                }
                case DwarfAttributeForm.Data2:
                {
                    ValueAsU64 = reader.ReadU16();
                    break;
                }
                case DwarfAttributeForm.Data4:
                {
                    ValueAsU64 = reader.ReadU32();
                    break;
                }
                case DwarfAttributeForm.Data8:
                {
                    ValueAsU64 = reader.ReadU64();
                    break;
                }

                case DwarfAttributeForm.String:
                {
                    ValueAsObject = reader.ReadStringUTF8NullTerminated();
                    break;
                }

                case DwarfAttributeForm.Block:
                {
                    var length = reader.ReadULEB128();
                    ValueAsObject = reader.ReadAsStream(length);
                    break;
                }
                case DwarfAttributeForm.Block1:
                {
                    var length = reader.ReadU8();
                    ValueAsObject = reader.ReadAsStream(length);
                    break;
                }
                case DwarfAttributeForm.Block2:
                {
                    var length = reader.ReadU16();
                    ValueAsObject = reader.ReadAsStream(length);
                    break;
                }
                case DwarfAttributeForm.Block4:
                {
                    var length = reader.ReadU32();
                    ValueAsObject = reader.ReadAsStream(length);
                    break;
                }

                case DwarfAttributeForm.Flag:
                {
                    ValueAsBoolean = reader.ReadU8() != 0;
                    break;
                }
                case DwarfAttributeForm.Sdata:
                {
                    ValueAsI64 = reader.ReadILEB128();
                    break;
                }
                case DwarfAttributeForm.Strp:
                {
                    var offset = reader.ReadUIntFromEncoding();
                    ValueAsObject = reader.File.StringTable.GetStringFromOffset(offset);
                    break;
                }
                case DwarfAttributeForm.Udata:
                {
                    ValueAsU64 = reader.ReadULEB128();
                    break;
                }
                case DwarfAttributeForm.RefAddr:
                {
                    ValueAsU64 = reader.ReadUIntFromEncoding();
                    reader.ResolveAttributeReferenceWithinSection(AttributeToDIERef(this), false);
                    break;
                }
                case DwarfAttributeForm.Ref1:
                {
                    ValueAsU64 = reader.ReadU8();
                    reader.ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(this), false);
                    break;
                }
                case DwarfAttributeForm.Ref2:
                {
                    ValueAsU64 = reader.ReadU16();
                    reader.ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(this), false);
                    break;
                }
                case DwarfAttributeForm.Ref4:
                {
                    ValueAsU64 = reader.ReadU32();
                    reader.ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(this), false);
                    break;
                }
                case DwarfAttributeForm.Ref8:
                {
                    ValueAsU64 = reader.ReadU64();
                    reader.ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(this), false);
                    break;
                }
                case DwarfAttributeForm.RefUdata:
                {
                    ValueAsU64 = reader.ReadULEB128();
                    reader.ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(this), false);
                    break;
                }
                case DwarfAttributeForm.Indirect:
                {
                    attributeForm = new DwarfAttributeFormEx(reader.ReadULEB128AsU32());
                    goto indirect;
                }

                // addptr
                // lineptr
                // loclist
                // loclistptr
                // macptr
                // rnglist
                // rngrlistptr
                // stroffsetsptr
                case DwarfAttributeForm.SecOffset:
                {
                    ValueAsU64 = reader.ReadUIntFromEncoding();
                    //Console.WriteLine($"attribute {Key} offset: {ValueAsU64}");
                    break;
                }

                case DwarfAttributeForm.Exprloc:
                {
                    var expression = new DwarfExpression();
                    expression.ReadInternal(reader);
                    ValueAsObject = expression;
                    break;
                }

                case DwarfAttributeForm.FlagPresent:
                {
                    ValueAsBoolean = true;
                    break;
                }

                case DwarfAttributeForm.RefSig8:
                {
                    var offset = reader.ReadU64();
                    ValueAsU64 = offset;
                    break;
                }

                case DwarfAttributeForm.Strx: throw new NotSupportedException("DW_FORM_strx - DWARF5");
                case DwarfAttributeForm.Addrx: throw new NotSupportedException("DW_FORM_addrx - DWARF5");
                case DwarfAttributeForm.RefSup4: throw new NotSupportedException("DW_FORM_ref_sup4 - DWARF5");
                case DwarfAttributeForm.StrpSup: throw new NotSupportedException("DW_FORM_strp_sup - DWARF5");
                case DwarfAttributeForm.Data16: throw new NotSupportedException("DW_FORM_data16 - DWARF5");
                case DwarfAttributeForm.LineStrp: throw new NotSupportedException("DW_FORM_line_strp - DWARF5");
                case DwarfAttributeForm.ImplicitConst: throw new NotSupportedException("DW_FORM_implicit_const - DWARF5");
                case DwarfAttributeForm.Loclistx: throw new NotSupportedException("DW_FORM_loclistx - DWARF5");
                case DwarfAttributeForm.Rnglistx: throw new NotSupportedException("DW_FORM_rnglistx - DWARF5");
                case DwarfAttributeForm.RefSup8: throw new NotSupportedException("DW_FORM_ref_sup8 - DWARF5");
                case DwarfAttributeForm.Strx1: throw new NotSupportedException("DW_FORM_strx1 - DWARF5");
                case DwarfAttributeForm.Strx2: throw new NotSupportedException("DW_FORM_strx2 - DWARF5");
                case DwarfAttributeForm.Strx3: throw new NotSupportedException("DW_FORM_strx3 - DWARF5");
                case DwarfAttributeForm.Strx4: throw new NotSupportedException("DW_FORM_strx4 - DWARF5");
                case DwarfAttributeForm.Addrx1: throw new NotSupportedException("DW_FORM_addrx1 - DWARF5");
                case DwarfAttributeForm.Addrx2: throw new NotSupportedException("DW_FORM_addrx2 - DWARF5");
                case DwarfAttributeForm.Addrx3: throw new NotSupportedException("DW_FORM_addrx3 - DWARF5");
                case DwarfAttributeForm.Addrx4: throw new NotSupportedException("DW_FORM_addrx4 - DWARF5");
                case DwarfAttributeForm.GNUAddrIndex: throw new NotSupportedException("DW_FORM_GNU_addr_index - GNU extension in debug_info.dwo.");
                case DwarfAttributeForm.GNUStrIndex: throw new NotSupportedException("DW_FORM_GNU_str_index - GNU extension, somewhat like DW_FORM_strp");
                case DwarfAttributeForm.GNURefAlt: throw new NotSupportedException("DW_FORM_GNU_ref_alt - GNU extension. Offset in .debug_info.");
                case DwarfAttributeForm.GNUStrpAlt: throw new NotSupportedException("DW_FORM_GNU_strp_alt - GNU extension. Offset in .debug_str of another object file.");
                default:
                    throw new NotSupportedException($"Unknown {nameof(DwarfAttributeForm)}: {attributeForm.Value}");
            }
        }

        internal void UpdateAttributeForm(DwarfLayoutContext context)
        {
            Form = ComputeAttributeForm(context);
        }
        
        private DwarfAttributeForm ComputeAttributeForm(DwarfLayoutContext context)
        {
            var key = Kind;
            var encoding = DwarfHelper.GetAttributeEncoding(key);

            if (encoding == DwarfAttributeEncoding.None)
            {
                switch (Kind.Value)
                {
                    case DwarfAttributeKind.GNUAllCallSites:
                    case DwarfAttributeKind.GNUAllTailCallSites:
                        encoding = DwarfAttributeEncoding.Flag;
                        break;
                    case DwarfAttributeKind.GNUCallSiteTarget:
                    case DwarfAttributeKind.GNUCallSiteValue:
                        encoding = DwarfAttributeEncoding.ExpressionLocation | DwarfAttributeEncoding.LocationList;
                        break;

                    default:
                        // TODO: Add pluggable support for requesting attribute encoding here
                        throw new InvalidOperationException($"Unsupported attribute {this} with unknown encoding");
                }
            }

            // If the attribute has a requested encoding
            if (Encoding.HasValue)
            {
                var requestedEncoding = Encoding.Value;
                if ((encoding & requestedEncoding) == 0)
                {
                    throw new InvalidOperationException($"Requested encoding {requestedEncoding} for {this} doesn't match supported encoding {encoding} for this attribute");
                }

                // Replace encoding with requested encoding
                encoding = requestedEncoding;
            }

            // Do we still have a complex encoding?
            if ((((uint)encoding) & ((uint)encoding - 1)) != 0U)
            {
                // if we have, try to detect which one we should select
                if (ValueAsObject is DwarfDIE)
                {
                    if ((encoding & DwarfAttributeEncoding.Reference) == 0)
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The DIE value of attribute {this} from DIE {this.Parent} is not valid for supported attribute encoding {encoding}. Expecting Reference.");
                    }

                    encoding = DwarfAttributeEncoding.Reference;
                }
                else if (this.ValueAsObject is Stream)
                {
                    if ((encoding & DwarfAttributeEncoding.Block) == 0)
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The Stream value of attribute {this} from DIE {this.Parent} is not valid for supported attribute encoding {encoding}. Expecting Block.");
                    }

                    encoding = DwarfAttributeEncoding.Block;
                }
                else if (this.ValueAsObject is string)
                {
                    if ((encoding & DwarfAttributeEncoding.String) == 0)
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The string value of attribute {this} from DIE {this.Parent} is not valid for supported attribute encoding {encoding}. Expecting String.");
                    }

                    encoding = DwarfAttributeEncoding.String;
                }
                else if (this.ValueAsObject is DwarfExpression)
                {
                    if ((encoding & DwarfAttributeEncoding.ExpressionLocation) == 0)
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The expression value of attribute {this} from DIE {this.Parent} is not valid for supported attribute encoding {encoding}. Expecting ExpressionLocation.");
                    }

                    encoding = DwarfAttributeEncoding.ExpressionLocation;
                }
                else if (this.ValueAsObject is DwarfLocationList)
                {
                    if ((encoding & DwarfAttributeEncoding.LocationList) == 0)
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The expression value of attribute {this} from DIE {this.Parent} is not valid for supported attribute encoding {encoding}. Expecting LocationList.");
                    }

                    encoding = DwarfAttributeEncoding.LocationList;
                }
                else if ((encoding & DwarfAttributeEncoding.Address) != 0)
                {
                    if (this.ValueAsObject != null)
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The {this.ValueAsObject.GetType()} value of attribute {this} from DIE {this.Parent} is not valid for supported attribute encoding {encoding}. Expecting Address.");
                    }

                    // If not specified explicitly, We consider HighPC as a constant (delta from LowPC)
                    if (this.Kind  == DwarfAttributeKindEx.HighPC)
                    {
                        encoding = DwarfAttributeEncoding.Constant;
                    }
                    else
                    {
                        encoding = DwarfAttributeEncoding.Address;
                    }
                }
                else if ((encoding & DwarfAttributeEncoding.Constant) != 0)
                {
                    if (this.ValueAsObject != null)
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The {this.ValueAsObject.GetType()} value of attribute {this} from DIE {this.Parent} is not valid for supported attribute encoding {encoding}. Expecting Constant.");
                    }

                    encoding = DwarfAttributeEncoding.Constant;
                }
            }

            switch (encoding)
            {
                case DwarfAttributeEncoding.Address:
                    return DwarfAttributeForm.Addr;

                case DwarfAttributeEncoding.Block:
                    VerifyAttributeValueNotNull(context);

                    if (!(this.ValueAsObject is Stream))
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {this} from DIE {this.Parent} must be a System.IO.Stream");
                    }

                    return DwarfAttributeForm.Block;

                case DwarfAttributeEncoding.Constant:

                    if (this.ValueAsU64 <= byte.MaxValue)
                    {
                        return DwarfAttributeForm.Data1;
                    }

                    if (this.ValueAsU64 <= ushort.MaxValue)
                    {
                        return DwarfAttributeForm.Data2;
                    }

                    if (this.ValueAsU64 <= uint.MaxValue)
                    {
                        return DwarfAttributeForm.Data4;
                    }

                    return DwarfAttributeForm.Data8;

                case DwarfAttributeEncoding.ExpressionLocation:
                    VerifyAttributeValueNotNull(context);

                    if (!(this.ValueAsObject is DwarfExpression))
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {this} from DIE {this.Parent} must be a {nameof(DwarfExpression)}");
                    }

                    return DwarfAttributeForm.Exprloc;

                case DwarfAttributeEncoding.Flag:
                    return this.ValueAsBoolean ? DwarfAttributeForm.FlagPresent : DwarfAttributeForm.Flag;

                case DwarfAttributeEncoding.LinePointer:
                    bool canHaveNull = this.Kind.Value == DwarfAttributeKind.StmtList;
                    if (!canHaveNull)
                    {
                        VerifyAttributeValueNotNull(context);

                        if (!(this.ValueAsObject is DwarfLine))
                        {
                            context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {this} from DIE {this.Parent} must be a {nameof(DwarfLine)}");
                        }
                    }

                    return DwarfAttributeForm.SecOffset;


                case DwarfAttributeEncoding.Reference:
                    VerifyAttributeValueNotNull(context);

                    if (this.ValueAsObject is DwarfDIE die)
                    {
                        var dieParentUnit = die.GetParentUnit();
                        // If we are not from the same unit 
                        if (dieParentUnit != context.CurrentUnit)
                        {
                            return DwarfAttributeForm.RefAddr;
                        }
                    }
                    else
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {this} from DIE {this.Parent} must be a {nameof(DwarfDIE)}");
                    }

                    return context.Config.DefaultAttributeFormForReference;

                case DwarfAttributeEncoding.String:
                    VerifyAttributeValueNotNull(context);

                    if (this.ValueAsObject is string str)
                    {
                        // Create string offset
                        if (context.File.StringTable.Contains(str))
                        {
                            return DwarfAttributeForm.Strp;
                        }
                    }
                    else
                    {
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {this} from DIE {this.Parent} must be a string.");
                    }

                    return DwarfAttributeForm.String;

                case DwarfAttributeEncoding.RangeList:
                case DwarfAttributeEncoding.LocationList:
                case DwarfAttributeEncoding.Indirect:
                case DwarfAttributeEncoding.AddressPointer:
                case DwarfAttributeEncoding.LocationListsPointer:
                case DwarfAttributeEncoding.RangeListsPointer:
                case DwarfAttributeEncoding.StringOffsetPointer:
                case DwarfAttributeEncoding.LocationListPointer:
                case DwarfAttributeEncoding.MacroPointer:
                case DwarfAttributeEncoding.RangeListPointer:
                    return DwarfAttributeForm.SecOffset;
            }

            context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The encoding {encoding} of attribute {this} from DIE {this.Parent} is not supported.");
            return DwarfAttributeForm.Data8;
        }

        private void VerifyAttributeValueNotNull(DwarfLayoutContext context)
        {
            if (ValueAsObject == null)
            {
                context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The object value of attribute {this} from DIE {this.Parent} cannot be null");
            }
        }

        protected override void Write(DwarfWriter writer)
        {
            var startAttributeOffset = Offset;
            Debug.Assert(Offset == startAttributeOffset);

            switch (Form.Value)
            {
                case DwarfAttributeForm.Addr:
                {
                    writer.WriteAddress(DwarfRelocationTarget.Code, ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.Data1:
                {
                    writer.WriteU8((byte) ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.Data2:
                {
                    writer.WriteU16((ushort) ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.Data4:
                {
                    writer.WriteU32((uint) ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.Data8:
                {
                    writer.WriteU64(ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.String:
                {
                    writer.WriteStringUTF8NullTerminated((string) ValueAsObject);
                    break;
                }
                case DwarfAttributeForm.Block:
                {
                    var stream = (Stream) ValueAsObject;
                    writer.WriteULEB128((ulong) stream.Length);
                    writer.Write(stream);
                    break;
                }
                case DwarfAttributeForm.Block1:
                {
                    var stream = (Stream) ValueAsObject;
                    writer.WriteU8((byte) stream.Length);
                    writer.Write(stream);
                    break;
                }
                case DwarfAttributeForm.Block2:
                {
                    var stream = (Stream) ValueAsObject;
                    writer.WriteU16((ushort) stream.Length);
                    writer.Write(stream);
                    break;
                }
                case DwarfAttributeForm.Block4:
                {
                    var stream = (Stream) ValueAsObject;
                    writer.WriteU32((uint) stream.Length);
                    writer.Write(stream);
                    break;
                }
                case DwarfAttributeForm.Flag:
                {
                    writer.WriteU8((byte) (ValueAsU64 != 0 ? 1 : 0));
                    break;
                }
                case DwarfAttributeForm.Sdata:
                {
                    writer.WriteILEB128(ValueAsI64);
                    break;
                }
                case DwarfAttributeForm.Strp:
                {
                    var offset = writer.File.StringTable.GetOrCreateString((string) ValueAsObject);
                    if (writer.EnableRelocation)
                    {
                        writer.RecordRelocation(DwarfRelocationTarget.DebugString, writer.SizeOfUIntEncoding(), offset);
                        offset = 0;
                    }
                    writer.WriteUIntFromEncoding(offset);
                    break;
                }
                case DwarfAttributeForm.Udata:
                {
                    writer.WriteULEB128(ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.RefAddr:
                {
                    var dieRef = (DwarfDIE) ValueAsObject;
                    writer.WriteUIntFromEncoding(dieRef.Offset);
                    break;
                }
                case DwarfAttributeForm.Ref1:
                {
                    var dieRef = (DwarfDIE) ValueAsObject;
                    writer.WriteU8((byte) (dieRef.Offset - writer.CurrentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.Ref2:
                {
                    var dieRef = (DwarfDIE) ValueAsObject;
                    writer.WriteU16((ushort) (dieRef.Offset - writer.CurrentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.Ref4:
                {
                    var dieRef = (DwarfDIE) ValueAsObject;
                    writer.WriteU32((uint) (dieRef.Offset - writer.CurrentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.Ref8:
                {
                    var dieRef = (DwarfDIE) ValueAsObject;
                    writer.WriteU64((dieRef.Offset - writer.CurrentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.RefUdata:
                {
                    var dieRef = (DwarfDIE) ValueAsObject;
                    writer.WriteULEB128((dieRef.Offset - writer.CurrentUnit.Offset));
                    break;
                }

                //case DwarfAttributeForm.indirect:
                //{
                //    attributeForm = reader.ReadLEB128As<DwarfAttributeForm>();
                //    goto indirect;
                //}

                // addptr
                // lineptr
                // loclist
                // loclistptr
                // macptr
                // rnglist
                // rngrlistptr
                // stroffsetsptr
                case DwarfAttributeForm.SecOffset:
                {
                    if (ValueAsObject != null)
                    {
                        writer.WriteUIntFromEncoding(((DwarfObject) ValueAsObject).Offset);
                    }
                    else
                    {
                        writer.WriteUIntFromEncoding(ValueAsU64);
                    }
                    break;
                }

                case DwarfAttributeForm.Exprloc:
                    ((DwarfExpression) ValueAsObject).WriteInternal(writer);
                    break;

                case DwarfAttributeForm.FlagPresent:
                    Debug.Assert(ValueAsBoolean);
                    break;

                case DwarfAttributeForm.RefSig8:
                    writer.WriteU64(ValueAsU64);
                    break;

                case DwarfAttributeForm.Strx: throw new NotSupportedException("DW_FORM_strx - DWARF5");
                case DwarfAttributeForm.Addrx: throw new NotSupportedException("DW_FORM_addrx - DWARF5");
                case DwarfAttributeForm.RefSup4: throw new NotSupportedException("DW_FORM_ref_sup4 - DWARF5");
                case DwarfAttributeForm.StrpSup: throw new NotSupportedException("DW_FORM_strp_sup - DWARF5");
                case DwarfAttributeForm.Data16: throw new NotSupportedException("DW_FORM_data16 - DWARF5");
                case DwarfAttributeForm.LineStrp: throw new NotSupportedException("DW_FORM_line_strp - DWARF5");
                case DwarfAttributeForm.ImplicitConst: throw new NotSupportedException("DW_FORM_implicit_const - DWARF5");
                case DwarfAttributeForm.Loclistx: throw new NotSupportedException("DW_FORM_loclistx - DWARF5");
                case DwarfAttributeForm.Rnglistx: throw new NotSupportedException("DW_FORM_rnglistx - DWARF5");
                case DwarfAttributeForm.RefSup8: throw new NotSupportedException("DW_FORM_ref_sup8 - DWARF5");
                case DwarfAttributeForm.Strx1: throw new NotSupportedException("DW_FORM_strx1 - DWARF5");
                case DwarfAttributeForm.Strx2: throw new NotSupportedException("DW_FORM_strx2 - DWARF5");
                case DwarfAttributeForm.Strx3: throw new NotSupportedException("DW_FORM_strx3 - DWARF5");
                case DwarfAttributeForm.Strx4: throw new NotSupportedException("DW_FORM_strx4 - DWARF5");
                case DwarfAttributeForm.Addrx1: throw new NotSupportedException("DW_FORM_addrx1 - DWARF5");
                case DwarfAttributeForm.Addrx2: throw new NotSupportedException("DW_FORM_addrx2 - DWARF5");
                case DwarfAttributeForm.Addrx3: throw new NotSupportedException("DW_FORM_addrx3 - DWARF5");
                case DwarfAttributeForm.Addrx4: throw new NotSupportedException("DW_FORM_addrx4 - DWARF5");
                case DwarfAttributeForm.GNUAddrIndex: throw new NotSupportedException("DW_FORM_GNU_addr_index - GNU extension in debug_info.dwo.");
                case DwarfAttributeForm.GNUStrIndex: throw new NotSupportedException("DW_FORM_GNU_str_index - GNU extension, somewhat like DW_FORM_strp");
                case DwarfAttributeForm.GNURefAlt: throw new NotSupportedException("DW_FORM_GNU_ref_alt - GNU extension. Offset in .debug_info.");
                case DwarfAttributeForm.GNUStrpAlt: throw new NotSupportedException("DW_FORM_GNU_strp_alt - GNU extension. Offset in .debug_str of another object file.");
                default:
                    throw new NotSupportedException($"Unknown {nameof(DwarfAttributeForm)}: {Form}");
            }

            Debug.Assert(writer.Offset - startAttributeOffset == Size);
        }

        private static readonly DwarfReader.DwarfDIEReferenceResolver DwarfAttributeDIEReferenceResolverInstance = DwarfAttributeDIEReferenceResolver;

        private static DwarfReader.DwarfDIEReference AttributeToDIERef(DwarfAttribute attr)
        {
            return new DwarfReader.DwarfDIEReference(attr.ValueAsU64, attr, DwarfAttributeDIEReferenceResolverInstance);
        }

        private static void DwarfAttributeDIEReferenceResolver(ref DwarfReader.DwarfDIEReference dieRef)
        {
            var attr = (DwarfAttribute)dieRef.DwarfObject;
            attr.ValueAsU64 = 0;
            attr.ValueAsObject = dieRef.Resolved;
        }
    }
}