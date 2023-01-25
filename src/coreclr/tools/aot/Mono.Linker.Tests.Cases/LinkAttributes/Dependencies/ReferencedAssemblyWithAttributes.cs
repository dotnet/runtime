using System;
using Mono.Linker.Tests.Cases.LinkAttributes.Dependencies;

[assembly: TestRemove]
[assembly: TestDontRemove]

namespace Mono.Linker.Tests.Cases.LinkAttributes.Dependencies
{
	public class ReferencedAssemblyWithAttributes
	{
	}
}
