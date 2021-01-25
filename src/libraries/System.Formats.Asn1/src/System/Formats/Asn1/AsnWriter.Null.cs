// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Formats.Asn1
{
    public sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write NULL with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 5).</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public void WriteNull(Asn1Tag? tag = null)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Null);

            WriteNullCore(tag?.AsPrimitive() ?? Asn1Tag.Null);
        }

        // T-REC-X.690-201508 sec 8.8
        private void WriteNullCore(Asn1Tag tag)
        {
            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);
            WriteLength(0);
        }
    }
}
