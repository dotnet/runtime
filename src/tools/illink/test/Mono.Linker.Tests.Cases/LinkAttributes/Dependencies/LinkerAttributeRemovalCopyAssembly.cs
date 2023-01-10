// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
