using System;

namespace Mono.Linker.Tests.Cases.References.Dependencies
{
	public class UserAssembliesAreLinkedByDefault_Library1
	{
		public void UsedMethod ()
		{
			Console.WriteLine ("Used");
		}

		public void UnusedMethod ()
		{
			Console.WriteLine ("NotUsed");
		}
	}
}
