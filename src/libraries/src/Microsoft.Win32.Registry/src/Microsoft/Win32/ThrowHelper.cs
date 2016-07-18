// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;

namespace Microsoft.Win32
{
    internal static class ThrowHelper
    {
        internal static void ThrowArgumentException(string msg)
        {
            throw new ArgumentException(msg);
        }

        internal static void ThrowArgumentException(string msg, string argument)
        {
            throw new ArgumentException(msg, argument);
        }

        internal static void ThrowArgumentNullException(string argument)
        {
            throw new ArgumentNullException(argument);
        }

        internal static void ThrowInvalidOperationException(string msg)
        {
            throw new InvalidOperationException(msg);
        }

        internal static void ThrowSecurityException(string msg)
        {
            throw new SecurityException(msg);
        }

        internal static void ThrowUnauthorizedAccessException(string msg)
        {
            throw new UnauthorizedAccessException(msg);
        }

        internal static void ThrowObjectDisposedException(string objectName, string msg)
        {
            throw new ObjectDisposedException(objectName, msg);
        }
    }
}
