// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;

namespace Mono.Linker.Steps
{

	public abstract class BaseStep : IStep
	{
		public static LinkContext Context { get { throw null; } }
		public static AnnotationStore Annotations { get { throw null; } }
		public void Process (LinkContext context) { throw null; }
		protected virtual bool ConditionToProcess () { throw null; }
		protected virtual void Process () { throw null; }
		protected virtual void EndProcess () { throw null; }
		protected virtual void ProcessAssembly (AssemblyDefinition assembly) { throw null; }
	}
}
