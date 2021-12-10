// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;

namespace Mono.Linker
{
	public class OverrideInformation
	{
		internal OverrideInformation ()
		{
		}

		public static MethodDefinition Base { get { throw null; } }
		public static MethodDefinition Override { get { throw null; } }
		public static InterfaceImplementation MatchingInterfaceImplementation { get { throw null; } }
		public static TypeDefinition InterfaceType { get { throw null; } }
	}
}
