// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Data.Odbc
{
    internal sealed class OdbcEnvironment
    {
        private static object s_globalEnvironmentHandle;
        private static object s_globalEnvironmentHandleLock = new object();

        private OdbcEnvironment() { }  // default const.

        internal static OdbcEnvironmentHandle GetGlobalEnvironmentHandle()
        {
            OdbcEnvironmentHandle globalEnvironmentHandle = s_globalEnvironmentHandle as OdbcEnvironmentHandle;
            if (null == globalEnvironmentHandle)
            {
                lock (s_globalEnvironmentHandleLock)
                {
                    globalEnvironmentHandle = s_globalEnvironmentHandle as OdbcEnvironmentHandle;
                    if (null == globalEnvironmentHandle)
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
            object globalEnvironmentHandle = Interlocked.Exchange(ref s_globalEnvironmentHandle, null);
            if (null != globalEnvironmentHandle)
            {
                (globalEnvironmentHandle as OdbcEnvironmentHandle).Dispose(); // internally refcounted so will happen correctly
            }
        }
    }
}
