using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[SetupLinkerAction ("copy", "test")]
	[SetupCompileBefore ("linked.dll", new[] { typeof (WithLinked_Attrs) })]

	[KeptMember (".ctor()")]
	[KeptMemberInAssembly ("linked.dll", typeof (WithLinked_Attrs.TypeAttribute), ".ctor()")]
	[KeptMemberInAssembly ("linked.dll", typeof (WithLinked_Attrs.FieldAttribute), ".ctor()")]
	[KeptMemberInAssembly ("linked.dll", typeof (WithLinked_Attrs.PropertyAttribute), ".ctor()")]
	[KeptMemberInAssembly ("linked.dll", typeof (WithLinked_Attrs.EventAttribute), ".ctor()")]
	[KeptMemberInAssembly ("linked.dll", typeof (WithLinked_Attrs.MethodAttribute), ".ctor()")]
	[KeptMemberInAssembly ("linked.dll", typeof (WithLinked_Attrs.ParameterAttribute), ".ctor()")]
	[KeptTypeInAssembly ("linked.dll", typeof (WithLinked_Attrs.FooEnum))]
	[KeptTypeInAssembly ("linked.dll", typeof (WithLinked_Attrs.MethodWithEnumValueAttribute))]
	public class CopyWithLinkedWillHaveAttributeDepsKept
	{
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (WithLinked_Attrs.TypeAttribute))]
		[WithLinked_Attrs.Type]
		class Foo
		{
			[Kept]
			[KeptAttributeAttribute (typeof (WithLinked_Attrs.FieldAttribute))]
			[WithLinked_Attrs.Field]
			private static int Field;

			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (WithLinked_Attrs.PropertyAttribute))]
			[WithLinked_Attrs.Property]
			private static int Property {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[KeptAttributeAttribute (typeof (WithLinked_Attrs.EventAttribute))]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[WithLinked_Attrs.Event]
			public event EventHandler Event;

			[Kept]
			[KeptAttributeAttribute (typeof (WithLinked_Attrs.MethodAttribute))]
			[WithLinked_Attrs.Method]
			static void Method ()
			{
			}

			[Kept]
			[KeptAttributeAttribute (typeof (WithLinked_Attrs.MethodWithEnumValueAttribute))]
			[WithLinked_Attrs.MethodWithEnumValue (WithLinked_Attrs.FooEnum.Three, typeof (WithLinked_Attrs))]
			static void MethodWithEnum ()
			{
			}

			[Kept]
			static void MethodWithParameter ([KeptAttributeAttribute (typeof (WithLinked_Attrs.ParameterAttribute))][WithLinked_Attrs.Parameter] int arg)
			{
			}
		}
	}
}