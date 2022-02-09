// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
