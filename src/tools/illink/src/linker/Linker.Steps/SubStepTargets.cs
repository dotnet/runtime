// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Steps
{
	[Flags]
	public enum SubStepTargets
	{
		None = 0,

		Assembly = 1,
		Type = 2,
		Field = 4,
		Method = 8,
		Property = 16,
		Event = 32,
	}
}
