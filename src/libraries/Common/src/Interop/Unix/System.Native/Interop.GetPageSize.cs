// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Sys
    {
        private static volatile int s_pageSize;

        internal static int PageSize
        {
            get
            {
                int size = s_pageSize;
                if (size == 0)
                {
                    Interlocked.CompareExchange(ref s_pageSize, GetPageSize(), 0);
                    size = s_pageSize;
                }

                return size;
            }
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPageSize")]
        private static partial int GetPageSize();
    }
}
