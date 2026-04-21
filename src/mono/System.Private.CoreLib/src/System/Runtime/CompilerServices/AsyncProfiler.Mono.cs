// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    internal static partial class AsyncProfiler
    {
        internal static partial class ContinuationWrapper
        {
            public static void InitInfo(ref Info info)
            {
                info.ContinuationIndex = 0;
                info.ContinuationTable = ref s_dummyContinuationTable;
            }

            public static long[] GetContinuationWrapperIPs()
            {
                return new long[COUNT];
            }

            private static nint s_dummyContinuationTable;
        }

        private static partial class SyncPoint
        {
            private static unsafe void ResumeAsyncCallstacks(AsyncThreadContext _)
            {
            }
        }
    }
}
