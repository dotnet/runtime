// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection {
	[StructLayout (LayoutKind.Sequential)]
	internal sealed class RuntimeLocalVariableInfo : LocalVariableInfo {
		#region Keep in sync with MonoReflectionLocalVariableInfo in object-internals.h
		internal Type type;
		internal bool is_pinned;
		internal ushort position;
		#endregion

		public override bool IsPinned => is_pinned;

		public override int LocalIndex => position;

		public override Type LocalType => type;
	}

}

