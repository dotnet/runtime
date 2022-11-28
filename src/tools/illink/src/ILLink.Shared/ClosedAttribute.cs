// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace StaticCs
{
	[AttributeUsage (AttributeTargets.Enum)]
	[Conditional ("EMIT_STATICCS_CLOSEDATTRIBUTE")]
	internal sealed class ClosedAttribute : Attribute
	{
		public ClosedAttribute () { }
	}
}