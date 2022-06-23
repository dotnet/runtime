// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupLinkerActionAttribute : BaseMetadataAttribute
	{
		public SetupLinkerActionAttribute (string action, string assembly)
		{
			if (string.IsNullOrEmpty (action))
				throw new ArgumentNullException (nameof (action));
		}
	}
}
