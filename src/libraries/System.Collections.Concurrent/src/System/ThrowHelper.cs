// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        internal static void ThrowKeyNullException() => ThrowArgumentNullException("key");

        [DoesNotReturn]
        internal static void ThrowArgumentNullException(string name) => throw new ArgumentNullException(name);

        [DoesNotReturn]
        internal static void ThrowArgumentNullException(string name, string message) => throw new ArgumentNullException(name, message);

        [DoesNotReturn]
        internal static void ThrowValueNullException() => throw new ArgumentException(SR.ConcurrentDictionary_TypeOfValueIncorrect);

        [DoesNotReturn]
        internal static void ThrowOutOfMemoryException() => throw new OutOfMemoryException();
    }
}
