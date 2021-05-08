// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Mono.Linker
{
	[Flags]
	public enum MetadataTrimming
	{
		None = 0,
		ParameterName = 1,

		Any = ParameterName
	}
}
