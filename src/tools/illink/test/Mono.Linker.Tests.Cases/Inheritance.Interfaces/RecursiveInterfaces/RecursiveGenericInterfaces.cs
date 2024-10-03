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
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/RecursiveGenericInterfaces.il" })]
	[KeptAllTypesAndMembersInAssembly ("library.dll")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IMiddle`2", "IBase<T,U,System.Int32>.GetV", "V IBase`3<T,U,System.Int32>::GetV()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IMiddle`2", "IBase<T,U,System.Single>.GetV", "V IBase`3<T,U,System.Single>::GetV()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Int64,System.Int32>.GetV", "V IBase`3<T,System.Int64,System.Int32>::GetV()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Int64,System.Single>.GetV", "V IBase`3<T,System.Int64,System.Single>::GetV()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Int64,System.Int32>.GetU", "U IBase`3<T,System.Int64,System.Int32>::GetU()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Int64,System.Single>.GetU", "U IBase`3<T,System.Int64,System.Single>::GetU()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Double,System.Int32>.GetU", "U IBase`3<T,System.Double,System.Int32>::GetU()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "IDerived`1", "IBase<T,System.Double,System.Single>.GetU", "U IBase`3<T,System.Double,System.Single>::GetU()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.Char,System.Int64,System.Int32>.GetT", "T IBase`3<System.Char,System.Int64,System.Int32>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.Char,System.Double,System.Int32>.GetT", "T IBase`3<System.Char,System.Double,System.Int32>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.Char,System.Int64,System.Single>.GetT", "T IBase`3<System.Char,System.Int64,System.Single>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.Char,System.Double,System.Single>.GetT", "T IBase`3<System.Char,System.Double,System.Single>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.String,System.Int64,System.Int32>.GetT", "T IBase`3<System.String,System.Int64,System.Int32>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.String,System.Double,System.Int32>.GetT", "T IBase`3<System.String,System.Double,System.Int32>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.String,System.Int64,System.Single>.GetT", "T IBase`3<System.String,System.Int64,System.Single>::GetT()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "MyClass", "IBase<System.String,System.Double,System.Single>.GetT", "T IBase`3<System.String,System.Double,System.Single>::GetT()")]
	[RuntimeInterfaceOnTypeInAssembly ("library.dll", "MyClass", "IBase`3<System.Char,System.Int64,System.Int32>", ["IDerived`1<System.Char>", "IMiddle`2<T,System.Int64>", "IBase`3<T,U,System.Int32>"])]
	[RuntimeInterfaceOnTypeInAssembly ("library.dll", "MyClass", "IBase`3<System.String,System.Int64,System.Int32>", ["IDerived`1<System.String>", "IMiddle`2<T,System.Int64>", "IBase`3<T,U,System.Int32>"])]
	public class RecursiveGenericInterfaces
	{
		[Kept]
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			UseIBase<char, double, float> (new MyDerivedClass ());
		}

		[Kept]
		public static void UseIBase<T, U, V> (IBase<T, U, V> myBase)
		{
			myBase.GetT ();
			myBase.GetU ();
			myBase.GetV ();
#endif
		}

		//public interface IBase<T, U, V>
		//{
		//	T GetT ();
		//	U GetU () => default;
		//	V GetV () => default;
		//}

		//public interface IMiddle<T, U> : IBase<T, U, int>, IBase<T, U, float>
		//{
		//	int IBase<T, U, int>.GetV () => 12;
		//	float IBase<T, U, float>.GetV () => 12.0f;
		//}

		//public interface IDerived<T> : IMiddle<T, long>, IMiddle<T, double>
		//{
		//	int IBase<T, long, int>.GetV () => 12;
		//	float IBase<T, long, float>.GetV () => 12.0f;

		//	long IBase<T, long, int>.GetU () => 12;
		//	long IBase<T, long, float>.GetU () => 12;

		//	double IBase<T, double, int>.GetU () => 12;
		//	double IBase<T, double, float>.GetU () => 12;
		//}

		//public class MyClass : IDerived<string>, IDerived<char>
		//{
		//	string IBase<string, long, float>.GetT () => throw new NotImplementedException ();
		//	string IBase<string, double, int>.GetT () => throw new NotImplementedException ();
		//	string IBase<string, double, float>.GetT () => throw new NotImplementedException ();
		//	string IBase<string, long, int>.GetT () => throw new NotImplementedException ();
		//	char IBase<char, long, int>.GetT () => throw new NotImplementedException ();
		//	char IBase<char, long, float>.GetT () => throw new NotImplementedException ();
		//	char IBase<char, double, int>.GetT () => throw new NotImplementedException ();
		//	char IBase<char, double, float>.GetT () => throw new NotImplementedException ();
		//}

		//public class MyDerivedClass : MyClass
		//{ }
	}
}
