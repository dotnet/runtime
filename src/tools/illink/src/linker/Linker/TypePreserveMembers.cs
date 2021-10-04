// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker
{
	[Flags]
	public enum TypePreserveMembers
	{
		Visible = 1 << 1,
		Internal = 1 << 2,
		Library = 1 << 3
	}
}
