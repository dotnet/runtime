// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Formats.Asn1
{
    public sealed partial class AsnWriter
    {
        /// <summary>
        ///   Begin writing a Sequence with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 16).</param>
        /// <returns>
        ///   A disposable value which will automatically call <see cref="PopSequence"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <seealso cref="PopSequence"/>
        public Scope PushSequence(Asn1Tag? tag = null)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Sequence);

            // Assert the constructed flag, in case it wasn't.
            return PushSequenceCore(tag?.AsConstructed() ?? Asn1Tag.Sequence);
        }

        /// <summary>
        ///   Indicate that the open Sequence with the specified tag is closed,
        ///   returning the writer to the parent context.
        /// </summary>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 16).</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   the writer is not currently positioned within a Sequence with the specified tag.
        /// </exception>
        /// <seealso cref="PushSequence"/>
        public void PopSequence(Asn1Tag? tag = null)
        {
            // PopSequence shouldn't be used to pop a SetOf.
            CheckUniversalTag(tag, UniversalTagNumber.Sequence);

            // Assert the constructed flag, in case it wasn't.
            PopSequenceCore(tag?.AsConstructed() ?? Asn1Tag.Sequence);
        }

        // T-REC-X.690-201508 sec 8.9, 8.10
        private Scope PushSequenceCore(Asn1Tag tag)
        {
            Debug.Assert(tag.IsConstructed);
            return PushTag(tag, UniversalTagNumber.Sequence);
        }

        // T-REC-X.690-201508 sec 8.9, 8.10
        private void PopSequenceCore(Asn1Tag tag)
        {
            Debug.Assert(tag.IsConstructed);
            PopTag(tag, UniversalTagNumber.Sequence);
        }
    }
}
