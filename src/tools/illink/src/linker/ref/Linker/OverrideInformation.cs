// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker
{
	public class OverrideInformation
	{
		internal OverrideInformation ()
		{
		}

		public MethodDefinition Base { get { throw null; } }
		public MethodDefinition Override { get { throw null; } }
		public InterfaceImplementation MatchingInterfaceImplementation { get { throw null; } }
		public TypeDefinition InterfaceType { get { throw null; } }
	}
}
