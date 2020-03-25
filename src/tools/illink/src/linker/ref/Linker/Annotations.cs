// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker {

	public partial class AnnotationStore
	{
		internal AnnotationStore() {}
		public void Mark (IMetadataTokenProvider provider) { throw null; }
		public void Mark (CustomAttribute attribute) { throw null; }
	}
}
