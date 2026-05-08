// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
        private static unsafe void OnUnhandledException(object* pException, Exception* pOutException)
        {
            try
            {
                OnUnhandledException(*pException);
            }
            catch
            {
                // The VM does not expect exceptions to propagate out of this callback
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe void OnFirstChanceException(Exception* pException, Exception* pOutException)
        {
            try
            {
                OnFirstChanceException(*pException, AppDomain.CurrentDomain);
            }
            catch (Exception ex)
            {
                *pOutException = ex;
            }
        }

        internal static void SetFirstChanceExceptionHandler()
            => SetFirstChanceExceptionHandlerInternal();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AppContext_SetFirstChanceExceptionHandler")]
        [SuppressGCTransition]
        private static partial void SetFirstChanceExceptionHandlerInternal();
    }
}
