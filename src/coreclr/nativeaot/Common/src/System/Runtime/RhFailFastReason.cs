// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    internal enum RhFailFastReason
    {
        Unknown = 0,
        InternalError = 1,                                   // "Runtime internal error"
        UnhandledException = 2,                              // "unhandled exception"
        UnhandledExceptionFromPInvoke = 3,                   // "Unhandled exception: an unmanaged exception was thrown out of a managed-to-native transition."
        EnvironmentFailFast = 4,
        AssertionFailure = 5,
    }
}
