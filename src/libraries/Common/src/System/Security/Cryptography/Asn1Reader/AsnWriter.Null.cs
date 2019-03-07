// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Security.Cryptography.Asn1
{
    internal sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write NULL with tag UNIVERSAL 5.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteNull()
        {
            WriteNullCore(Asn1Tag.Null);
        }

        /// <summary>
        ///   Write NULL with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteNull(Asn1Tag tag)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Null);

            WriteNullCore(tag.AsPrimitive());
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
