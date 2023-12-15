// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker
{

	public class LinkContext : IMetadataResolver
	{
		internal LinkContext () { }
		public AnnotationStore Annotations { get { throw null; } }

		public TypeDefinition GetType (string fullName) { throw null; }
		public string GetAssemblyLocation (AssemblyDefinition assembly) { throw null; }
		public AssemblyDefinition GetLoadedAssembly (string name) { throw null; }

		public void LogMessage (MessageContainer message) { throw null; }

		public bool HasCustomData (string key) { throw null; }
		public bool TryGetCustomData (string key, out string value) { throw null; }

		public MethodDefinition Resolve (MethodReference methodReference) { throw null; }
		public FieldDefinition Resolve (FieldReference fieldReference) { throw null; }
		public TypeDefinition Resolve (TypeReference typeReference) { throw null; }

		public MethodDefinition TryResolve (MethodReference methodReference) { throw null; }
		public FieldDefinition TryResolve (FieldReference fieldReference) { throw null; }
		public TypeDefinition TryResolve (TypeReference typeReference) { throw null; }

		public AssemblyDefinition Resolve (AssemblyNameReference nameReference) { throw null; }
	}
}
