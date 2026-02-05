// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Runtime.ExceptionServices
{
    public static class ExceptionHandling
    {
        internal static bool IsHandledByGlobalHandler(Exception ex)
        {
            return false;
        }
    }
}
