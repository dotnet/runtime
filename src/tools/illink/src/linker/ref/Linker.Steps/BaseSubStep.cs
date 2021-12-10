// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public abstract class BaseSubStep : ISubStep
	{
		protected static AnnotationStore Annotations { get => throw null; }

		protected static LinkContext Context { get => throw null; }

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