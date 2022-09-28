// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	public sealed class StripSubstitutionsAttribute : BaseMetadataAttribute
	{
		public readonly bool Value;

		public StripSubstitutionsAttribute (bool value)
		{
			Value = value;
		}
	}
}
