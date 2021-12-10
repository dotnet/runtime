// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;

namespace Mono.Linker
{

	public class LinkContext : IMetadataResolver
	{
		internal LinkContext () { }
		public static AnnotationStore Annotations { get { throw null; } }

		public static TypeDefinition GetType (string fullName) { throw null; }
		public static string GetAssemblyLocation (AssemblyDefinition assembly) { throw null; }
		public static AssemblyDefinition GetLoadedAssembly (string name) { throw null; }

		public static void LogMessage (MessageContainer message) { throw null; }

		public static bool HasCustomData (string key) { throw null; }
		public static bool TryGetCustomData (string key, out string value) { throw null; }

		public MethodDefinition Resolve (MethodReference methodReference) { throw null; }
		public FieldDefinition Resolve (FieldReference fieldReference) { throw null; }
		public TypeDefinition Resolve (TypeReference typeReference) { throw null; }

		public static MethodDefinition TryResolve (MethodReference methodReference) { throw null; }
		public static FieldDefinition TryResolve (FieldReference fieldReference) { throw null; }
		public static TypeDefinition TryResolve (TypeReference typeReference) { throw null; }

		public static AssemblyDefinition Resolve (AssemblyNameReference nameReference) { throw null; }
	}
}
