// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Mono.Cecil;

namespace TLens
{
	static class SizeStatistics
	{
		public static int GetEstimatedSize (this TypeDefinition type)
		{
			return type.Methods.Sum (l => l.GetEstimatedSize ());
		}

		public static int GetEstimatedSize (this MethodDefinition method)
		{
			return method.HasBody ? method.Body.CodeSize : 0;
		}
	}
}
