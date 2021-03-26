// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Net.Security
{
    //
    // Implements delayed SSPI handle release:
    // finalizable objects though the handles are kept alive until being pushed out by the newly incoming ones.
    //
    internal static class SSPIHandleCache
    {
        private const int c_MaxCacheSize = 0x1F;  // must a (power of 2) - 1
        private static readonly SafeCredentialReference?[] s_cacheSlots = new SafeCredentialReference[c_MaxCacheSize + 1];
        private static int s_current = -1;

        internal static void CacheCredential(SafeFreeCredentials newHandle)
        {
            try
            {
                SafeCredentialReference? newRef = SafeCredentialReference.CreateReference(newHandle);
                if (newRef == null)
                {
                    return;
                }

                int index = Interlocked.Increment(ref s_current) & c_MaxCacheSize;
#pragma warning disable CS8601 // Possible null reference assignment.
                newRef = Interlocked.Exchange<SafeCredentialReference>(ref s_cacheSlots[index], newRef);
#pragma warning restore CS8601 // Possible null reference assignment.

                newRef?.Dispose();
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled() && !ExceptionCheck.IsFatal(e))
                {
                    NetEventSource.Error(null, $"Attempted to throw: {e}");
                }
            }
        }
    }
}
