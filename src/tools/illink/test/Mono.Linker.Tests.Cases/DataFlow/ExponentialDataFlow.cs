// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class ExponentialDataFlow
	{
		public static void Main ()
		{
			ExponentialArrayStates.Test ();
			ExponentialArrayStatesDataFlow.Test<int> ();
			ArrayStatesDataFlow.Test<int> ();
			ExponentialArrayInStateMachine.Test ();
			ExponentialStateFieldInStateMachine.Test ();
		}

		class ExponentialArrayStates
		{
			public static void Test ()
			{
				typeof (TestType).RequiresAll (); // Force data flow analysis

				object[] data = new object[20];
				if (true) data[0] = new object ();
				if (true) data[1] = new object ();
				if (true) data[2] = new object ();
				if (true) data[3] = new object ();
				if (true) data[4] = new object ();
				if (true) data[5] = new object ();
				if (true) data[6] = new object ();
				if (true) data[7] = new object ();
				if (true) data[8] = new object ();
				if (true) data[9] = new object ();
				if (true) data[10] = new object ();
				if (true) data[11] = new object ();
				if (true) data[12] = new object ();
				if (true) data[13] = new object ();
				if (true) data[14] = new object ();
				if (true) data[15] = new object ();
				if (true) data[16] = new object ();
				if (true) data[17] = new object ();
				if (true) data[18] = new object ();
				if (true) data[19] = new object ();
			}
		}

		class ArrayStatesDataFlow
		{
			class GenericTypeWithRequires<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T>
			{
			}

			[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'")]
			public static void Test<T> ()
			{
				Type[] types = new Type[1] { typeof (int) };
				if (true) types[0] = typeof (T);
				typeof (GenericTypeWithRequires<>).MakeGenericType (types);
			}
		}

		class ExponentialArrayStatesDataFlow
		{
			class GenericTypeWithRequires<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T0,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T1,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T2,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T3,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T4,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T5,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T6,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T7,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T8,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T9,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T10,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T11,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T12,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T13,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T14,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T15,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T16,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T17,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T18,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T19>
			{
			}

			[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			// The way we track arrays causes the analyzer to track exponentially many
			// ArrayValues in the ValueSet for the pattern in this method, hitting the limit.
			// When this happens, we replace the ValueSet with an unknown value, producing
			// this warning.
			[ExpectedWarning ("IL2055", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2090", "'T'", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			public static void Test<T> ()
			{
				Type[] types = new Type[20] {
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int),
					typeof (int)
				};
				if (Condition) types[0] = typeof (T);
				if (Condition) types[1] = typeof (T);
				if (Condition) types[2] = typeof (T);
				if (Condition) types[3] = typeof (T);
				if (Condition) types[4] = typeof (T);
				if (Condition) types[5] = typeof (T);
				if (Condition) types[6] = typeof (T);
				if (Condition) types[7] = typeof (T);
				if (Condition) types[8] = typeof (T);
				if (Condition) types[9] = typeof (T);
				if (Condition) types[10] = typeof (T);
				if (Condition) types[11] = typeof (T);
				if (Condition) types[12] = typeof (T);
				if (Condition) types[13] = typeof (T);
				if (Condition) types[14] = typeof (T);
				if (Condition) types[15] = typeof (T);
				if (Condition) types[16] = typeof (T);
				if (Condition) types[17] = typeof (T);
				if (Condition) types[18] = typeof (T);
				if (Condition) types[19] = typeof (T);

				typeof (GenericTypeWithRequires<,,,,,,,,,,,,,,,,,,,>).MakeGenericType (types);
			}

			static bool Condition => Random.Shared.Next (2) == 0;
		}

		class ExponentialArrayInStateMachine
		{
			// Force state machine
			static async Task RecursiveReassignment ()
			{
				typeof (TestType).RequiresAll (); // Force data flow analysis

				object[] args = null;
				args = new[] { args };
			}

			public static void Test()
			{
				RecursiveReassignment ().Wait ();
			}
		}

		class ExponentialStateFieldInStateMachine
		{
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			public static async void Test ()
			{
				Type t = GetWithPublicFields ();

				// 100
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();

				// 200
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();

				// 300
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();
				await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync (); await MethodAsync ();

				t.RequiresAll ();
			}
		}

		class TestType { }

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}

		static Type GetWithPublicFields () => null;
	}
}
