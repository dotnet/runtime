using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[KeptMember (".cctor()")]
	[ExpectedNoWarnings ()]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("EscapedTypeNames.dll", new[] { "Dependencies/EscapedTypeNames.il" })]
	[SetupCompileBefore ("RequireHelper.dll", new[] { "Dependencies/RequireHelper.cs" })]
	[KeptTypeInAssembly ("EscapedTypeNames", "Library.Not\\+Nested")]
	[KeptTypeInAssembly ("EscapedTypeNames", "Library.Not\\+Nested+Nes\\\\ted")]
	[KeptTypeInAssembly ("EscapedTypeNames", "Library.Not\\+Nested+Nes/ted")]
	[RemovedTypeInAssembly ("RequireHelper", typeof (TypeDefinedInSameAssemblyAsGetType))]
	[KeptDelegateCacheField ("0", nameof (AssemblyResolver))]
	[KeptDelegateCacheField ("1", nameof (GetTypeFromAssembly))]
	public class TypeUsedViaReflection
	{
		public static void Main ()
		{
			TestNull ();
			TestEmptyString ();
			TestFullString ();
			TestGenericString ();
			TestFullStringConst ();
			TestTypeAsmName ();
			TestType ();
			TestPointer ();
			TestReference ();
			TestArray ();
			TestArrayOfArray ();
			TestGenericInstantiation ();
			TestGenericInstantiationFullString ();
			TestGenericInstantiationOverCoreLib ();
			TestMultiDimensionalArray ();
			TestMultiDimensionalArrayFullString ();
			TestMultiDimensionalArrayAsmName ();
			TestDeeplyNested ();
			TestTypeOf ();
			TestTypeFromBranch (3);
			TestTypeUsingCaseInsensitiveFlag ();
			TestTypeUsingCaseUnknownByTheLinker ();
			TestTypeUsingCaseUnknownByTheLinker2 ();
			TestTypeOverloadWith3Parameters ();
			TestTypeOverloadWith4Parameters ();
			TestTypeOverloadWith5ParametersWithIgnoreCase ();
			TestTypeOverloadWith5ParametersWithoutIgnoreCase ();
			TestInvalidTypeName ();
			TestUnknownIgnoreCase3Params (1);
			TestUnknownIgnoreCase5Params (1);
			TestGenericTypeWithAnnotations ();

			BaseTypeInterfaces.Test ();

			TestInvalidTypeCombination ();
			TestEscapedTypeName ();
			AssemblyTypeResolutionBehavior.Test ();
		}

		[Kept]
		public static void TestNull ()
		{
			const string reflectionTypeKeptString = null;
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		public static void TestEmptyString ()
		{
			const string reflectionTypeKeptString = "";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (Full))]
		public class Full { }

		[Kept]
		[ExpectedWarning ("IL2026", nameof (Full))]
		public static void TestFullString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Full, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (Generic<T>))]
		public class Generic<T> { }

		[Kept]
		[ExpectedWarning ("IL2026", "Generic")]
		public static void TestGenericString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Generic`1, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (GenericInstantiation<T>))]
		public class GenericInstantiation<T> { }

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (GenericArgument))]
		public class GenericArgument { }

		[Kept]
		[ExpectedWarning ("IL2026", "GenericInstantiation")]
		public static void TestGenericInstantiation ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericInstantiation`1[[Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericArgument]]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (GenericInstantiationFullString<T>))]
		public class GenericInstantiationFullString<T> { }

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (GenericArgumentFullString))]
		public class GenericArgumentFullString { }

		[Kept]
		[ExpectedWarning ("IL2026", "GenericInstantiationFullString")]
		public static void TestGenericInstantiationFullString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericInstantiationFullString`1["
					+ "[Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericArgumentFullString, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]"
				+ "], test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (GenericInstantiationOverCoreLib<T>))]
		public class GenericInstantiationOverCoreLib<T> { }

		[Kept]
		[ExpectedWarning ("IL2026", "GenericInstantiationOverCoreLib", Tool.Trimmer | Tool.NativeAot, "Analyzer can't resolve type names from corelib")]
		public static void TestGenericInstantiationOverCoreLib ()
		{
			// Note: the argument type should not be assembly-qualified for this test, which is checking that
			// we can resolve non-assembly-qualified generic argument types from corelib.
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericInstantiationOverCoreLib`1[[System.String]], test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (FullConst))]
		public class FullConst { }

		[Kept]
		[ExpectedWarning ("IL2026", nameof (FullConst))]
		public static void TestFullStringConst ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+FullConst, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (TypeAsmName))]
		public class TypeAsmName { }

		[Kept]
		[ExpectedWarning ("IL2026", nameof (TypeAsmName))]
		public static void TestTypeAsmName ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeAsmName, test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (AType))]
		public class AType { }

		[Kept]
		[ExpectedWarning ("IL2026", nameof (AType))]
		public static void TestType ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AType";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (Pointer))]
		public class Pointer { }

		[Kept]
		// Applying DynamicallyAccessedMembers annotations to a pointer type doesn't keep members of the underlying type.
		public static void TestPointer ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Pointer*";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (Reference))]
		public class Reference { }

		[Kept]
		// Applying DynamicallyAccessedMembers annotations to a byref type doesn't keep members of the underlying type.
		public static void TestReference ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Reference&";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (Array))]
		public class Array { }

		[Kept]
		public static void TestArray ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Array[]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (ArrayOfArray))]
		public class ArrayOfArray { }

		[Kept]
		public static void TestArrayOfArray ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+ArrayOfArray[][]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}


		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (MultiDimensionalArray))]
		public class MultiDimensionalArray { }

		[Kept]
		public static void TestMultiDimensionalArray ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimensionalArray[,]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (MultiDimensionalArrayFullString))]
		public class MultiDimensionalArrayFullString { }

		[Kept]
		public static void TestMultiDimensionalArrayFullString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimensionalArrayFullString[,], test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (MultiDimensionalArrayAsmName))]
		public class MultiDimensionalArrayAsmName { }

		[Kept]
		public static void TestMultiDimensionalArrayAsmName ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimensionalArrayAsmName[,], test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
			RequireConstructor (typeKept);
		}

		[Kept]
		class Nested1
		{
			[Kept]
			class N2
			{
				[Kept]
				[KeptMember (".ctor()")]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (N3))]
				class N3
				{
				}
			}
		}

		[Kept]
		[ExpectedWarning ("IL2026", "N3")]
		static void TestDeeplyNested ()
		{
			var typeKept = Type.GetType ("Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Nested1+N2+N3");
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		class TypeOfToKeep { }

		[Kept]
		static void TestTypeOf ()
		{
			var typeKept = typeof (TypeOfToKeep);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (TypeFromBranchA))]
		class TypeFromBranchA { }

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (TypeFromBranchB))]
		class TypeFromBranchB { }

		[Kept]
		[ExpectedWarning ("IL2026", nameof (TypeFromBranchA))]
		[ExpectedWarning ("IL2026", nameof (TypeFromBranchB))]
		static void TestTypeFromBranch (int b)
		{
			string name = null;
			switch (b) {
			case 0:
				name = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeFromBranchA";
				break;
			case 1:
				name = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeFromBranchB";
				break;
			default:
				break;
			}

			var typeKept = Type.GetType (name);
			RequireConstructor (typeKept);
		}

		public class CaseInsensitive { }

		[Kept]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Boolean, Boolean)'")]
		static void TestTypeUsingCaseInsensitiveFlag ()
		{
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseInsensitive, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false, true);
			typeKept.RequiresPublicMethods (); // Validate that we don't track the value anymore since the above already warned about the problem
		}

		public class CaseUnknown { }

		[Kept]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Boolean, Boolean)'")]
		static void TestTypeUsingCaseUnknownByTheLinker ()
		{
			bool hideCase = GetCase ();
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseUnknown, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false, hideCase);
			RequireConstructor (typeKept);
		}

		[Kept]
		static bool GetCase ()
		{
			return false;
		}

		[Kept]
		static bool fieldHideCase = true;

		public class CaseUnknown2 { }

		[Kept]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Boolean, Boolean)'")]
		static void TestTypeUsingCaseUnknownByTheLinker2 ()
		{
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseUnknown2, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false, fieldHideCase);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (OverloadWith3Parameters))]
		public class OverloadWith3Parameters { }

		[Kept]
		[ExpectedWarning ("IL2026", nameof (OverloadWith3Parameters))]
		static void TestTypeOverloadWith3Parameters ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith3Parameters";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly);
			RequireConstructor (typeKept);
		}


		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (OverloadWith4Parameters))]
		public class OverloadWith4Parameters { }

		[Kept]
		[ExpectedWarning ("IL2026", nameof (OverloadWith4Parameters))]
		static void TestTypeOverloadWith4Parameters ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith4Parameters";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false);
			RequireConstructor (typeKept);
		}

		public class OverloadWith5ParametersWithIgnoreCase { }

		[Kept]
		// Small difference in formatting between analyzer/NativeAOT/linker
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Func<AssemblyName,Assembly>, Func<Assembly,String,Boolean,Type>, Boolean, Boolean)'", Tool.Trimmer, "")]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String,Func`2<AssemblyName,Assembly>,Func`4<Assembly,String,Boolean,Type>,Boolean,Boolean)'", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)'", Tool.Analyzer, "")]
		static void TestTypeOverloadWith5ParametersWithIgnoreCase ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith5ParametersWithIgnoreCase";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false, true);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (OverloadWith5ParametersWithIgnoreCase))]
		public class OverloadWith5ParametersWithoutIgnoreCase { }

		[Kept]
		[ExpectedWarning ("IL2026", nameof (OverloadWith5ParametersWithIgnoreCase))]
		static void TestTypeOverloadWith5ParametersWithoutIgnoreCase ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith5ParametersWithoutIgnoreCase";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false, false);
			RequireConstructor (typeKept);
		}

		/// <summary>
		/// This test verifies that if `TypeParser.ParseTypeName` hits an exception and returns null that ILLink doesn't fail
		/// </summary>
		[Kept]
		static void TestInvalidTypeName ()
		{
			var type = Type.GetType ("System.Collections.Generic.List`1[GenericClass`1[System.String]+Nested]");
		}

		[Kept]
		static Assembly AssemblyResolver (AssemblyName assemblyName)
		{
			return Assembly.Load (assemblyName);
		}

		[Kept]
		[ExpectedWarning ("IL2026", "'System.Reflection.Assembly.GetType(String, Boolean)'")]
		[ExpectedWarning ("IL2057", "'System.Type.GetType(String, Boolean)'")]
		static Type GetTypeFromAssembly (Assembly assembly, string name, bool caseSensitive)
		{
			return assembly == null ? Type.GetType (name, caseSensitive) : assembly.GetType (name, caseSensitive);
		}

		[Kept]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Boolean, Boolean)'")]
		static void TestUnknownIgnoreCase3Params (int num)
		{
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseUnknown2, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			bool unknownValue = num + 1 == 1;
			var typeKept = Type.GetType (reflectionTypeKeptString, false, unknownValue);
		}

		[Kept]
		// Small difference in formatting between analyzer/NativeAOT/linker
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Func<AssemblyName,Assembly>, Func<Assembly,String,Boolean,Type>, Boolean, Boolean)'", Tool.Trimmer, "")]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String,Func`2<AssemblyName,Assembly>,Func`4<Assembly,String,Boolean,Type>,Boolean,Boolean)'", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)'", Tool.Analyzer, "")]
		static void TestUnknownIgnoreCase5Params (int num)
		{
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseUnknown2, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			bool unknownValue = num + 1 == 1;
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false, unknownValue);
			RequireConstructor (typeKept);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode ("GenericTypeWithAnnotations_OuterType")]
		public class GenericTypeWithAnnotations_OuterType<
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicProperties)] T>
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (GenericTypeWithAnnotations_InnerType))]
		public class GenericTypeWithAnnotations_InnerType
		{
			[Kept]
			[KeptBackingField]
			private static bool PrivateProperty { [Kept] get; [Kept] set; }

			private static void PrivateMethod () { }
		}

		[Kept]
		[ExpectedWarning ("IL2026", "GenericTypeWithAnnotations_OuterType")]
		[ExpectedWarning ("IL2026", nameof (GenericTypeWithAnnotations_InnerType), "PrivateProperty.get")]
		[ExpectedWarning ("IL2026", nameof (GenericTypeWithAnnotations_InnerType), "PrivateProperty.set")]
		static void TestGenericTypeWithAnnotations ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericTypeWithAnnotations_OuterType`1["
					+ "[Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericTypeWithAnnotations_InnerType, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]"
				+ "], test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString);
			RequireConstructor (typeKept);
		}

		[Kept]
		class BaseTypeInterfaces
		{
			[Kept]
			interface ITest
			{
				[Kept (By = Tool.Trimmer)]
				void Method ();
			}

			[Kept]
			[KeptInterface (typeof (ITest))]
			class BaseType : ITest
			{
				[Kept]
				public void Method () { }
			}

			[Kept]
			[KeptBaseType (typeof (BaseType))]
			[KeptInterface (typeof (ITest))]
			class DerivedType : BaseType, ITest
			{
				[Kept]
				public void Method () { }
			}

			[Kept]
			public static void Test ()
			{
				ITest t = null;
				t.Method ();
				typeof (DerivedType).GetInterfaces ();
			}
		}

		[Kept]
		static void TestInvalidTypeCombination ()
		{
			try {
				// It's invalid to create an array of Span
				// This should throw at runtime, but should not warn nor fail the compilation
				Console.WriteLine (Type.GetType ("System.Span`1[[System.Byte, System.Runtime]][], System.Runtime"));
			} catch (Exception e) { }
		}

		[Kept]
		static void TestEscapedTypeName ()
		{
			var typeKept = Type.GetType ("Library.Not\\+Nested, EscapedTypeNames");
			RequireConstructor (typeKept);
			typeKept = Type.GetType ("Library.Not\\+Nested+Nes\\\\ted, EscapedTypeNames");
			RequireConstructor (typeKept);
			typeKept = Type.GetType ("Library.Not\\+Nested+Nes/ted, EscapedTypeNames");
			RequireConstructor (typeKept);
		}

		[Kept]
		class AssemblyTypeResolutionBehavior
		{
			[Kept]
			[ExpectedWarning ("IL2122")]
			static void TestRequireTypeInSameAssemblyAsGetType () {
				RequireHelper.RequireType ("Mono.Linker.Tests.Cases.Reflection.Dependencies.TypeDefinedInSameAssemblyAsGetType");
			}

			[Kept]
			[ExpectedWarning ("IL2122")]
			static void TestRequireTypeInSameAssemblyAsCallToRequireType () {
				RequireHelper.RequireType ("Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AssemblyTypeResolutionBehavior+TypeDefinedInSameAssemblyAsCallToRequireType");
			}

			[Kept]
			[ExpectedWarning ("IL2122")]
			static void TestRequireTypeWithNonAssemblyQualifiedGenericArguments () {
				RequireHelper.RequireType ("Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AssemblyTypeResolutionBehavior+Generic`1[[System.Int32]], test");
			}

			[Kept]
			[ExpectedWarning ("IL2122")]
			static void TestRequireTypeWithNonAssemblyQualifiedArrayType () {
				RequireHelper.RequireType ("Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AssemblyTypeResolutionBehavior+Generic`1["
						+ "[Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AssemblyTypeResolutionBehavior+ArrayElementGenericArgumentType]"
					+ "][], test");
			}

			[Kept]
			[ExpectedWarning ("IL2122")]
			static void TestRequireTypeWithNonAssemblyQualifiedPointerType () {
				RequireHelper.RequireType ("Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AssemblyTypeResolutionBehavior+Generic`1["
						+ "[Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AssemblyTypeResolutionBehavior+PointerElementGenericArgumentType]"
					+ "]*, test");
			}

			[Kept]
			[ExpectedWarning ("IL2122")]
			static void TestRequireTypeWithNonAssemblyQualifiedByRefType () {
				RequireHelper.RequireType ("Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AssemblyTypeResolutionBehavior+Generic`1["
						+ "[Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AssemblyTypeResolutionBehavior+ByRefElementGenericArgumentType]"
					+ "]&, test");
			}

			[Kept]
			public static void Test () {
				TestRequireTypeInSameAssemblyAsGetType ();
				TestRequireTypeInSameAssemblyAsCallToRequireType ();
				TestRequireTypeWithNonAssemblyQualifiedGenericArguments ();
				TestRequireTypeWithNonAssemblyQualifiedArrayType ();
				TestRequireTypeWithNonAssemblyQualifiedPointerType ();
				TestRequireTypeWithNonAssemblyQualifiedByRefType ();
			}

			class TypeDefinedInSameAssemblyAsCallToRequireType {}

			class Generic<T> {}

			class ArrayElementGenericArgumentType {}

			class PointerElementGenericArgumentType {}

			class ByRefElementGenericArgumentType {}
		}

		[Kept]
		static void RequireConstructor (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type) { }
	}
}
