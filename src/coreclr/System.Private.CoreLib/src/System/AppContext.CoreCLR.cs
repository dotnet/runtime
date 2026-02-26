// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    public static partial class AppContext
    {
        [UnmanagedCallersOnly]
        private static unsafe void OnProcessExit(Exception* pException)
        {
            try
            {
                OnProcessExit();
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe void OnUnhandledException(object* pUnhandledException, Exception* _)
        {
            try
            {
                OnUnhandledException(*pUnhandledException);
            }
            catch
            {
                // The VM does not expect exceptions to propagate out of this callback
            }
        }
    }
}
