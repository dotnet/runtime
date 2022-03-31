// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker
{
	public static class CustomAttributeArgumentExtensions
	{
		public static bool IsEqualTo (this CustomAttributeArgument A, CustomAttributeArgument B)
		{
			var aVal = A.BaseValue ();
			var bVal = B.BaseValue ();
			if (aVal is CustomAttributeArgument[] aArray && bVal is CustomAttributeArgument[] bArray) {
				if (aArray.Length != bArray.Length)
					return false;
				for (int i = 0; i < aArray.Length; i++) {
					if (!aArray[i].IsEqualTo (bArray[i]))
						return false;
				}
				return true;
			}
			return aVal.Equals (bVal);
		}
		static object BaseValue (this object value)
		{
			while (value is CustomAttributeArgument caa)
				value = caa.Value;
			return value;
		}
	}
}
