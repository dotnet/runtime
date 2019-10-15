// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Asn1
{
    /// <summary>
    ///   Tag assignments for the UNIVERSAL class in ITU-T X.680.
    /// </summary>
    // ITU-T-REC.X.680-201508 sec 8.6
    internal enum UniversalTagNumber
    {
        /// <summary>
        ///   The reserved identifier for the End-of-Contents marker in an indefinite
        ///   length encoding.
        /// </summary>
        EndOfContents = 0,

        /// <summary>
        ///   The universal class tag value for Boolean.
        /// </summary>
        Boolean = 1,

        /// <summary>
        ///   The universal class tag value for Integer.
        /// </summary>
        Integer = 2,

        /// <summary>
        ///   The universal class tag value for Bit String.
        /// </summary>
        BitString = 3,

        /// <summary>
        ///   The universal class tag value for Octet String.
        /// </summary>
        OctetString = 4,

        /// <summary>
        ///   The universal class tag value for Null.
        /// </summary>
        Null = 5,

        /// <summary>
        ///   The universal class tag value for Object Identifier.
        /// </summary>
        ObjectIdentifier = 6,

        /// <summary>
        ///   The universal class tag value for Object Descriptor.
        /// </summary>
        ObjectDescriptor = 7,

        /// <summary>
        ///   The universal class tag value for External.
        /// </summary>
        External = 8,

        /// <summary>
        ///   The universal class tag value for Instance-Of.
        /// </summary>
        InstanceOf = External,

        /// <summary>
        ///   The universal class tag value for Real.
        /// </summary>
        Real = 9,

        /// <summary>
        ///   The universal class tag value for Enumerated.
        /// </summary>
        Enumerated = 10,

        /// <summary>
        ///   The universal class tag value for Embedded-PDV.
        /// </summary>
        Embedded = 11,

        /// <summary>
        ///   The universal class tag value for UTF8String.
        /// </summary>
        UTF8String = 12,

        /// <summary>
        ///   The universal class tag value for Relative Object Identifier.
        /// </summary>
        RelativeObjectIdentifier = 13,

        /// <summary>
        ///   The universal class tag value for Time.
        /// </summary>
        Time = 14,

        // 15 is reserved

        /// <summary>
        ///   The universal class tag value for Sequence.
        /// </summary>
        Sequence = 16,

        /// <summary>
        ///   The universal class tag value for Sequence-Of.
        /// </summary>
        SequenceOf = Sequence,

        /// <summary>
        ///   The universal class tag value for Set.
        /// </summary>
        Set = 17,

        /// <summary>
        ///   The universal class tag value for Set-Of.
        /// </summary>
        SetOf = Set,

        /// <summary>
        ///   The universal class tag value for NumericString.
        /// </summary>
        NumericString = 18,

        /// <summary>
        ///   The universal class tag value for PrintableString.
        /// </summary>
        PrintableString = 19,

        /// <summary>
        ///   The universal class tag value for TeletexString (T61String).
        /// </summary>
        TeletexString = 20,

        /// <summary>
        ///   The universal class tag value for T61String (TeletexString).
        /// </summary>
        T61String = TeletexString,

        /// <summary>
        ///   The universal class tag value for VideotexString.
        /// </summary>
        VideotexString = 21,

        /// <summary>
        ///   The universal class tag value for IA5String.
        /// </summary>
        IA5String = 22,

        /// <summary>
        ///   The universal class tag value for UTCTime.
        /// </summary>
        UtcTime = 23,

        /// <summary>
        ///   The universal class tag value for GeneralizedTime.
        /// </summary>
        GeneralizedTime = 24,

        /// <summary>
        ///   The universal class tag value for GraphicString.
        /// </summary>
        GraphicString = 25,

        /// <summary>
        ///   The universal class tag value for VisibleString (ISO646String).
        /// </summary>
        VisibleString = 26,

        /// <summary>
        ///   The universal class tag value for ISO646String (VisibleString).
        /// </summary>
        ISO646String = VisibleString,

        /// <summary>
        ///   The universal class tag value for GeneralString.
        /// </summary>
        GeneralString = 27,

        /// <summary>
        ///   The universal class tag value for UniversalString.
        /// </summary>
        UniversalString = 28,

        /// <summary>
        ///   The universal class tag value for an unrestricted character string.
        /// </summary>
        UnrestrictedCharacterString = 29,

        /// <summary>
        ///   The universal class tag value for BMPString.
        /// </summary>
        BMPString = 30,

        /// <summary>
        ///   The universal class tag value for Date.
        /// </summary>
        Date = 31,

        /// <summary>
        ///   The universal class tag value for Time-Of-Day.
        /// </summary>
        TimeOfDay = 32,

        /// <summary>
        ///   The universal class tag value for Date-Time.
        /// </summary>
        DateTime = 33,

        /// <summary>
        ///   The universal class tag value for Duration.
        /// </summary>
        Duration = 34,

        /// <summary>
        ///   The universal class tag value for Object Identifier
        ///   Internationalized Resource Identifier (IRI).
        /// </summary>
        ObjectIdentifierIRI = 35,

        /// <summary>
        ///   The universal class tag value for Relative Object Identifier
        ///   Internationalized Resource Identifier (IRI).
        /// </summary>
        RelativeObjectIdentifierIRI = 36,
    }
}
