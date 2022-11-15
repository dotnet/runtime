// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.NativeFormat;

namespace Internal.Runtime.CompilerHelpers
{
    internal static class StringLiteralHelpers
    {
        internal static unsafe string GetStringLiteral(byte* data)
        {
            // TODO: we actually want WTF-8, not UTF-8
            // TODO: do we need a caching scheme?
            var reader = new NativeReader(data, (uint.MaxValue / 4) - 1);
            reader.DecodeString(0, out string literal);
            return string.Intern(literal);
        }
    }
}
