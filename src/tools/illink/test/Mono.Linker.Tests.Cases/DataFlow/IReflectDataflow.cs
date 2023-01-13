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
	[ExpectedNoWarnings]
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
			ReflectOverType.Test ();
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
		[KeptMember (".ctor()")]
		class MyReflect : IReflect
		{
			[Kept]
			public Type UnderlyingSystemType { [Kept] get => throw new NotImplementedException (); }
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
			public FieldInfo GetField (string name, BindingFlags bindingAttr) => throw new NotImplementedException ();
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
			public FieldInfo[] GetFields (BindingFlags bindingAttr) => throw new NotImplementedException ();
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers ((DynamicallyAccessedMemberTypes) 8191)]
			public MemberInfo[] GetMember (string name, BindingFlags bindingAttr) => throw new NotImplementedException ();
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers ((DynamicallyAccessedMemberTypes) 8191)]
			public MemberInfo[] GetMembers (BindingFlags bindingAttr) => throw new NotImplementedException ();
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public MethodInfo GetMethod (string name, BindingFlags bindingAttr) => throw new NotImplementedException ();
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public MethodInfo GetMethod (string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException ();
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public MethodInfo[] GetMethods (BindingFlags bindingAttr) => throw new NotImplementedException ();
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
			public PropertyInfo[] GetProperties (BindingFlags bindingAttr) => throw new NotImplementedException ();
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
			public PropertyInfo GetProperty (string name, BindingFlags bindingAttr) => throw new NotImplementedException ();
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
			public PropertyInfo GetProperty (string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException ();
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			public object InvokeMember (string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) => throw new NotImplementedException ();
		}

		[Kept]
		[KeptBaseType (typeof (MyReflect))]
		class MyReflectDerived : MyReflect
		{
		}

		// This is effectively an E2E test for a situation encountered in https://github.com/dotnet/winforms/blob/main/src/System.Windows.Forms/src/System/Windows/Forms/HtmlToClrEventProxy.cs
		// Validates that by using IReflect there's no escaping the annotations system.
		[Kept]
		class ReflectOverType
		{
			[Kept]
			[KeptBaseType (typeof (MyReflect))]
			class MyReflectOverType : MyReflect
			{
				[Kept]
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
				Type _underlyingType;
				[Kept]
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
				IReflect _underlyingReflect;

				[Kept]
				public MyReflectOverType (
					[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
					Type type)
				{
					_underlyingType = type;
					_underlyingReflect = _underlyingType as IReflect;
				}

				[Kept]
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
				public FieldInfo[] GetFields (BindingFlags bindingAttr) => _underlyingReflect.GetFields (bindingAttr);
			}

			[Kept]
			class TestType
			{
				[Kept]
				public int Field;
			}

			[Kept]
			public static void Test ()
			{
				new MyReflectOverType (typeof (TestType)).GetFields (BindingFlags.Instance | BindingFlags.Public);
			}
		}
	}
}
