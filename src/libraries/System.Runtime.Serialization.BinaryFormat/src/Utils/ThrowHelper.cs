// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal static class ThrowHelper
{
    internal static void ThrowUnexpectedNullRecordCount()
        => throw new SerializationException("Unexpected Null Record count.");

    internal static Exception InvalidPrimitiveType(PrimitiveType primitiveType)
        => new SerializationException($"Invalid primitive type: {primitiveType}");

    internal static Exception InvalidBinaryType(BinaryType binaryType)
        => new SerializationException($"Invalid binary type: {binaryType}");

    internal static void ThrowMaxArrayLength(int limit, uint actual)
        => throw new SerializationException(
            $"The serialized array length ({actual}) was larger that the configured limit {limit}");

    internal static void ThrowArrayContainedNull()
        => throw new SerializationException("The array contained null(s)");

    internal static void ThrowEndOfStreamException()
        => throw new EndOfStreamException();

    internal static void ThrowForUnexpectedRecordType(RecordType recordType)
    {
        // The enum values are not part of the public API surface, as they are not supported
        // and users don't need to handle these values.

#pragma warning disable IDE0066 // Convert switch statement to expression
        switch ((int)recordType)
        {
            case 2: // SystemClassWithMembers
            case 3: // ClassWithMembers
                throw new NotSupportedException("FormatterTypeStyle.TypesAlways is a must have.");
            // 18~20 are from the reference source but aren't in the OpenSpecs doc
            case 18: // CrossAppDomainMap
            case 19: // CrossAppDomainString
            case 20: // CrossAppDomainAssembly
                throw new NotSupportedException("Cross domain is not supported by design");
            case 21: // MethodCall
            case 22: // MethodReturn
                throw new NotSupportedException("Remote invocation is not supported by design");
            default:
                throw new SerializationException($"Unexpected type seen: {recordType}.");
        }
#pragma warning restore IDE0066 // Convert switch statement to expression
    }
}
