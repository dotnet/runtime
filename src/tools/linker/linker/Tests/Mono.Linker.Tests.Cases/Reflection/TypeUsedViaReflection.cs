using System;
using System.Diagnostics;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class TypeUsedViaReflection {
		public static void Main ()
		{
			TestNull ();
			TestEmptyString ();
			TestFullString ();
			TestGenericString ();
			TestFullStringConst();
			TestTypeAsmName ();
			TestType ();
			TestPointer ();
			TestReference ();
			TestArray ();
			TestArrayOfArray ();
			TestMultiDimentionalArray ();
			TestMultiDimentionalArrayFullString ();
			TestMultiDimentionalArrayAsmName ();
		}

		[Kept]
		public static void TestNull ()
		{
			string reflectionTypeKeptString = null;
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public static void TestEmptyString ()
		{
			string reflectionTypeKeptString = "";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Full { }

		[Kept]
		public static void TestFullString ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Full, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Generic<T> { }

		[Kept]
		public static void TestGenericString ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Generic`1, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class FullConst { }

		[Kept]
		public static void TestFullStringConst()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+FullConst, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType(reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeAsmName { }

		[Kept]
		public static void TestTypeAsmName ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeAsmName, test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class AType { }

		[Kept]
		public static void TestType ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AType";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Pointer { }

		[Kept]
		public static void TestPointer ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Pointer*";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Reference { }

		[Kept]
		public static void TestReference ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Reference&";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Array { }

		[Kept]
		public static void TestArray ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Array[]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class ArrayOfArray{ }

		[Kept]
		public static void TestArrayOfArray ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+ArrayOfArray[][]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}


		[Kept]
		public class MultiDimentionalArray{ }

		[Kept]
		public static void TestMultiDimentionalArray ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimentionalArray[,]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class MultiDimentionalArrayFullString { }

		[Kept]
		public static void TestMultiDimentionalArrayFullString ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimentionalArrayFullString[,], test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class MultiDimentionalArrayAsmName { }

		[Kept]
		public static void TestMultiDimentionalArrayAsmName ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimentionalArrayAsmName[,], test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}
	}
}
