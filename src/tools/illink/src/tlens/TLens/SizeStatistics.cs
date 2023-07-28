// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
