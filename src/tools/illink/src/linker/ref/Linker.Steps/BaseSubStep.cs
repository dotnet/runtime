// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public abstract class BaseSubStep : ISubStep
	{
		protected AnnotationStore Annotations { get => throw null; }

		protected LinkContext Context { get => throw null; }

		public abstract SubStepTargets Targets { get; }

		public virtual void Initialize (LinkContext context) => throw null;
		public virtual bool IsActiveFor (AssemblyDefinition assembly) => throw null;
		public virtual void ProcessAssembly (AssemblyDefinition assembly) => throw null;
		public virtual void ProcessType (TypeDefinition type) => throw null;
		public virtual void ProcessField (FieldDefinition field) => throw null;
		public virtual void ProcessMethod (MethodDefinition method) => throw null;
		public virtual void ProcessProperty (PropertyDefinition property) => throw null;
		public virtual void ProcessEvent (EventDefinition @event) => throw null;
	}
}
