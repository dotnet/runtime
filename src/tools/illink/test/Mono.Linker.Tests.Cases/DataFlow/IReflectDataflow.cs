using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Hits what appears to be a bug in the tool
	// Could not initialize vtable of class(0x02000007) .MyReflect due to VTable setup of type Mono.Linker.Tests.Cases.DataFlow.IReflectDataflow+MyReflect failed assembly:/tmp/linker_tests/output/test.exe type:MyReflect member:(null)
	[SkipPeVerify]
	class IReflectDataflow
	{
		[ExpectBodyModified]
		public static void Main ()
		{
			// The cast here fails at runtime, but that's okay, we just want something to flow here.
			// Casts are transparent to the dataflow analysis and preserve the tracked value.
			RequirePublicParameterlessConstructor ((object) typeof (C1) as MyReflect);
			s_requirePublicNestedTypes = ((object) typeof (C2)) as MyReflectDerived;
			RequirePrivateMethods (typeof (C3));
		}

		[Kept]
		class C1
		{
			[Kept]
			public C1 () { }
			public C1 (string s) { }
		}

		[Kept]
		class C2
		{
			public C2 () { }

			[Kept]
			[KeptMember (".ctor()")]
			public class Nested { }
		}

		[Kept]
		class C3
		{
			[Kept]
			static void Foo () { }
			public static void Bar () { }
		}

		[Kept]
		static void RequirePublicParameterlessConstructor ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute)), DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] MyReflect mine)
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)]
		static MyReflectDerived s_requirePublicNestedTypes;

		[Kept]
		static void RequirePrivateMethods ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute)), DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] IReflect m)
		{
		}

		[Kept]
		[KeptInterface (typeof (IReflect))]
		class MyReflect : IReflect
		{
			public Type UnderlyingSystemType => throw new NotImplementedException ();
			public FieldInfo GetField (string name, BindingFlags bindingAttr) => throw new NotImplementedException ();
			public FieldInfo[] GetFields (BindingFlags bindingAttr) => throw new NotImplementedException ();
			public MemberInfo[] GetMember (string name, BindingFlags bindingAttr) => throw new NotImplementedException ();
			public MemberInfo[] GetMembers (BindingFlags bindingAttr) => throw new NotImplementedException ();
			public MethodInfo GetMethod (string name, BindingFlags bindingAttr) => throw new NotImplementedException ();
			public MethodInfo GetMethod (string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException ();
			public MethodInfo[] GetMethods (BindingFlags bindingAttr) => throw new NotImplementedException ();
			public PropertyInfo[] GetProperties (BindingFlags bindingAttr) => throw new NotImplementedException ();
			public PropertyInfo GetProperty (string name, BindingFlags bindingAttr) => throw new NotImplementedException ();
			public PropertyInfo GetProperty (string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException ();
			public object InvokeMember (string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) => throw new NotImplementedException ();
		}

		[Kept]
		[KeptBaseType (typeof (MyReflect))]
		class MyReflectDerived : MyReflect
		{
		}
	}
}
