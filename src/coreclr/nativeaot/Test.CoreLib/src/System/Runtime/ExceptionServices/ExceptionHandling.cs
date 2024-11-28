// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Runtime.ExceptionServices
{
    public delegate bool UnhandledExceptionHandler(System.Exception exception);

    public static class ExceptionHandling
    {
        internal static UnhandledExceptionHandler? s_handler;
    }
}
