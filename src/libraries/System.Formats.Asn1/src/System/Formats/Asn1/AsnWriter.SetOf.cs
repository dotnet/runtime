// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Formats.Asn1
{
    public sealed partial class AsnWriter
    {
        /// <summary>
        ///   Begin writing a Set-Of with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 17).</param>
        /// <returns>
        ///   A disposable value which will automatically call <see cref="PopSetOf"/>.
        /// </returns>
        /// <remarks>
        ///   In <see cref="AsnEncodingRules.CER"/> and <see cref="AsnEncodingRules.DER"/> modes
        ///   the writer will sort the Set-Of elements when the tag is closed.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <seealso cref="PopSetOf"/>
        public Scope PushSetOf(Asn1Tag? tag = null)
        {
            CheckUniversalTag(tag, UniversalTagNumber.SetOf);

            // Assert the constructed flag, in case it wasn't.
            return PushSetOfCore(tag?.AsConstructed() ?? Asn1Tag.SetOf);
        }

        /// <summary>
        ///   Indicate that the open Set-Of with the specified tag is closed,
        ///   returning the writer to the parent context.
        /// </summary>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 17).</param>
        /// <remarks>
        ///   In <see cref="AsnEncodingRules.CER"/> and <see cref="AsnEncodingRules.DER"/> modes
        ///   the writer will sort the Set-Of elements when the tag is closed.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   the writer is not currently positioned within a Set-Of with the specified tag.
        /// </exception>
        /// <seealso cref="PushSetOf"/>
        public void PopSetOf(Asn1Tag? tag = null)
        {
            CheckUniversalTag(tag, UniversalTagNumber.SetOf);

            // Assert the constructed flag, in case it wasn't.
            PopSetOfCore(tag?.AsConstructed() ?? Asn1Tag.SetOf);
        }

        // T-REC-X.690-201508 sec 8.12
        // The writer claims SetOf, and not Set, so as to avoid the field
        // ordering clause of T-REC-X.690-201508 sec 9.3
        private Scope PushSetOfCore(Asn1Tag tag)
        {
            Debug.Assert(tag.IsConstructed);
            return PushTag(tag, UniversalTagNumber.SetOf);
        }

        // T-REC-X.690-201508 sec 8.12
        private void PopSetOfCore(Asn1Tag tag)
        {
            Debug.Assert(tag.IsConstructed);

            // T-REC-X.690-201508 sec 11.6
            bool sortContents = RuleSet == AsnEncodingRules.CER || RuleSet == AsnEncodingRules.DER;

            PopTag(tag, UniversalTagNumber.SetOf, sortContents);
        }
    }
}
