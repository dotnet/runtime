// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Formats.Asn1
{
    public sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write a Boolean value with a specified tag.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 1).</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public void WriteBoolean(bool value, Asn1Tag? tag = null)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Boolean);

            WriteBooleanCore(tag?.AsPrimitive() ?? Asn1Tag.Boolean, value);
        }

        // T-REC-X.690-201508 sec 11.1, 8.2
        private void WriteBooleanCore(Asn1Tag tag, bool value)
        {
            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);
            WriteLength(1);
            // Ensured by WriteLength
            Debug.Assert(_offset < _buffer.Length);
            _buffer[_offset] = (byte)(value ? 0xFF : 0x00);
            _offset++;
        }
    }
}
