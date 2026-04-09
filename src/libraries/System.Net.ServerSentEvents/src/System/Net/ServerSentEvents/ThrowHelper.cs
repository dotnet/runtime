// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.ServerSentEvents
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string parameterName)
        {
            throw new ArgumentNullException(parameterName);
        }

        public static void ThrowArgumentException_CannotContainLineBreaks(string parameterName)
        {
            throw new ArgumentException(SR.ArgumentException_CannotContainLineBreaks, parameterName);
        }

        public static void ThrowArgumentException_CannotBeNegative(string parameterName)
        {
            throw new ArgumentException(SR.ArgumentException_CannotBeNegative, parameterName);
        }
    }
}
