// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.RecursiveInterfaces
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods | TestRunCharacteristics.SupportsStaticInterfaceMethods, "Requires support for default and static interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/RecursiveGenericInterfacesStatic.il" })]
	[KeptAllTypesAndMembersInAssembly ("library.dll")]
	[KeptMemberInAssembly ("library.dll", "IBase`3", "GetT()", "GetU()", "GetV()")]
	[KeptTypeInAssembly ("library.dll", "IMiddle`2")]
	// Below isn't strictly necessary, but we keep them since the interface is generic and we haven't hardened generic interface handling to only keep the single closest DIM.
	// We use method definition to match the .override to the required DIM. However, one DIM might be for a different generic instance than we are searching for.
	// Because of this, we keep all generic interface DIMs that may be the DIM we need.
	[KeptMemberInAssembly ("library.dll", "IMiddle`2", "IBase<T,U,System.Int32>.GetV()", "IBase<T,U,System.Single>.GetV()")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "IMiddle`2", "library.dll", "IBase`3<T,U,System.Int32>")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "IMiddle`2", "library.dll", "IBase`3<T,U,System.Single>")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IMiddle`2", "IBase<T,U,System.Int32>.GetV", "V IBase`3<T,U,System.Int32>::GetV()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IMiddle`2", "IBase<T,U,System.Single>.GetV", "V IBase`3<T,U,System.Single>::GetV()")]
	[KeptTypeInAssembly ("library.dll", "IDerived`1")]
	[KeptMemberInAssembly ("library.dll", "IDerived`1",
			"IBase<T,System.Int64,System.Int32>.GetV()",
			"IBase<T,System.Int64,System.Single>.GetV()",
			"IBase<T,System.Int64,System.Int32>.GetU()",
			"IBase<T,System.Int64,System.Single>.GetU()",
			"IBase<T,System.Double,System.Int32>.GetU()",
			"IBase<T,System.Double,System.Single>.GetU()")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "IDerived`1", "library.dll", "IMiddle`2<T,System.Int64>")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "IDerived`1", "library.dll", "IMiddle`2<T,System.Double>")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Int64,System.Int32>.GetV", "V IBase`3<T,System.Int64,System.Int32>::GetV()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Int64,System.Single>.GetV", "V IBase`3<T,System.Int64,System.Single>::GetV()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Int64,System.Int32>.GetU", "U IBase`3<T,System.Int64,System.Int32>::GetU()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Int64,System.Single>.GetU", "U IBase`3<T,System.Int64,System.Single>::GetU()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Double,System.Int32>.GetU", "U IBase`3<T,System.Double,System.Int32>::GetU()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Double,System.Single>.GetU", "U IBase`3<T,System.Double,System.Single>::GetU()")]
	[KeptTypeInAssembly ("library.dll", "MyClass")]
	[KeptMemberInAssembly ("library.dll", "MyClass",
			"IBase<System.Char,System.Int64,System.Int32>.GetT()",
			"IBase<System.Char,System.Double,System.Int32>.GetT()",
			"IBase<System.Char,System.Int64,System.Single>.GetT()",
			"IBase<System.Char,System.Double,System.Single>.GetT()",
			"IBase<System.String,System.Int64,System.Int32>.GetT()",
			"IBase<System.String,System.Double,System.Int32>.GetT()",
			"IBase<System.String,System.Int64,System.Single>.GetT()",
			"IBase<System.String,System.Double,System.Single>.GetT()")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "MyClass", "library.dll", "IDerived`1<System.Char>")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "MyClass", "library.dll", "IDerived`1<System.String>")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.Char,System.Int64,System.Int32>.GetT", "T IBase`3<System.Char,System.Int64,System.Int32>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.Char,System.Double,System.Int32>.GetT", "T IBase`3<System.Char,System.Double,System.Int32>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.Char,System.Int64,System.Single>.GetT", "T IBase`3<System.Char,System.Int64,System.Single>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.Char,System.Double,System.Single>.GetT", "T IBase`3<System.Char,System.Double,System.Single>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.String,System.Int64,System.Int32>.GetT", "T IBase`3<System.String,System.Int64,System.Int32>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.String,System.Double,System.Int32>.GetT", "T IBase`3<System.String,System.Double,System.Int32>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.String,System.Int64,System.Single>.GetT", "T IBase`3<System.String,System.Int64,System.Single>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.String,System.Double,System.Single>.GetT", "T IBase`3<System.String,System.Double,System.Single>::GetT()")]
	public class RecursiveGenericInterfacesStatic
	{
		[Kept]
		public static void Main ()
		{

#if IL_ASSEMBLY_AVAILABLE
			UseIBase<char, double, float, MyDerivedClass> ();
			_ = new MyDerivedClass ();
		}

		[Kept]
		public static void UseIBase<T, U, V, TBase> () where TBase: IBase<T, U, V>
		{
			TBase.GetT ();
			TBase.GetU ();
			TBase.GetV ();
#endif
		}

		//public interface IBase<T, U, V>
		//{
		//	static abstract T GetT ();
		//	static virtual U GetU () => default;
		//	static virtual V GetV () => default;
		//}

		//public interface IMiddle<T, U> : IBase<T, U, int>, IBase<T, U, float>
		//{
		//	static int IBase<T, U, int>.GetV () => 12;
		//	static float IBase<T, U, float>.GetV () => 12.0f;
		//}

		//public interface IDerived<T> : IMiddle<T, long>, IMiddle<T, double>
		//{
		//	static int IBase<T, long, int>.GetV () => 12;
		//	static float IBase<T, long, float>.GetV () => 12.0f;

		//	static long IBase<T, long, int>.GetU () => 12;
		//	static long IBase<T, long, float>.GetU () => 12;

		//	static double IBase<T, double, int>.GetU () => 12;
		//	static double IBase<T, double, float>.GetU () => 12;
		//}

		//public class MyClass : IDerived<string>, IDerived<char>
		//{
		//	static string IBase<string, long, float>.GetT () => throw new NotImplementedException ();
		//	static string IBase<string, double, int>.GetT () => throw new NotImplementedException ();
		//	static string IBase<string, double, float>.GetT () => throw new NotImplementedException ();
		//	static string IBase<string, long, int>.GetT () => throw new NotImplementedException ();
		//	static char IBase<char, long, int>.GetT () => throw new NotImplementedException ();
		//	static char IBase<char, long, float>.GetT () => throw new NotImplementedException ();
		//	static char IBase<char, double, int>.GetT () => throw new NotImplementedException ();
		//	static char IBase<char, double, float>.GetT () => throw new NotImplementedException ();
		//}

		//public class MyDerivedClass : MyClass
		//{ }
	}
}
