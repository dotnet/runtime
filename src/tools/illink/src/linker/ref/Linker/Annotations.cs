// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{

	public partial class AnnotationStore
	{
		internal AnnotationStore () { }

		public IEnumerable<OverrideInformation> GetOverrides (MethodDefinition method) { throw null; }

		public void Mark (IMetadataTokenProvider provider) { throw null; }
		public void Mark (CustomAttribute attribute) { throw null; }

		public bool IsMarked (IMetadataTokenProvider provider) { throw null; }
		public bool IsMarked (CustomAttribute attribute) { throw null; }

		public void AddPreservedMethod (MethodDefinition key, MethodDefinition method) { throw null; }
		public void AddPreservedMethod (TypeDefinition type, MethodDefinition method) { throw null; }
		public void SetPreserve (TypeDefinition type, TypePreserve preserve) { throw null; }

		public void SetAction (MethodDefinition method, MethodAction action) { throw null; }
		public void SetStubValue (MethodDefinition method, object value) { throw null; }

		public AssemblyAction GetAction (AssemblyDefinition assembly) { throw null; }
		public void SetAction (AssemblyDefinition assembly, AssemblyAction action) { throw null; }
		public bool HasAction (AssemblyDefinition assembly) { throw null; }

		public object GetCustomAnnotation (object key, IMetadataTokenProvider item) { throw null; }
		public void SetCustomAnnotation (object key, IMetadataTokenProvider item, object value) { throw null; }
	}
}
