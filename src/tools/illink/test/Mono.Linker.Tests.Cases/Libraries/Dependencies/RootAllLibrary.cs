// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

#if RootAllLibrary
[assembly: TypeForwardedTo (typeof (RootAllLibrary_ExportedType))]
#endif

namespace Mono.Linker.Tests.Cases.Libraries.Dependencies
{
	public class RootAllLibrary
	{
		public static void Public ()
		{
		}

		private static void Private ()
		{
		}

		private class NestedType
		{
		}

		public static void RemovedBranch ()
		{
			if (SubstitutedProperty)
				RootAllLibrary_OptionalDependency.Use ();
		}

		// Substituted to false in RootAllLibrary_Substitutions.xml
		static bool SubstitutedProperty {
			get {
				RequiresUnreferencedCode ();
				return true;
			}
		}

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode))]
		static void RequiresUnreferencedCode ()
		{
		}

		[RootAllLibrary_RemovedAttribute]
		class TypeWithRemovedAttribute
		{
		}
	}

	class NonPublicType
	{
	}
}
