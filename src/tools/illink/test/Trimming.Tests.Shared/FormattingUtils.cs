// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public static class FormattingUtils
	{
		public static string FormatSequenceCompareFailureMessage (IEnumerable<string> actual, IEnumerable<string> expected)
		{
			var builder = new StringBuilder ();
			builder.AppendLine ("---------------");
			builder.AppendLine ($"Expected/Original (Total : {expected.Count ()})");
			builder.AppendLine ("---------------");
			// Format in a quoted array form for easier copying into a expected sequence attribute
			builder.AppendLine (expected.Select (c => $"\"{c}\",").AggregateWithNewLine ());
			builder.AppendLine ("---------------");
			builder.AppendLine ($"Actual/Linked (Total : {actual.Count ()})");
			builder.AppendLine ("---------------");
			// Format in a quoted array form for easier copying into a expected sequence attribute
			builder.AppendLine (actual.Select (c => $"\"{c}\",").AggregateWithNewLine ());
			builder.AppendLine ("---------------");
			return builder.ToString ();
		}

		public static string FormatSequenceCompareFailureMessage2 (IEnumerable<string> actual, IEnumerable<string> expected, IEnumerable<string> original)
		{
			var builder = new StringBuilder ();
			builder.AppendLine ("---------------");
			builder.AppendLine ($"Expected (Total : {expected.Count ()})");
			builder.AppendLine ("---------------");
			// Format in a quoted array form for easier copying into a expected sequence attribute
			builder.AppendLine (expected.Select (c => $"\"{c}\",").AggregateWithNewLine ());
			builder.AppendLine ("---------------");
			builder.AppendLine ($"Actual/Linked (Total : {actual.Count ()})");
			builder.AppendLine ("---------------");
			// Format in a quoted array form for easier copying into a expected sequence attribute
			builder.AppendLine (actual.Select (c => $"\"{c}\",").AggregateWithNewLine ());
			builder.AppendLine ("---------------");
			builder.AppendLine ($"Original (Total : {original.Count ()})");
			builder.AppendLine ("---------------");
			// Format in a quoted array form for easier copying into a expected sequence attribute
			builder.AppendLine (original.Select (c => $"\"{c}\",").AggregateWithNewLine ());
			builder.AppendLine ("---------------");
			return builder.ToString ();
		}

		private static string AggregateWithNewLine (this IEnumerable<string> elements)
		{
			return elements.AggregateWith (System.Environment.NewLine);
		}

		private static string AggregateWith (this IEnumerable<string> elements, string separator)
		{
			if (elements.Any ())
				return elements.Aggregate ((buff, s) => buff + separator + s);

			return string.Empty;
		}
	}
}
