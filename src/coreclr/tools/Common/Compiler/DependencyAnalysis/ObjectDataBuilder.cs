// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    // There is small set of ObjectDataBuilder in at src/installer/managed/Microsoft.NET.HostModel/ObjectDataBuilder.cs
    // only for ResourceData.WriteResources
    public struct ObjectDataBuilder
#if !READYTORUN
        : Internal.Runtime.ITargetBinaryWriter
#endif
    {
        public ObjectDataBuilder(NodeFactory factory, bool relocsOnly) : this(factory.Target, relocsOnly)
        {
        }

        public ObjectDataBuilder(TargetDetails target, bool relocsOnly)
        {
            _target = target;
            _data = default(ArrayBuilder<byte>);
            _relocs = default(ArrayBuilder<Relocation>);
            Alignment = 1;
            _definedSymbols = default(ArrayBuilder<ISymbolDefinitionNode>);
#if DEBUG
            _numReservations = 0;
            _checkAllSymbolDependenciesMustBeMarked = !relocsOnly;
#endif
        }

        private TargetDetails _target;
        private ArrayBuilder<Relocation> _relocs;
        private ArrayBuilder<byte> _data;
        public int Alignment { get; private set; }
        private ArrayBuilder<ISymbolDefinitionNode> _definedSymbols;

#if DEBUG
        private int _numReservations;
        private bool _checkAllSymbolDependenciesMustBeMarked;
#endif

        public int CountBytes
        {
            get
            {
                return _data.Count;
            }
        }

        public int TargetPointerSize
        {
            get
            {
                return _target.PointerSize;
            }
        }

        /// <summary>
        /// Raise the alignment requirement of this object to <paramref name="align"/>. This has no effect
        /// if the alignment requirement is already larger than <paramref name="align"/>.
        /// </summary>
        public void RequireInitialAlignment(int align)
        {
            Alignment = Math.Max(align, Alignment);
        }

        /// <summary>
        /// Raise the alignment requirement of this object to the target pointer size. This has no effect
        /// if the alignment requirement is already larger than a pointer size.
        /// </summary>
        public void RequireInitialPointerAlignment()
        {
            RequireInitialAlignment(_target.PointerSize);
        }

        public void EmitByte(byte emit)
        {
            _data.Add(emit);
        }

        public void EmitShort(short emit)
        {
            EmitByte((byte)(emit & 0xFF));
            EmitByte((byte)((emit >> 8) & 0xFF));
        }

        public void EmitUShort(ushort emit)
        {
            EmitByte((byte)(emit & 0xFF));
            EmitByte((byte)((emit >> 8) & 0xFF));
        }

        public void EmitInt(int emit)
        {
            EmitByte((byte)(emit & 0xFF));
            EmitByte((byte)((emit >> 8) & 0xFF));
            EmitByte((byte)((emit >> 16) & 0xFF));
            EmitByte((byte)((emit >> 24) & 0xFF));
        }

        public void EmitUInt(uint emit)
        {
            EmitByte((byte)(emit & 0xFF));
            EmitByte((byte)((emit >> 8) & 0xFF));
            EmitByte((byte)((emit >> 16) & 0xFF));
            EmitByte((byte)((emit >> 24) & 0xFF));
        }

        public void EmitLong(long emit)
        {
            EmitByte((byte)(emit & 0xFF));
            EmitByte((byte)((emit >> 8) & 0xFF));
            EmitByte((byte)((emit >> 16) & 0xFF));
            EmitByte((byte)((emit >> 24) & 0xFF));
            EmitByte((byte)((emit >> 32) & 0xFF));
            EmitByte((byte)((emit >> 40) & 0xFF));
            EmitByte((byte)((emit >> 48) & 0xFF));
            EmitByte((byte)((emit >> 56) & 0xFF));
        }

        public void EmitNaturalInt(int emit)
        {
            if (_target.PointerSize == 8)
            {
                EmitLong(emit);
            }
            else
            {
                Debug.Assert(_target.PointerSize == 4);
                EmitInt(emit);
            }
        }

        public void EmitHalfNaturalInt(short emit)
        {
            if (_target.PointerSize == 8)
            {
                EmitInt(emit);
            }
            else
            {
                Debug.Assert(_target.PointerSize == 4);
                EmitShort(emit);
            }
        }

        public void EmitCompressedUInt(uint emit)
        {
            if (emit < 128)
            {
                EmitByte((byte)(emit * 2 + 0));
            }
            else if (emit < 128 * 128)
            {
                EmitByte((byte)(emit * 4 + 1));
                EmitByte((byte)(emit >> 6));
            }
            else if (emit < 128 * 128 * 128)
            {
                EmitByte((byte)(emit * 8 + 3));
                EmitByte((byte)(emit >> 5));
                EmitByte((byte)(emit >> 13));
            }
            else if (emit < 128 * 128 * 128 * 128)
            {
                EmitByte((byte)(emit * 16 + 7));
                EmitByte((byte)(emit >> 4));
                EmitByte((byte)(emit >> 12));
                EmitByte((byte)(emit >> 20));
            }
            else
            {
                EmitByte((byte)15);
                EmitInt((int)emit);
            }
        }

        public void EmitBytes(byte[] bytes)
        {
            _data.Append(bytes);
        }

        public void EmitBytes(byte[] bytes, int offset, int length)
        {
            _data.Append(bytes, offset, length);
        }

        internal void EmitBytes(ArrayBuilder<byte> bytes)
        {
            _data.Append(bytes);
        }

        public void EmitZeroPointer()
        {
            _data.ZeroExtend(_target.PointerSize);
        }

        public void EmitZeros(int numBytes)
        {
            _data.ZeroExtend(numBytes);
        }

        private Reservation GetReservationTicket(int size)
        {
#if DEBUG
            _numReservations++;
#endif
            Reservation ticket = (Reservation)_data.Count;
            _data.ZeroExtend(size);
            return ticket;
        }

#pragma warning disable CA1822 // Mark members as static
        private int ReturnReservationTicket(Reservation reservation)
#pragma warning restore CA1822 // Mark members as static
        {
#if DEBUG
            Debug.Assert(_numReservations > 0);
            _numReservations--;
#endif
            return (int)reservation;
        }

        public Reservation ReserveByte()
        {
            return GetReservationTicket(1);
        }

        public void EmitByte(Reservation reservation, byte emit)
        {
            int offset = ReturnReservationTicket(reservation);
            _data[offset] = emit;
        }

        public Reservation ReserveShort()
        {
            return GetReservationTicket(2);
        }

        public void EmitShort(Reservation reservation, short emit)
        {
            int offset = ReturnReservationTicket(reservation);
            _data[offset] = (byte)(emit & 0xFF);
            _data[offset + 1] = (byte)((emit >> 8) & 0xFF);
        }

        public Reservation ReserveInt()
        {
            return GetReservationTicket(4);
        }

        public void EmitInt(Reservation reservation, int emit)
        {
            int offset = ReturnReservationTicket(reservation);
            _data[offset] = (byte)(emit & 0xFF);
            _data[offset + 1] = (byte)((emit >> 8) & 0xFF);
            _data[offset + 2] = (byte)((emit >> 16) & 0xFF);
            _data[offset + 3] = (byte)((emit >> 24) & 0xFF);
        }

        public void EmitUInt(Reservation reservation, uint emit)
        {
            int offset = ReturnReservationTicket(reservation);
            _data[offset] = (byte)(emit & 0xFF);
            _data[offset + 1] = (byte)((emit >> 8) & 0xFF);
            _data[offset + 2] = (byte)((emit >> 16) & 0xFF);
            _data[offset + 3] = (byte)((emit >> 24) & 0xFF);
        }

        public void EmitReloc(ISymbolNode symbol, RelocType relocType, int delta = 0)
        {
#if DEBUG
            if (_checkAllSymbolDependenciesMustBeMarked)
            {
                var node = symbol as ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<NodeFactory>;
                if (node != null)
                    Debug.Assert(node.Marked);
            }
#endif

            _relocs.Add(new Relocation(relocType, _data.Count, symbol));

            // And add space for the reloc
            switch (relocType)
            {
                case RelocType.IMAGE_REL_BASED_REL32:
                case RelocType.IMAGE_REL_BASED_RELPTR32:
                case RelocType.IMAGE_REL_BASED_ABSOLUTE:
                case RelocType.IMAGE_REL_BASED_HIGHLOW:
                case RelocType.IMAGE_REL_SECREL:
                case RelocType.IMAGE_REL_TLSGD:
                case RelocType.IMAGE_REL_TPOFF:
                case RelocType.IMAGE_REL_FILE_ABSOLUTE:
                case RelocType.IMAGE_REL_BASED_ADDR32NB:
                case RelocType.IMAGE_REL_SYMBOL_SIZE:
                    EmitInt(delta);
                    break;
                case RelocType.IMAGE_REL_BASED_DIR64:
                    EmitLong(delta);
                    break;
                case RelocType.IMAGE_REL_BASED_THUMB_BRANCH24:
                case RelocType.IMAGE_REL_BASED_ARM64_BRANCH26:
                case RelocType.IMAGE_REL_BASED_THUMB_MOV32:
                case RelocType.IMAGE_REL_BASED_THUMB_MOV32_PCREL:
                case RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21:
                case RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12L:
                case RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A:

                case RelocType.IMAGE_REL_AARCH64_TLSDESC_ADR_PAGE21:
                case RelocType.IMAGE_REL_AARCH64_TLSDESC_LD64_LO12:
                case RelocType.IMAGE_REL_AARCH64_TLSDESC_ADD_LO12:
                case RelocType.IMAGE_REL_AARCH64_TLSDESC_CALL:
                case RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_HI12:
                case RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_LO12_NC:

                case RelocType.IMAGE_REL_BASED_LOONGARCH64_PC:
                case RelocType.IMAGE_REL_BASED_LOONGARCH64_JIR:

                //TODO: consider removal of IMAGE_REL_RISCV64_JALR from runtime too
                case RelocType.IMAGE_REL_BASED_RISCV64_PC:
                    Debug.Assert(delta == 0);
                    // Do not vacate space for this kind of relocation, because
                    // the space is embedded in the instruction.
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void EmitPointerReloc(ISymbolNode symbol, int delta = 0)
        {
            EmitReloc(symbol, (_target.PointerSize == 8) ? RelocType.IMAGE_REL_BASED_DIR64 : RelocType.IMAGE_REL_BASED_HIGHLOW, delta);
        }

        public ObjectNode.ObjectData ToObjectData()
        {
#if DEBUG
            Debug.Assert(_numReservations == 0);
#endif

            ObjectNode.ObjectData returnData = new ObjectNode.ObjectData(_data.ToArray(),
                                                                         _relocs.ToArray(),
                                                                         Alignment,
                                                                         _definedSymbols.ToArray());

            return returnData;
        }

        public enum Reservation { }

        public void AddSymbol(ISymbolDefinitionNode node)
        {
            _definedSymbols.Add(node);
        }

        public void PadAlignment(int align)
        {
            Debug.Assert((align == 2) || (align == 4) || (align == 8) || (align == 16));
            int misalignment = _data.Count & (align - 1);
            if (misalignment != 0)
            {
                EmitZeros(align - misalignment);
            }
        }
    }
}
