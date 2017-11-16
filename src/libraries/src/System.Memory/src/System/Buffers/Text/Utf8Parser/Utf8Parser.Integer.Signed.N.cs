// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        private static bool TryParseSByteN(ReadOnlySpan<byte> text, out sbyte value, out int bytesConsumed)
        {
            throw NotImplemented.ActiveIssue("https://github.com/dotnet/corefx/issues/24986");
        }

        private static bool TryParseInt16N(ReadOnlySpan<byte> text, out short value, out int bytesConsumed)
        {
            throw NotImplemented.ActiveIssue("https://github.com/dotnet/corefx/issues/24986");
        }

        private static bool TryParseInt32N(ReadOnlySpan<byte> text, out int value, out int bytesConsumed)
        {
            throw NotImplemented.ActiveIssue("https://github.com/dotnet/corefx/issues/24986");
        }

        private static bool TryParseInt64N(ReadOnlySpan<byte> text, out long value, out int bytesConsumed)
        {
            throw NotImplemented.ActiveIssue("https://github.com/dotnet/corefx/issues/24986");
        }
    }
}
