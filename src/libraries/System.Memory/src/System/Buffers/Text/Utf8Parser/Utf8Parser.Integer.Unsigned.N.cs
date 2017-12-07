// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    //
    // Parsing unsigned integers for the 'N' format. Emulating int.TryParse(NumberStyles.AllowThousands | NumberStyles.Integer | NumberStyles.AllowDecimalPoint)
    //
    public static partial class Utf8Parser
    {
        private static bool TryParseByteN(ReadOnlySpan<byte> text, out byte value, out int bytesConsumed)
        {
            throw NotImplemented.ActiveIssue("https://github.com/dotnet/corefx/issues/24986");
        }

        private static bool TryParseUInt16N(ReadOnlySpan<byte> text, out ushort value, out int bytesConsumed)
        {
            throw NotImplemented.ActiveIssue("https://github.com/dotnet/corefx/issues/24986");
        }

        private static bool TryParseUInt32N(ReadOnlySpan<byte> text, out uint value, out int bytesConsumed)
        {
            throw NotImplemented.ActiveIssue("https://github.com/dotnet/corefx/issues/24986");
        }

        private static bool TryParseUInt64N(ReadOnlySpan<byte> text, out ulong value, out int bytesConsumed)
        {
            throw NotImplemented.ActiveIssue("https://github.com/dotnet/corefx/issues/24986");
        }
    }
}
