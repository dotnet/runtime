// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Data.Odbc
{
    internal sealed class OdbcEnvironment
    {
        private static object? s_globalEnvironmentHandle;
        private static readonly object s_globalEnvironmentHandleLock = new object();

        private OdbcEnvironment() { }  // default const.

        internal static OdbcEnvironmentHandle GetGlobalEnvironmentHandle()
        {
            OdbcEnvironmentHandle? globalEnvironmentHandle = s_globalEnvironmentHandle as OdbcEnvironmentHandle;
            if (globalEnvironmentHandle is null)
            {
                lock (s_globalEnvironmentHandleLock)
                {
                    globalEnvironmentHandle = s_globalEnvironmentHandle as OdbcEnvironmentHandle;
                    if (globalEnvironmentHandle is null)
                    {
                        globalEnvironmentHandle = new OdbcEnvironmentHandle();
                        s_globalEnvironmentHandle = globalEnvironmentHandle;
                    }
                }
            }
            return globalEnvironmentHandle;
        }

        internal static void ReleaseObjectPool()
        {
            object? globalEnvironmentHandle = Interlocked.Exchange(ref s_globalEnvironmentHandle, null);
            if (globalEnvironmentHandle is not null)
            {
                ((OdbcEnvironmentHandle)globalEnvironmentHandle).Dispose(); // internally refcounted so will happen correctly
            }
        }
    }
}
