// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

using size_t = System.IntPtr;

internal static partial class Interop
{
    internal static partial class libc
    {
		public unsafe struct iovec
		{
			public void* iov_base;
			public size_t iov_len;
		}
    }
}
