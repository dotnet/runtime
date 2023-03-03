using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies
{
	public class AnotherLibrary<T>
	{
		public string Prop { get; set; }
	}
}
