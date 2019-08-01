// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
	partial class Object
	{
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern Type GetType ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		protected extern object MemberwiseClone ();

		[Intrinsic]
		internal ref byte GetRawData () => throw new NotImplementedException ();

		internal object CloneInternal () => MemberwiseClone ();
	}
}
