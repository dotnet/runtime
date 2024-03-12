// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text;
using ILLink.Shared.DataFlow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	internal static partial class ValueExtensions
	{
		internal static string ValueToString (this SingleValue value, params object[] args)
		{
			if (value == null)
				return "<null>";

			StringBuilder sb = new ();
			sb.Append (value.GetType ().Name);
			sb.Append ('(');
			if (args != null) {
				for (int i = 0; i < args.Length; i++) {
					if (i > 0)
						sb.Append (',');
					sb.Append (args[i] == null ? "<null>" : args[i].ToString ());
				}
			}
			sb.Append (')');
			return sb.ToString ();
		}

		internal static int? AsConstInt (this SingleValue value)
		{
			if (value is ConstIntValue constInt)
				return constInt.Value;

			return null;
		}

		internal static int? AsConstInt (this in MultiValue value)
		{
			if (value.AsSingleValue () is ConstIntValue constInt)
				return constInt.Value;

			return null;
		}

		internal static SingleValue? AsSingleValue (this in MultiValue node)
		{
			var values = node.AsEnumerable ();
			if (values.Count () != 1)
				return null;

			return values.Single ();
		}

		private static ValueSet<SingleValue>.Enumerable Unknown = new ValueSet<SingleValue>.Enumerable (UnknownValue.Instance);

		// ValueSet<TValue> is not enumerable. This helper translates ValueSet<SingleValue>.Unknown
		// into a ValueSet<SingleValue> whose sole element is UnknownValue.Instance.
		internal static ValueSet<SingleValue>.Enumerable AsEnumerable (this MultiValue multiValue)
		{
			return multiValue.IsUnknown ()
				? Unknown
				: multiValue.GetKnownValues ();
		}
	}
}
