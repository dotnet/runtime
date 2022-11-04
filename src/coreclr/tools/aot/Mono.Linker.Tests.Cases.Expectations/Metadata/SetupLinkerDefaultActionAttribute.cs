// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public class SetupLinkerDefaultActionAttribute : BaseMetadataAttribute
	{
		public SetupLinkerDefaultActionAttribute (string action)
		{
			if (string.IsNullOrEmpty (action))
				throw new ArgumentNullException (nameof (action));
		}
	}
}
