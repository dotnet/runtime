// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.Linker.Tests.Cases.LinkAttributes.Dependencies
{
	public enum TestAttributeUsedFromCopyAssemblyEnum
	{
		None
	}

	public class TestAttributeUsedFromCopyAssemblyAttribute : Attribute
	{
		public TestAttributeUsedFromCopyAssemblyAttribute (TestAttributeUsedFromCopyAssemblyEnum n)
		{
		}
	}

	public class TestAnotherAttributeUsedFromCopyAssemblyAttribute : Attribute
	{
		public TestAnotherAttributeUsedFromCopyAssemblyAttribute ()
		{
		}
	}

	public class TestAttributeReferencedAsTypeFromCopyAssemblyAttribute : Attribute
	{
	}
}
