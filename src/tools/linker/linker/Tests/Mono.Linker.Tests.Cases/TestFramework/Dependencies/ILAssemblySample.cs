using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TestFramework.Dependencies {
	/// <summary>
	/// Note : This file is not used during the test run itself.
	/// 
	/// The file has been left here for two reasons
	/// 
	/// 1) To keep the test that will use the class happy when compiling inside VS.  (We could use #if's to get around this, but..see #2)
	/// 2) Since this is meant to be one of the trivial examples, by keeping the source we can easily regenerate the .il version if anything
	/// ever needed to be refactored
	/// </summary>
	[NotATestCase]
	public class ILAssemblySample {
		public string GiveMeAValue ()
		{
			return "Bar";
		}
	}
}
