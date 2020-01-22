// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
	static class UnsafeNativeMethods
	{
		internal static unsafe class ManifestEtw
		{
			internal unsafe delegate void EtwEnableCallback(
				[In] ref Guid sourceId,
				[In] int isEnabled,
				[In] byte level,
				[In] long matchAnyKeywords,
				[In] long matchAllKeywords,
				[In] EVENT_FILTER_DESCRIPTOR* filterData,
				[In] void* callbackContext
				);

			[StructLayout(LayoutKind.Sequential)]
			unsafe internal struct EVENT_FILTER_DESCRIPTOR
			{
				public long Ptr;
				public int Size;
				public int Type;
			}

			internal enum ActivityControl : uint
			{
				EVENT_ACTIVITY_CTRL_GET_ID = 1,
				EVENT_ACTIVITY_CTRL_SET_ID = 2,
				EVENT_ACTIVITY_CTRL_CREATE_ID = 3,
				EVENT_ACTIVITY_CTRL_GET_SET_ID = 4,
				EVENT_ACTIVITY_CTRL_CREATE_SET_ID = 5
			}

			internal const int ERROR_ARITHMETIC_OVERFLOW = 534;
			internal const int ERROR_NOT_ENOUGH_MEMORY = 8;
			internal const int ERROR_MORE_DATA = 0xEA;

			internal const int EVENT_CONTROL_CODE_DISABLE_PROVIDER = 0;
			internal const int EVENT_CONTROL_CODE_ENABLE_PROVIDER = 1;
			internal const int EVENT_CONTROL_CODE_CAPTURE_STATE = 2;
		}
	}
}
