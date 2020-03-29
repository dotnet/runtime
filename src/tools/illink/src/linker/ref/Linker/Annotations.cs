// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker {

	public partial class AnnotationStore
	{
		internal AnnotationStore() {}

		public List<OverrideInformation> GetOverrides (MethodDefinition method) { throw null; }

		public void Mark (IMetadataTokenProvider provider) { throw null; }
		public void Mark (CustomAttribute attribute) { throw null; }

		public void AddPreservedMethod (TypeDefinition type, MethodDefinition method) { throw null; }
		public void SetPreserve (TypeDefinition type, TypePreserve preserve) { throw null; }

		public AssemblyAction GetAction (AssemblyDefinition assembly) { throw null; }
		public void SetAction (AssemblyDefinition assembly, AssemblyAction action) { throw null; }
		public bool HasAction (AssemblyDefinition assembly) { throw null; }

		public Dictionary<IMetadataTokenProvider, object> GetCustomAnnotations (object key) { throw null; }
	}
}
