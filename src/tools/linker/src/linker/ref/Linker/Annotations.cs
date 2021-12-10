// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{

	public partial class AnnotationStore
	{
		internal AnnotationStore () { }

		public static IEnumerable<OverrideInformation> GetOverrides (MethodDefinition method) { throw null; }

		public static void Mark (IMetadataTokenProvider provider) { throw null; }
		public static void Mark (CustomAttribute attribute) { throw null; }

		public static bool IsMarked (IMetadataTokenProvider provider) { throw null; }
		public static bool IsMarked (CustomAttribute attribute) { throw null; }

		public static void AddPreservedMethod (MethodDefinition key, MethodDefinition method) { throw null; }
		public static void AddPreservedMethod (TypeDefinition type, MethodDefinition method) { throw null; }
		public static void SetPreserve (TypeDefinition type, TypePreserve preserve) { throw null; }

		public static void SetAction (MethodDefinition method, MethodAction action) { throw null; }
		public static void SetStubValue (MethodDefinition method, object value) { throw null; }

		public static AssemblyAction GetAction (AssemblyDefinition assembly) { throw null; }
		public static void SetAction (AssemblyDefinition assembly, AssemblyAction action) { throw null; }
		public static bool HasAction (AssemblyDefinition assembly) { throw null; }

		public static object GetCustomAnnotation (object key, IMetadataTokenProvider item) { throw null; }
		public static void SetCustomAnnotation (object key, IMetadataTokenProvider item, object value) { throw null; }
	}
}
