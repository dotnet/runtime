using System;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies
{
	public class AttributeDefinedAndUsedInOtherAssemblyIsKept_Lib
	{
		public static void UseTheAttributeType ()
		{
			var str = typeof (FooAttribute).ToString ();
		}

		public class FooAttribute : Attribute
		{
		}
	}
}
