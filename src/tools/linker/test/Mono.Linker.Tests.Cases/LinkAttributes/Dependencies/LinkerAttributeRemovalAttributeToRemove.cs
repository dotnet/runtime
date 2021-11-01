// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
