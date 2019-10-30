// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
	partial struct GCHandle
	{
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern IntPtr InternalAlloc (object value, GCHandleType type);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern void InternalFree (IntPtr handle);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern object InternalGet (IntPtr handle);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern void InternalSet (IntPtr handle, object value);
	}
}
