// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies
{
	public class LibWithConstantSubstitution
	{
		static bool _value;

		public static bool ReturnFalse ()
		{
			return _value;
		}
	}
}
