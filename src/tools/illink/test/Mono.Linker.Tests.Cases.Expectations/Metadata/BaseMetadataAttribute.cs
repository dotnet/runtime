// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[Conditional ("INCLUDE_EXPECTATIONS")]
	public abstract class BaseMetadataAttribute : Attribute
	{
	}
}
