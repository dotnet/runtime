// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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