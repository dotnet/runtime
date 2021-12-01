// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        #region Constants

        private const byte OpenBrace = (byte)'{';
        private const byte CloseBrace = (byte)'}';

        private const byte OpenParen = (byte)'(';
        private const byte CloseParen = (byte)')';

        private const byte Dash = (byte)'-';

        #endregion Constants

        /// <summary>
        /// Formats a Guid as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="destination">Buffer to write the UTF8-formatted value to</param>
        /// <param name="bytesWritten">Receives the length of the formatted text in bytes</param>
        /// <param name="format">The standard format to use</param>
        /// <returns>
        /// true for success. "bytesWritten" contains the length of the formatted text in bytes.
        /// false if buffer was too short. Iteratively increase the size of the buffer and retry until it succeeds.
        /// </returns>
        /// <remarks>
        /// Formats supported:
        ///     D (default)     nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn
        ///     B               {nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn}
        ///     P               (nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn)
        ///     N               nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(Guid value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            const int INSERT_DASHES = unchecked((int)0x80000000);
            const int NO_DASHES = 0;
            const int INSERT_CURLY_BRACES = (CloseBrace << 16) | (OpenBrace << 8);
            const int INSERT_ROUND_BRACES = (CloseParen << 16) | (OpenParen << 8);
            const int NO_BRACES = 0;
            const int LEN_GUID_BASE = 32;
            const int LEN_ADD_DASHES = 4;
            const int LEN_ADD_BRACES = 2;

            // This is a 32-bit value whose contents (where 0 is the low byte) are:
            // 0th byte: minimum required length of the output buffer,
            // 1st byte: the ASCII byte to insert for the opening brace position (or 0 if no braces),
            // 2nd byte: the ASCII byte to insert for the closing brace position (or 0 if no braces),
            // 3rd byte: high bit set if dashes are to be inserted.
            //
            // The reason for keeping a single flag instead of separate vars is that we can avoid register spillage
            // as we build up the output value.
            int flags;

            switch (FormattingHelpers.GetSymbolOrDefault(format, 'D'))
            {
                case 'D': // nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn
                    flags = INSERT_DASHES + NO_BRACES + LEN_GUID_BASE + LEN_ADD_DASHES;
                    break;

                case 'B': // {nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn}
                    flags = INSERT_DASHES + INSERT_CURLY_BRACES + LEN_GUID_BASE + LEN_ADD_DASHES + LEN_ADD_BRACES;
                    break;

                case 'P': // (nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn)
                    flags = INSERT_DASHES + INSERT_ROUND_BRACES + LEN_GUID_BASE + LEN_ADD_DASHES + LEN_ADD_BRACES;
                    break;

                case 'N': // nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn
                    flags = NO_BRACES + NO_DASHES + LEN_GUID_BASE;
                    break;

                default:
                    return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
            }

            // At this point, the low byte of flags contains the minimum required length

            if ((byte)flags > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = (byte)flags;
            flags >>= 8;

            // At this point, the low byte of flags contains the opening brace char (if any)

            if ((byte)flags != 0)
            {
                destination[0] = (byte)flags;
                destination = destination.Slice(1);
            }
            flags >>= 8;

            // At this point, the low byte of flags contains the closing brace char (if any)
            // And since we're performing arithmetic shifting the high bit of flags is set (flags is negative) if dashes are required

            DecomposedGuid guidAsBytes = default;
            guidAsBytes.Guid = value;

            // When a GUID is blitted, the first three components are native-endian, and the last component is big-endian.

            // The line below forces the JIT to hoist the bounds check for the following segment.
            // The JIT will optimize away the read, but it cannot optimize away the bounds check
            // because it may have an observable side effect (throwing).
            // We use 8 instead of 7 so that we also capture the dash if we're asked to insert one.

            { _ = destination[8]; }
            if (BitConverter.IsLittleEndian)
            {
                HexConverter.ToBytesBuffer(guidAsBytes.Byte03, destination, 0, HexConverter.Casing.Lower);
                HexConverter.ToBytesBuffer(guidAsBytes.Byte02, destination, 2, HexConverter.Casing.Lower);
                HexConverter.ToBytesBuffer(guidAsBytes.Byte01, destination, 4, HexConverter.Casing.Lower);
                HexConverter.ToBytesBuffer(guidAsBytes.Byte00, destination, 6, HexConverter.Casing.Lower);
            }
            else
            {
                HexConverter.ToBytesBuffer(guidAsBytes.Byte00, destination, 0, HexConverter.Casing.Lower);
                HexConverter.ToBytesBuffer(guidAsBytes.Byte01, destination, 2, HexConverter.Casing.Lower);
                HexConverter.ToBytesBuffer(guidAsBytes.Byte02, destination, 4, HexConverter.Casing.Lower);
                HexConverter.ToBytesBuffer(guidAsBytes.Byte03, destination, 6, HexConverter.Casing.Lower);
            }

            if (flags < 0 /* use dash? */)
            {
                destination[8] = Dash;
                destination = destination.Slice(9);
            }
            else
            {
                destination = destination.Slice(8);
            }

            { _ = destination[4]; }
            if (BitConverter.IsLittleEndian)
            {
                HexConverter.ToBytesBuffer(guidAsBytes.Byte05, destination, 0, HexConverter.Casing.Lower);
                HexConverter.ToBytesBuffer(guidAsBytes.Byte04, destination, 2, HexConverter.Casing.Lower);
            }
            else
            {
                HexConverter.ToBytesBuffer(guidAsBytes.Byte04, destination, 0, HexConverter.Casing.Lower);
                HexConverter.ToBytesBuffer(guidAsBytes.Byte05, destination, 2, HexConverter.Casing.Lower);
            }

            if (flags < 0 /* use dash? */)
            {
                destination[4] = Dash;
                destination = destination.Slice(5);
            }
            else
            {
                destination = destination.Slice(4);
            }

            { _ = destination[4]; }
            if (BitConverter.IsLittleEndian)
            {
                HexConverter.ToBytesBuffer(guidAsBytes.Byte07, destination, 0, HexConverter.Casing.Lower);
                HexConverter.ToBytesBuffer(guidAsBytes.Byte06, destination, 2, HexConverter.Casing.Lower);
            }
            else
            {
                HexConverter.ToBytesBuffer(guidAsBytes.Byte06, destination, 0, HexConverter.Casing.Lower);
                HexConverter.ToBytesBuffer(guidAsBytes.Byte07, destination, 2, HexConverter.Casing.Lower);
            }

            if (flags < 0 /* use dash? */)
            {
                destination[4] = Dash;
                destination = destination.Slice(5);
            }
            else
            {
                destination = destination.Slice(4);
            }

            { _ = destination[4]; }
            HexConverter.ToBytesBuffer(guidAsBytes.Byte08, destination, 0, HexConverter.Casing.Lower);
            HexConverter.ToBytesBuffer(guidAsBytes.Byte09, destination, 2, HexConverter.Casing.Lower);

            if (flags < 0 /* use dash? */)
            {
                destination[4] = Dash;
                destination = destination.Slice(5);
            }
            else
            {
                destination = destination.Slice(4);
            }

            { _ = destination[11]; } // can't hoist bounds check on the final brace (if exists)
            HexConverter.ToBytesBuffer(guidAsBytes.Byte10, destination, 0, HexConverter.Casing.Lower);
            HexConverter.ToBytesBuffer(guidAsBytes.Byte11, destination, 2, HexConverter.Casing.Lower);
            HexConverter.ToBytesBuffer(guidAsBytes.Byte12, destination, 4, HexConverter.Casing.Lower);
            HexConverter.ToBytesBuffer(guidAsBytes.Byte13, destination, 6, HexConverter.Casing.Lower);
            HexConverter.ToBytesBuffer(guidAsBytes.Byte14, destination, 8, HexConverter.Casing.Lower);
            HexConverter.ToBytesBuffer(guidAsBytes.Byte15, destination, 10, HexConverter.Casing.Lower);

            if ((byte)flags != 0)
            {
                destination[12] = (byte)flags;
            }

            return true;
        }

        /// <summary>
        /// Used to provide access to the individual bytes of a GUID.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct DecomposedGuid
        {
            [FieldOffset(00)] public Guid Guid;
            [FieldOffset(00)] public byte Byte00;
            [FieldOffset(01)] public byte Byte01;
            [FieldOffset(02)] public byte Byte02;
            [FieldOffset(03)] public byte Byte03;
            [FieldOffset(04)] public byte Byte04;
            [FieldOffset(05)] public byte Byte05;
            [FieldOffset(06)] public byte Byte06;
            [FieldOffset(07)] public byte Byte07;
            [FieldOffset(08)] public byte Byte08;
            [FieldOffset(09)] public byte Byte09;
            [FieldOffset(10)] public byte Byte10;
            [FieldOffset(11)] public byte Byte11;
            [FieldOffset(12)] public byte Byte12;
            [FieldOffset(13)] public byte Byte13;
            [FieldOffset(14)] public byte Byte14;
            [FieldOffset(15)] public byte Byte15;
        }
    }
}
