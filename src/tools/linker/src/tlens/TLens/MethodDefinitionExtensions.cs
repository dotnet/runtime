// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Mono.Cecil;

namespace TLens
{
	static class MethodDefinitionExtensions
	{
		public static string ToDisplay (this MethodDefinition method, bool showSize = false)
		{
			var str = new StringBuilder ();
			str.Append (method.FullName);
			int idx = method.FullName.IndexOf (' ');
			str.Remove (0, idx + 1);
			if (showSize) {
				str.Append (" [size: ");
				str.Append (method.GetEstimatedSize ());
				str.Append ("]");
			}

			return str.ToString ();
		}
	}
}