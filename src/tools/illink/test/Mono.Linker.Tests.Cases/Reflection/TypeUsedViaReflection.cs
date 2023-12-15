using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[KeptMember (".cctor()")]
	[ExpectedNoWarnings ()]
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
			TestGenericArray ();
			TestGenericArrayFullString ();
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
		}

		[Kept]
		public static void TestNull ()
		{
			const string reflectionTypeKeptString = null;
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public static void TestEmptyString ()
		{
			const string reflectionTypeKeptString = "";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Full { }

		[Kept]
		public static void TestFullString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Full, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Generic<T> { }

		[Kept]
		public static void TestGenericString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Generic`1, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class GenericArray<T> { }

		[Kept]
		public class GenericArgument { }

		[Kept]
		public static void TestGenericArray ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericArray`1[[Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericArgument]]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class GenericArrayFullString<T> { }

		[Kept]
		public class GenericArgumentFullString { }

		[Kept]
		public static void TestGenericArrayFullString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericArrayFullString`1" +
				"[[Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericArgumentFullString, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]," +
				" test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class FullConst { }

		[Kept]
		public static void TestFullStringConst ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+FullConst, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeAsmName { }

		[Kept]
		public static void TestTypeAsmName ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeAsmName, test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class AType { }

		[Kept]
		public static void TestType ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AType";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Pointer { }

		[Kept]
		public static void TestPointer ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Pointer*";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Reference { }

		[Kept]
		public static void TestReference ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Reference&";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Array { }

		[Kept]
		public static void TestArray ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Array[]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class ArrayOfArray { }

		[Kept]
		public static void TestArrayOfArray ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+ArrayOfArray[][]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}


		[Kept]
		public class MultiDimensionalArray { }

		[Kept]
		public static void TestMultiDimensionalArray ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimensionalArray[,]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class MultiDimensionalArrayFullString { }

		[Kept]
		public static void TestMultiDimensionalArrayFullString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimensionalArrayFullString[,], test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class MultiDimensionalArrayAsmName { }

		[Kept]
		public static void TestMultiDimensionalArrayAsmName ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimensionalArrayAsmName[,], test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		class Nested1
		{
			[Kept]
			class N2
			{
				[Kept]
				class N3
				{
				}
			}
		}

		[Kept]
		static void TestDeeplyNested ()
		{
			var typeKept = Type.GetType ("Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Nested1+N2+N3");
		}

		[Kept]
		class TypeOfToKeep { }

		[Kept]
		static void TestTypeOf ()
		{
			var typeKept = typeof (TypeOfToKeep);
		}

		[Kept]
		class TypeFromBranchA { }
		[Kept]
		class TypeFromBranchB { }

		[Kept]
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
		}

		[Kept]
		public class OverloadWith3Parameters { }

		[Kept]
		static void TestTypeOverloadWith3Parameters ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith3Parameters";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly);
		}


		[Kept]
		public class OverloadWith4Parameters { }

		[Kept]
		static void TestTypeOverloadWith4Parameters ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith4Parameters";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false);
		}

		public class OverloadWith5ParametersWithIgnoreCase { }

		[Kept]
		// Small difference in formatting between analyzer/NativeAOT/linker
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Func<AssemblyName,Assembly>, Func<Assembly,String,Boolean,Type>, Boolean, Boolean)'", ProducedBy = Tool.Trimmer)]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String,Func`2<AssemblyName,Assembly>,Func`4<Assembly,String,Boolean,Type>,Boolean,Boolean)'", ProducedBy = Tool.NativeAot)]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)'", ProducedBy = Tool.Analyzer)]
		static void TestTypeOverloadWith5ParametersWithIgnoreCase ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith5ParametersWithIgnoreCase";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false, true);
		}

		[Kept]
		public class OverloadWith5ParametersWithoutIgnoreCase { }

		[Kept]
		static void TestTypeOverloadWith5ParametersWithoutIgnoreCase ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith5ParametersWithoutIgnoreCase";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false, false);
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
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Func<AssemblyName,Assembly>, Func<Assembly,String,Boolean,Type>, Boolean, Boolean)'", ProducedBy = Tool.Trimmer)]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String,Func`2<AssemblyName,Assembly>,Func`4<Assembly,String,Boolean,Type>,Boolean,Boolean)'", ProducedBy = Tool.NativeAot)]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)'", ProducedBy = Tool.Analyzer)]
		static void TestUnknownIgnoreCase5Params (int num)
		{
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseUnknown2, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			bool unknownValue = num + 1 == 1;
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false, unknownValue);
		}

		[Kept]
		public class GenericTypeWithAnnotations_OuterType<
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicProperties)] T>
		{
		}

		[Kept]
		public class GenericTypeWithAnnotations_InnerType
		{
			// NativeAOT: https://github.com/dotnet/runtime/issues/95140
			[Kept (By = Tool.Trimmer)]
			[KeptBackingField]
			private static bool PrivateProperty { [Kept (By = Tool.Trimmer)] get; [Kept (By = Tool.Trimmer)] set; }

			private static void PrivateMethod () { }
		}

		[Kept]
		static void TestGenericTypeWithAnnotations ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericTypeWithAnnotations_OuterType`1" +
				"[[Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+GenericTypeWithAnnotations_InnerType, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]," +
				" test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			Type.GetType (reflectionTypeKeptString);
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
	}
}
