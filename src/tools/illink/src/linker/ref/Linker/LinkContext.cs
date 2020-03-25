// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker {

	public class LinkContext
	{
		internal LinkContext () { }
		public AnnotationStore Annotations { get { throw null; } }
		public TypeDefinition GetType (string fullName) { throw null; }
	}
}
