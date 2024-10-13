// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class DataflowInLocalMethodGroupArgument
	{
		public static void Main ()
		{
			new GenericWithNonPublicConstructors<int> ().Test ();
			GenericWithNonPublicConstructors<int>.TestInstanceLocalMethod ();
		}

		public class GenericWithNonPublicConstructors<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>
		{
			public void Test ()
			{
				[ExpectedWarning ("IL2087", "PublicFields", "RequiresPublicFields", "T", "GenericWithNonPublicConstructors")]
				static int GetIntWithBadDataflow (int x)
				{
					typeof (T).RequiresPublicFields();
					return 0;
				}

				new Dictionary<int, int> ().GetOrAdd (0, GetIntWithBadDataflow);
			}

			public static void TestInstanceLocalMethod ()
			{
				[ExpectedWarning ("IL2087", "PublicFields", "RequiresPublicFields", "T", "GenericWithNonPublicConstructors")]
				int GetIntWithBadDataflow (int x)
				{
					typeof (T).RequiresPublicFields();
					return 0;
				}

				new Dictionary<int, int> ().GetOrAdd (0, GetIntWithBadDataflow);
			}
		}
	}

	public static class DictionaryExtensions
	{
		public static TValue GetOrAdd<TKey, TValue> (this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
		{
			if (dictionary.TryGetValue (key, out var value))
				return value;

			value = valueFactory (key);
			dictionary.TryAdd (key, value);
			return value;
		}
	}
}
