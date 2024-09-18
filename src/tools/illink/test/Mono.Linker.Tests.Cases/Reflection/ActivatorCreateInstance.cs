using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	[KeptMember (".ctor()")]
	[KeptMember (".cctor()")]
	public class ActivatorCreateInstance
	{
		public static void Main ()
		{
			Activator.CreateInstance (typeof (Test1));
			Activator.CreateInstance (typeof (Test2), true);
			Activator.CreateInstance (typeof (Test3), BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
			Activator.CreateInstance (typeof (Test4), GetBindingFlags (), null, GetArgs (), null);
			Activator.CreateInstance (typeof (Test5), new object[] { 1, "ss" });

			var p = new ActivatorCreateInstance ();

			// Direct call to static method with a parameter
			FromParameterOnStaticMethod (typeof (FromParameterOnStaticMethodType));

			// Value comes from multiple sources - each must be marked appropriately
			Type fromParameterOnStaticMethodType = p == null ?
				typeof (FromParameterOnStaticMethodTypeA) :
				typeof (FromParameterOnStaticMethodTypeB);
			FromParameterOnStaticMethod (fromParameterOnStaticMethodType);

			// Direct call to instance method with a parameter
			p.FromParameterOnInstanceMethod (typeof (FromParameterOnInstanceMethodType));

			// Non-public constructors required
			FromParameterWithNonPublicConstructors (typeof (FromParameterWithNonPublicConstructorsType));

			// Public contructors required
			FromParameterWithPublicConstructors (typeof (FromParameterWithPublicConstructorsType));

			WithAssemblyName ();
			WithAssemblyPath ();

			AppDomainCreateInstance ();

			UnsupportedCreateInstance ();

			TestCreateInstanceOfTWithConcreteType ();
			TestCreateInstanceOfTWithNewConstraint<TestCreateInstanceOfTWithNewConstraintType> ();
			TestCreateInstanceOfTWithNoConstraint<TestCreateInstanceOfTWithNoConstraintType> ();

			TestCreateInstanceOfTWithDataflowType.Test ();

			TestNullArgsOnKnownType ();
			TestNullArgsOnAnnotatedType (typeof (TestType));
			TestNullArgsNonPublicOnly (typeof (TestType));
			TestNullArgsNonPublicWithNonPublicAnnotation (typeof (TestType));

			TestNullType ();
			TestNoValue ();

			CreateInstanceWithGetTypeFromHierarchy.Test ();
		}

		[Kept]
		class Test1
		{
			[Kept]
			public Test1 ()
			{
			}

			public Test1 (int arg)
			{
			}
		}

		[Kept]
		class Test2
		{
			[Kept]
			private Test2 ()
			{
			}

			public Test2 (int arg)
			{
			}
		}

		[Kept]
		class Test3
		{
			[Kept]
			private Test3 ()
			{
			}

			public Test3 (int arg)
			{
			}
		}

		[Kept]
		class Test4
		{
			[Kept]
			public Test4 (int i, object o)
			{
			}

			[Kept]
			private Test4 (char b)
			{
			}

			[Kept]
			internal Test4 (int arg)
			{
			}

			[Kept]
			static Test4 ()
			{
			}
		}

		[Kept]
		class Test5
		{
			[Kept]
			public Test5 (int i, object o)
			{
			}
		}

		[Kept]
		private static void FromParameterOnStaticMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			Activator.CreateInstance (type);
		}

		[Kept]
		class FromParameterOnStaticMethodType
		{
			[Kept]
			public FromParameterOnStaticMethodType () { }
			public FromParameterOnStaticMethodType (int arg) { }
		}

		[Kept]
		class FromParameterOnStaticMethodTypeA
		{
			[Kept]
			public FromParameterOnStaticMethodTypeA () { }
			public FromParameterOnStaticMethodTypeA (int arg) { }
		}

		[Kept]
		class FromParameterOnStaticMethodTypeB
		{
			[Kept]
			public FromParameterOnStaticMethodTypeB () { }
			public FromParameterOnStaticMethodTypeB (int arg) { }
		}

		// Small formatting difference
		[ExpectedWarning ("IL2067", nameof (Activator) + "." + nameof (Activator.CreateInstance) + "(Type, Object[])", Tool.Trimmer, "")]
		[ExpectedWarning ("IL2067", nameof (Activator) + "." + nameof (Activator.CreateInstance) + "(Type, params Object[])", Tool.Analyzer, "")]
		[ExpectedWarning ("IL2067", nameof (Activator) + "." + nameof (Activator.CreateInstance), nameof (CultureInfo))]
		[Kept]
		private void FromParameterOnInstanceMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			// Only default ctor is available from the parameter, so only the first one should work
			Activator.CreateInstance (type);
			Activator.CreateInstance (type, new object[] { 1 });
			Activator.CreateInstance (type, BindingFlags.NonPublic, null, new object[] { 1, 2 }, null);
		}

		[Kept]
		class FromParameterOnInstanceMethodType
		{
			[Kept]
			public FromParameterOnInstanceMethodType () { }
			public FromParameterOnInstanceMethodType (int arg) { }
			public FromParameterOnInstanceMethodType (int arg, int arg2) { }
		}

		[ExpectedWarning ("IL2067", nameof (Activator) + "." + nameof (Activator.CreateInstance) + "(Type)")]
		// Small formatting difference
		[ExpectedWarning ("IL2067", nameof (Activator) + "." + nameof (Activator.CreateInstance) + "(Type, Object[])", Tool.Trimmer, "")]
		[ExpectedWarning ("IL2067", nameof (Activator) + "." + nameof (Activator.CreateInstance) + "(Type, params Object[])", Tool.Analyzer, "")]
		[Kept]
		private static void FromParameterWithNonPublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)

		{
			// Only the explicitly non-public call will work
			Activator.CreateInstance (type);
			Activator.CreateInstance (type, new object[] { 1 });
			Activator.CreateInstance (type, BindingFlags.NonPublic, null, new object[] { 1, 2 }, null);
		}

		[Kept]
		class FromParameterWithNonPublicConstructorsType
		{
			public FromParameterWithNonPublicConstructorsType () { }
			public FromParameterWithNonPublicConstructorsType (int arg) { }
			[Kept]
			private FromParameterWithNonPublicConstructorsType (int arg, int arg2) { }
		}

		[ExpectedWarning ("IL2067", nameof (Activator) + "." + nameof (Activator.CreateInstance))]
		[Kept]
		private static void FromParameterWithPublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			// Only public ctors and default ctor are required, so only those cases should work
			Activator.CreateInstance (type);
			Activator.CreateInstance (type, new object[] { 1 });
			Activator.CreateInstance (type, BindingFlags.NonPublic, null, new object[] { 1, 2 }, null);
		}

		[Kept]
		class FromParameterWithPublicConstructorsType
		{
			[Kept]
			public FromParameterWithPublicConstructorsType () { }
			[Kept]
			public FromParameterWithPublicConstructorsType (int arg) { }
			private FromParameterWithPublicConstructorsType (int arg, int arg2) { }
		}

		[Kept]
		class WithAssemblyNameParameterless1
		{
			[Kept]
			public WithAssemblyNameParameterless1 ()
			{
			}

			public WithAssemblyNameParameterless1 (int i, object o)
			{
			}
		}

		[Kept]
		class WithAssemblyNameParameterless2
		{
			[Kept]
			public WithAssemblyNameParameterless2 ()
			{
			}

			public WithAssemblyNameParameterless2 (int i, object o)
			{
			}
		}

		[Kept]
		class WithAssemblyNamePublicOnly
		{
			[Kept]
			public WithAssemblyNamePublicOnly ()
			{
			}

			[Kept]
			public WithAssemblyNamePublicOnly (int i, object o)
			{
			}

			private WithAssemblyNamePublicOnly (int i, object o, int j)
			{
			}
		}

		[Kept]
		class WithAssemblyNamePrivateOnly
		{
			public WithAssemblyNamePrivateOnly ()
			{
			}

			[Kept]
			private WithAssemblyNamePrivateOnly (int i, object o, int j)
			{
			}
		}

		[Kept]
		private static BindingFlags GetBindingFlags ()
		{
			return BindingFlags.Public;
		}

		[Kept]
		private static object[] GetArgs ()
		{
			return null;
		}

		[Kept]
		private static void WithAssemblyName ()
		{
			Activator.CreateInstance ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyNameParameterless1");
			Activator.CreateInstance ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyNameParameterless2", new object[] { });
			Activator.CreateInstance ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyNamePublicOnly", false, BindingFlags.Public, null, new object[] { }, null, new object[] { });
			Activator.CreateInstance ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyNamePrivateOnly", false, BindingFlags.NonPublic, null, new object[] { }, null, new object[] { });

			WithNullAssemblyName ();
			WithEmptyAssemblyName ();
			WithNoValueAssemblyName ();
			WithAssemblyAndNullTypeName ();
			WithAssemblyAndEmptyTypeName ();
			WithAssemblyAndNoValueTypeName ();
			WithNonExistingAssemblyName ();
			WithAssemblyAndUnknownTypeName ();
			WithAssemblyAndNonExistingTypeName ();
		}

		[Kept]
		[ExpectedWarning ("IL2032", nameof (Activator) + "." + nameof (Activator.CreateInstance), "assemblyName")]
		private static void WithNullAssemblyName ()
		{
			Activator.CreateInstance (null, "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyNameParameterless1");
		}

		[Kept]
		private static void WithEmptyAssemblyName ()
		{
			Activator.CreateInstance (string.Empty, "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyNameParameterless1");
		}

		[Kept]
		private static void WithNoValueAssemblyName ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			Activator.CreateInstance (noValue, "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyNameParameterless1");
		}

		[Kept]
		private static void WithAssemblyAndNullTypeName ()
		{
			Activator.CreateInstance ("test", null);
		}

		[Kept]
		private static void WithAssemblyAndEmptyTypeName ()
		{
			Activator.CreateInstance ("test", string.Empty);
		}

		[Kept]
		private static void WithAssemblyAndNoValueTypeName ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			Activator.CreateInstance ("test", noValue);
		}

		[Kept]
		// Analyzer doesn't handle assembly resolution
		[ExpectedWarning ("IL2061", nameof (Activator) + "." + nameof (Activator.CreateInstance), "NonExistingAssembly", Tool.Trimmer, "")]
		private static void WithNonExistingAssemblyName ()
		{
			Activator.CreateInstance ("NonExistingAssembly", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyNameParameterless1");
		}

		[Kept]
		private static string _typeNameField = "Unknown";

		[Kept]
		[ExpectedWarning ("IL2032", nameof (Activator) + "." + nameof (Activator.CreateInstance), "typeName")]
		private static void WithAssemblyAndUnknownTypeName ()
		{
			Activator.CreateInstance ("test", _typeNameField, new object[] { });
		}

		[Kept]
		private static void WithAssemblyAndNonExistingTypeName ()
		{
			Activator.CreateInstance ("test", "NonExistingType", new object[] { });
		}

		[Kept]
		class WithAssemblyPathParameterless
		{
			[Kept]
			public WithAssemblyPathParameterless ()
			{
			}

			public WithAssemblyPathParameterless (int i, object o)
			{
			}
		}

		[Kept]
		private static void WithAssemblyPath ()
		{
			Activator.CreateInstanceFrom ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyPathParameterless");

			WithNullAssemblyPath ();
			WithEmptyAssemblyPath ();
			WithNoValueAssemblyPath ();
			WithAssemblyPathAndNullTypeName ();
			WithAssemblyPathAndEmptyTypeName ();
			WithAssemblyPathAndNoValueTypeName ();
		}

		[Kept]
		// Technically this shouldn't warn because CreateInstanceFrom throws if the assembly file path is null.
		// However, our implementation is the same for CreateInstance and CreateInstanceFrom.
		[ExpectedWarning ("IL2032", nameof (Activator) + "." + nameof (Activator.CreateInstance), "assemblyFile")]
		private static void WithNullAssemblyPath ()
		{
			Activator.CreateInstanceFrom (null, "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyPathParameterless");
		}

		[Kept]
		private static void WithEmptyAssemblyPath ()
		{
			Activator.CreateInstanceFrom (string.Empty, "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyPathParameterless");
		}

		[Kept]
		private static void WithNoValueAssemblyPath ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			Activator.CreateInstanceFrom (noValue, "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+WithAssemblyPathParameterless");
		}

		[Kept]
		private static void WithAssemblyPathAndNullTypeName ()
		{
			Activator.CreateInstanceFrom ("test", null);
		}

		[Kept]
		private static void WithAssemblyPathAndEmptyTypeName ()
		{
			Activator.CreateInstanceFrom ("test", string.Empty);
		}

		[Kept]
		private static void WithAssemblyPathAndNoValueTypeName ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			Activator.CreateInstanceFrom ("test", noValue);
		}

		[Kept]
		class AppDomainCreateInstanceType
		{
			[Kept]
			public AppDomainCreateInstanceType ()
			{
			}
		}

		[Kept]
		private static void AppDomainCreateInstance ()
		{
			// Just a basic test that these are all recognized, we're not testing that it marks correctly as it has the exact same implementation
			// as the above tested Activator.CreateInstance overloads
			AppDomain.CurrentDomain.CreateInstance ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType");
			AppDomain.CurrentDomain.CreateInstance ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType", new object[] { });
			AppDomain.CurrentDomain.CreateInstance ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType", false, BindingFlags.Public, null, null, null, null);

			AppDomain.CurrentDomain.CreateInstanceAndUnwrap ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType");
			AppDomain.CurrentDomain.CreateInstanceAndUnwrap ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType", new object[] { });
			AppDomain.CurrentDomain.CreateInstanceAndUnwrap ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType", false, BindingFlags.Public, null, null, null, null);

			AppDomain.CurrentDomain.CreateInstanceFrom ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType");
			AppDomain.CurrentDomain.CreateInstanceFrom ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType", new object[] { });
			AppDomain.CurrentDomain.CreateInstanceFrom ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType", false, BindingFlags.Public, null, null, null, null);

			AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType");
			AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType", new object[] { });
			AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+AppDomainCreateInstanceType", false, BindingFlags.Public, null, null, null, null);
		}

		[Kept]
		[ExpectedWarning ("IL2058", nameof (Assembly) + "." + nameof (Assembly.CreateInstance) + "(String)")]
		[ExpectedWarning ("IL2058", nameof (Assembly) + "." + nameof (Assembly.CreateInstance) + "(String, Boolean)")]
		[ExpectedWarning ("IL2058", nameof (Assembly) + "." + nameof (Assembly.CreateInstance), nameof (BindingFlags))]
		private static void UnsupportedCreateInstance ()
		{
			typeof (ActivatorCreateInstance).Assembly.CreateInstance ("NonExistent");
			typeof (ActivatorCreateInstance).Assembly.CreateInstance ("NonExistent", ignoreCase: false);
			typeof (ActivatorCreateInstance).Assembly.CreateInstance ("NonExistent", false, BindingFlags.Public, null, new object[] { }, null, new object[] { });
		}

		[Kept]
		class TestCreateInstanceOfTWithConcreteTypeType
		{
			[Kept]
			public TestCreateInstanceOfTWithConcreteTypeType ()
			{
			}

			public TestCreateInstanceOfTWithConcreteTypeType (int i)
			{
			}
		}

		[Kept]
		private static void TestCreateInstanceOfTWithConcreteType ()
		{
			Activator.CreateInstance<TestCreateInstanceOfTWithConcreteTypeType> ();
		}

		[Kept]
		class TestCreateInstanceOfTWithNewConstraintType
		{
			[Kept]
			public TestCreateInstanceOfTWithNewConstraintType ()
			{
			}

			public TestCreateInstanceOfTWithNewConstraintType (int i)
			{
			}
		}

		[Kept]
		private static void TestCreateInstanceOfTWithNewConstraint<
			[KeptGenericParamAttributes (GenericParameterAttributes.DefaultConstructorConstraint)]
			T
		> () where T : new()
		{
			Activator.CreateInstance<T> ();
		}

		[Kept]
		class TestCreateInstanceOfTWithNoConstraintType
		{
			public TestCreateInstanceOfTWithNoConstraintType ()
			{
			}

			public TestCreateInstanceOfTWithNoConstraintType (int i)
			{
			}
		}

		[Kept]
		[ExpectedWarning ("IL2091", nameof (Activator), nameof (Activator.CreateInstance) + "<T>")]
		private static void TestCreateInstanceOfTWithNoConstraint<T> ()
		{
			Activator.CreateInstance<T> ();
		}

		[Kept]
		class TestCreateInstanceOfTWithDataflowType
		{
			[Kept]
			public TestCreateInstanceOfTWithDataflowType ()
			{
			}

			public TestCreateInstanceOfTWithDataflowType (int i)
			{
			}

			[Kept]
			static void TestWithMatchingDataFlow<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor),
				KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))] T> ()
			{
				Activator.CreateInstance<T> ();
			}

			[Kept]
			[ExpectedWarning ("IL2091")]
			static void TestWithMismatchDataFlow<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields),
				KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))] T> ()
			{
				Activator.CreateInstance<T> ();
			}

			[Kept]
			public static void Test ()
			{
				TestWithMatchingDataFlow<TestCreateInstanceOfTWithDataflowType> ();
				TestWithMismatchDataFlow<TestCreateInstanceOfTWithDataflowType> ();
			}
		}

		[Kept]
		class TestNullArgsType
		{
			[Kept]
			public TestNullArgsType () { }

			public TestNullArgsType (int i) { }
		}

		[Kept]
		private static void TestNullArgsOnKnownType ()
		{
			Activator.CreateInstance (typeof (TestNullArgsType), null);
		}

		[Kept]
		private static void TestNullArgsOnAnnotatedType (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor),
			KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))] Type type)
		{
			Activator.CreateInstance (type, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
		}

		[Kept]
		[ExpectedWarning ("IL2067", nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicConstructors))]
		private static void TestNullArgsNonPublicOnly (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor),
			KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))] Type type)
		{
			Activator.CreateInstance (type, BindingFlags.NonPublic | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
		}

		[Kept]
		[ExpectedNoWarnings]
		private static void TestNullArgsNonPublicWithNonPublicAnnotation (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicConstructors),
			KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))] Type type)
		{
			Activator.CreateInstance (type, nonPublic: true);
		}

		[Kept]
		private static void TestNullType ()
		{
			Type t = null;
			Activator.CreateInstance (t);
		}

		[Kept]
		private static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			Activator.CreateInstance (noValue);
		}

		[Kept]
		class CreateInstanceWithGetTypeFromHierarchy
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[KeptMember (".ctor()")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			class AnnotatedBase
			{
				[Kept]
				[ExpectedNoWarnings]
				public void TestCreateInstance ()
				{
					Activator.CreateInstance (GetType (), BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture, null);
				}
			}

			[Kept]
			[KeptBaseType (typeof (AnnotatedBase))]
			[KeptMember (".ctor()")]
			class Derived : AnnotatedBase
			{
				[Kept]
				public static void KeepIt () { }
			}

			[Kept]
			public static void Test ()
			{
				Derived.KeepIt ();
				(new AnnotatedBase ()).TestCreateInstance ();
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		class TestType { }
	}
}
