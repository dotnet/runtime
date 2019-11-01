// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Asn1
{
    internal sealed partial class AsnWriter
    {
        /// <summary>
        ///   Begin writing a Sequence with tag UNIVERSAL 16.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="PushSequence(Asn1Tag)"/>
        /// <seealso cref="PopSequence()"/>
        public void PushSequence()
        {
            PushSequenceCore(Asn1Tag.Sequence);
        }

        /// <summary>
        ///   Begin writing a Sequence with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="PopSequence(Asn1Tag)"/>
        public void PushSequence(Asn1Tag tag)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Sequence);

            // Assert the constructed flag, in case it wasn't.
            PushSequenceCore(tag.AsConstructed());
        }

        /// <summary>
        ///   Indicate that the open Sequence with tag UNIVERSAL 16 is closed,
        ///   returning the writer to the parent context.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   the writer is not currently positioned within a Sequence with tag UNIVERSAL 16
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="PopSequence(Asn1Tag)"/>
        /// <seealso cref="PushSequence()"/>
        public void PopSequence()
        {
            PopSequenceCore(Asn1Tag.Sequence);
        }

        /// <summary>
        ///   Indicate that the open Sequence with the specified tag is closed,
        ///   returning the writer to the parent context.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   the writer is not currently positioned within a Sequence with the specified tag
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="PopSequence(System.Security.Cryptography.Asn1.Asn1Tag)"/>
        /// <seealso cref="PushSequence()"/>
        public void PopSequence(Asn1Tag tag)
        {
            // PopSequence shouldn't be used to pop a SetOf.
            CheckUniversalTag(tag, UniversalTagNumber.Sequence);

            // Assert the constructed flag, in case it wasn't.
            PopSequenceCore(tag.AsConstructed());
        }

        // T-REC-X.690-201508 sec 8.9, 8.10
        private void PushSequenceCore(Asn1Tag tag)
        {
            PushTag(tag.AsConstructed(), UniversalTagNumber.Sequence);
        }

        // T-REC-X.690-201508 sec 8.9, 8.10
        private void PopSequenceCore(Asn1Tag tag)
        {
            PopTag(tag, UniversalTagNumber.Sequence);
        }
    }
}
