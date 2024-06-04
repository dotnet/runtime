using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
	public class DebuggerDisplayAttributeOnGenerics
	{
		public static void Main ()
		{
			_ = new GenericDerivedWithField<TestType> ();
			_ = new GenericDerivedWithProperty<TestType> ();
		}

		[KeptMember (".ctor()")]
		class GenericBase<T>
		{
			[Kept]
			public T FieldOnBase;

			[Kept]
			[KeptBackingField]
			public T PropertyOnBase { [Kept] get; [Kept] set; }

			public void MethodOnBase () { }
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (GenericBase<>), "T")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		[DebuggerDisplay ("F = {FieldOnBase}")]
		class GenericDerivedWithField<T> : GenericBase<T>
		{
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (GenericBase<>), "T")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		[DebuggerDisplay ("P = {PropertyOnBase}")]
		class GenericDerivedWithProperty<T> : GenericBase<T>
		{
		}

		[Kept]
		class TestType
		{
		}
	}
}