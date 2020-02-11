// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public enum DSASignatureFormat
    {
        IeeeP1363FixedFieldConcatenation,
        Rfc3279DerSequence
    }

    internal static class DSASignatureFormatHelpers
    {
        internal static bool IsKnownValue(this DSASignatureFormat signatureFormat) =>
            signatureFormat >= DSASignatureFormat.IeeeP1363FixedFieldConcatenation &&
            signatureFormat <= DSASignatureFormat.Rfc3279DerSequence;

        internal static Exception CreateUnknownValueException(DSASignatureFormat signatureFormat) =>
            new ArgumentOutOfRangeException(
                nameof(signatureFormat),
                SR.Format(SR.Cryptography_UnknownSignatureFormat, signatureFormat));
    }
}
