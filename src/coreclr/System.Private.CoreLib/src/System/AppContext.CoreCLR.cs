// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    public partial class AppContext
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
        private static unsafe void OnUnhandledException(object* pException, Exception* pOutException)
        {
            try
            {
                OnUnhandledException(*pException);
            }
            catch (Exception ex)
            {
                *pOutException = ex;
            }
        }
    }
}
