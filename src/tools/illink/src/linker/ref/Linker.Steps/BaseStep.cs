// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker.Steps
{

	public abstract class BaseStep : IStep
	{
		public LinkContext Context { get { throw null; } }
		public AnnotationStore Annotations { get { throw null; } }
		public void Process (LinkContext context) { throw null; }
		protected virtual bool ConditionToProcess () { throw null; }
		protected virtual void Process () { throw null; }
		protected virtual void EndProcess () { throw null; }
		protected virtual void ProcessAssembly (AssemblyDefinition assembly) { throw null; }
	}
}
