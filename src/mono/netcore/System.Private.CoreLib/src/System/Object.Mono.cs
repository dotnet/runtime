// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
	partial class Object
	{
		[Intrinsic]
		public Type GetType () => GetType ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		protected extern object MemberwiseClone ();

		[Intrinsic]
		internal ref byte GetRawData () => ref GetRawData ();

		internal object CloneInternal () => MemberwiseClone ();
	}
}
