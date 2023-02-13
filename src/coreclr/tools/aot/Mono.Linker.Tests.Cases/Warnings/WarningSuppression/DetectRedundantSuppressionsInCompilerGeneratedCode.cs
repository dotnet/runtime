// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;


namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class DetectRedundantSuppressionsInCompilerGeneratedCode
	{
		public static void Main ()
		{
			RedundantSuppressionOnLocalMethod.Test ();
			RedundantSuppressionInIteratorBody.Test ();
			RedundantSuppressionInAsyncBody.Test ();
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (DetectRedundantSuppressionsInCompilerGeneratedCode);
		}

		public static string TrimmerCompatibleMethod ()
		{
			return "test";
		}

		public class RedundantSuppressionOnLocalMethod
		{
			public static void Test ()
			{
				[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
				[UnconditionalSuppressMessage ("Test", "IL2071")]
				void LocalMethod ()
				{
					TrimmerCompatibleMethod ();
				}

				LocalMethod ();
			}
		}

		public class RedundantSuppressionInIteratorBody
		{
			public static void Test ()
			{
				Enumerable ();
			}

			[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			static IEnumerable<int> Enumerable ()
			{
				TrimmerCompatibleMethod ();
				yield return 0;
			}
		}

		public class RedundantSuppressionInAsyncBody
		{
			[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public static async void Test ()
			{
				TrimmerCompatibleMethod ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2121", "IL2070", ProducedBy = ProducedBy.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2070")]
			static async Task<int> MethodAsync ()
			{
				return await Task.FromResult (0);
			}
		}
	}
}
