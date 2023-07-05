// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.TestCasesRunner;
using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[NonParallelizable]
	[TestFixture]
	public class GetDisplayNameTests
	{
		[TestCaseSource (nameof (GetMemberAssertions), new object[] { typeof (GetDisplayNameTests) })]
		public void TestGetDisplayName (IMemberDefinition member, CustomAttribute customAttribute)
		{
			// The only intention with these tests is to check that the language elements that could
			// show up in a warning are printed in a way that is friendly to the user.
			if (customAttribute.AttributeType.Name != nameof (DisplayNameAttribute))
				throw new NotImplementedException ();

			var expectedDisplayName = (string) customAttribute.ConstructorArguments[0].Value;
			switch (member.MetadataToken.TokenType) {
			case TokenType.TypeRef:
			case TokenType.TypeDef:
				Assert.AreEqual (expectedDisplayName, (member as TypeReference).GetDisplayName ());
				break;
			case TokenType.MemberRef:
			case TokenType.Method:
				Assert.AreEqual (expectedDisplayName, (member as MethodReference).GetDisplayName ());
				break;
			case TokenType.Field:
				Assert.AreEqual (expectedDisplayName, (member as FieldReference).GetDisplayName ());
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		public static IEnumerable<TestCaseData> GetMemberAssertions (Type type) => MemberAssertionsCollector.GetMemberAssertionsData (type);

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.Field")]
		public int Field;

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MultipleParameters(Int32, Int32)")]
		public static void MultipleParameters (int a, int b)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A")]
		public class A
		{
			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.B")]
			public class B
			{
				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.B.C")]
				public class C
				{
				}
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.SingleDimensionalArrayTypeParameter(Int32[])")]
			public static void SingleDimensionalArrayTypeParameter (int[] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.MultiDimensionalArrayTypeParameter(Int32[,])")]
			public static void MultiDimensionalArrayTypeParameter (int[,] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.JaggedArrayTypeParameter(Int32[][,])")]
			public static void JaggedArrayTypeParameter (int[][,] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.JaggedArrayTypeParameter(Int32[,][])")]
			public static void JaggedArrayTypeParameter (int[,][] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.JaggedArrayTypeParameter(Int32[,][,,][,,,])")]
			public static void JaggedArrayTypeParameter (int[,][,,][,,,] p)
			{
			}

			// PointerType
			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.CommonPointerPointerTypeParameter(Int32*)")]
			public static unsafe void CommonPointerPointerTypeParameter (int* p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.PointerToPointerPointerTypeParameter(Int32**)")]
			public static unsafe void PointerToPointerPointerTypeParameter (int** p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.PointerToArrayPointerTypeParameter(Int32*[,,,])")]
			public static unsafe void PointerToArrayPointerTypeParameter (int*[,,,] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.PointerToArrayPointerTypeParameter(Int32*[,][,,])")]
			public static unsafe void PointerToArrayPointerTypeParameter (int*[,][,,] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.PointerTypeToUnknownTypeParameter(Void*)")]
			public static unsafe void PointerTypeToUnknownTypeParameter (void* p)
			{
			}
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.PartialClass")]
		public partial class PartialClass
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.StaticClass")]
		public static class StaticClass
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>")]
		public class GenericClassOneParameter<T>
		{
			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>")]
			public class NestedGenericClassOneParameter<S>
			{
				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.A<U>")]
				public class A<U>
				{
				}

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.Delegate<U>")]
				public delegate void Delegate<U> (U p);

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.MethodGenericArray<U,V>(IList<U>, V)")]
				public void MethodGenericArray<U, V> (IList<U> p, V q)
				{
				}

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.MethodWithRef(Int32&)")]
				public void MethodWithRef (ref int p)
				{
				}

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.MethodWithRefAndGeneric<U>(U&)")]
				public void MethodWithRefAndGeneric<U> (ref U p)
				{
				}

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.MethodWithRefReturnType()")]
				public ref int MethodWithRefReturnType ()
				{
					int[] p = new int[] { 0 };
					return ref p[0];
				}
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.B")]
			public class B
			{
				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.B.MethodWithGenericTypeArguments" +
					"(GetDisplayNameTests.GenericClassMultipleParameters<Int32,Int32[]>)")]
				public void MethodWithGenericTypeArguments (GenericClassMultipleParameters<int, int[]> p)
				{
				}

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.B.CommonMethod()")]
				public void CommonMethod ()
				{
				}
			}
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithNestedGenericTypeArgumentsNoArgumentsOnLeaf(GetDisplayNameTests.GenericClassOneParameter<Int32>.B)")]
		public static void MethodWithNestedGenericTypeArgumentsNoArgumentsOnLeaf (GenericClassOneParameter<int>.B p) { }

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassMultipleParameters<T,S>")]
		public class GenericClassMultipleParameters<T, S>
		{
			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassMultipleParameters<T,S>.NestedGenericClassMultipleParameters<U,V>")]
			public class NestedGenericClassMultipleParameters<U, V>
			{
			}
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithGenericTypeArgument(IList<GetDisplayNameTests.GenericClassOneParameter<Byte*[]>>)")]
		public static unsafe void MethodWithGenericTypeArgument (IList<GenericClassOneParameter<byte*[]>> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithGenericTypeArguments(GetDisplayNameTests.GenericClassMultipleParameters<Char*[],Int32[,][]>)")]
		public static unsafe void MethodWithGenericTypeArguments (GenericClassMultipleParameters<char*[], int[,][]> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithNestedGenericTypeArguments" +
			"(GetDisplayNameTests.GenericClassMultipleParameters<Char*[],Int32[,][]>.NestedGenericClassMultipleParameters<Char*[],Int32[,][]>)")]
		public static unsafe void MethodWithNestedGenericTypeArguments (GenericClassMultipleParameters<char*[], int[,][]>.NestedGenericClassMultipleParameters<char*[], int[,][]> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithPartiallyInstantiatedNestedGenericTypeArguments<MethodT,MethodV>" +
			"(GetDisplayNameTests.GenericClassMultipleParameters<MethodT,String>.NestedGenericClassMultipleParameters<Int32,MethodV>)")]
		public static void MethodWithPartiallyInstantiatedNestedGenericTypeArguments<MethodT, MethodV> (
			GenericClassMultipleParameters<MethodT, string>.NestedGenericClassMultipleParameters<int, MethodV> p)
		{
		}
	}
}

[TestFixture]
public class GetDisplayNameTestsGlobalScope
{
	[TestCaseSource (nameof (GetMemberAssertions), new object[] { typeof (global::GetDisplayNameTestsGlobalScope) })]
	public void TestGetDisplayName (IMemberDefinition member, CustomAttribute customAttribute)
	{
		var expectedDisplayName = (string) customAttribute.ConstructorArguments[0].Value;
		Assert.AreEqual (expectedDisplayName, (member as MemberReference).GetDisplayName ());
	}

	public static IEnumerable<TestCaseData> GetMemberAssertions (Type type) => MemberAssertionsCollector.GetMemberAssertionsData (type);

	[DisplayName ("GetDisplayNameTestsGlobalScope.TypeInGlobalScope")]
	public class TypeInGlobalScope
	{
		[DisplayName ("GetDisplayNameTestsGlobalScope.TypeInGlobalScope.Method()")]
		public static void Method ()
		{
		}
	}
}
