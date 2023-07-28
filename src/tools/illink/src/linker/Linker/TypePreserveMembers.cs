// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
