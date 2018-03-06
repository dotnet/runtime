// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    // @todo: https://github.com/dotnet/corefx/issues/26894 - these emulate MemoryExtension apis that we removed. Clean up the callsites and remove this class.
    internal static class TestHelpers
    {
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] array) => new ReadOnlySpan<T>(array);
    }
}
