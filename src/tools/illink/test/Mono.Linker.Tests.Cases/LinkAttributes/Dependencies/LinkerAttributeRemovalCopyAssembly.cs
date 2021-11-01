// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Mono.Linker.Tests.Cases.LinkAttributes.Dependencies;

[assembly: TestAnotherAttributeUsedFromCopyAssembly]

namespace Mono.Linker.Tests.Cases.LinkAttributes.Dependencies
{
	[TestAttributeUsedFromCopyAssemblyAttribute (TestAttributeUsedFromCopyAssemblyEnum.None)]
	[EditorBrowsable (EditorBrowsableState.Never)]
	public class TypeOnCopyAssemblyWithAttributeUsage
	{
		public TypeOnCopyAssemblyWithAttributeUsage ()
		{
			typeof (TestAttributeReferencedAsTypeFromCopyAssemblyAttribute).ToString ();
		}
	}
}
