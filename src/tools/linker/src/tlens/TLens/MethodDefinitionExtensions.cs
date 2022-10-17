// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
