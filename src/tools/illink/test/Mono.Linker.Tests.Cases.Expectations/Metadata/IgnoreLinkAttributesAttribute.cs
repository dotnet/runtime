﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	public sealed class IgnoreLinkAttributesAttribute : BaseMetadataAttribute
	{
		public readonly bool Value;

		public IgnoreLinkAttributesAttribute (bool value)
		{
			Value = value;
		}
	}
}
