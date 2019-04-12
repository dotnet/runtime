using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml.FeatureExclude { 
	[SetupLinkerArgument ("--exclude-feature", "one")]
	public class OnProperty {
		public static void Main ()
		{
			new Foo (); // Used to avoid lazy body marking
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Foo { 
			public int FeatureOne { get; set; }
			
			[Kept] 
			[KeptBackingField] 
			public int FeatureTwo {[Kept] get; [Kept] set; }
		}
	}
}
