using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Mono.Linker.Tests.Cases.DataFlow.Dependencies;

[assembly: TestAnotherAttributeUsedFromCopyAssembly]

namespace Mono.Linker.Tests.Cases.DataFlow.Dependencies
{
	[TestAttributeUsedFromCopyAssemblyAttribute (TestAttributeUsedFromCopyAssemblyEnum.None)]
	[EditorBrowsable (EditorBrowsableState.Never)]
	public class TypeOnCopyAssemblyWithAttributeUsage
	{
	}
}
