// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;

namespace Microsoft.NET.HostModel.Win32Resources
{
    public struct ObjectDataBuilder
    {
        public ObjectDataBuilder()
        {
            _data = default(ArrayBuilder<byte>);
#if DEBUG
            _numReservations = 0;
#endif
        }

        private ArrayBuilder<byte> _data;

#if DEBUG
        private int _numReservations;
#endif

        public int CountBytes
        {
            get
            {
                return _data.Count;
            }
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

        public byte[] ToData()
        {
#if DEBUG
            Debug.Assert(_numReservations == 0);
#endif

            return _data.ToArray();
        }

        public enum Reservation { }

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
