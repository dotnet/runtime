// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	internal static partial class ValueExtensions
	{
		internal static string ValueToString (this SingleValue value, params object[] args)
		{
			if (value == null)
				return "<null>";

			StringBuilder sb = new StringBuilder ();
			sb.Append (value.GetType ().Name);
			sb.Append ("(");
			if (args != null) {
				for (int i = 0; i < args.Length; i++) {
					if (i > 0)
						sb.Append (",");
					sb.Append (args[i] == null ? "<null>" : args[i].ToString ());
				}
			}
			sb.Append (")");
			return sb.ToString ();
		}
	}
}
