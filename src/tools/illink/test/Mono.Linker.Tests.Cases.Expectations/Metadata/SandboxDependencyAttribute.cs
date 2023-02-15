// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SandboxDependencyAttribute : BaseMetadataAttribute
	{

		public SandboxDependencyAttribute (string relativePathToFile, string destinationFileName = null)
		{
			if (string.IsNullOrEmpty (relativePathToFile))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (relativePathToFile));
		}

		public SandboxDependencyAttribute (Type typeOfSourceFileToInclude, string destinationFileName = null)
		{
			if (typeOfSourceFileToInclude == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (typeOfSourceFileToInclude));
		}
	}
}