// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public abstract class BaseSubStep : ISubStep
	{
		protected AnnotationStore Annotations => Context.Annotations;

		protected LinkContext Context { get; private set; }

		public abstract SubStepTargets Targets { get; }

		public virtual void Initialize (LinkContext context)
		{
			Context = context;
		}

		public virtual bool IsActiveFor (AssemblyDefinition assembly) => true;

		public virtual void ProcessAssembly (AssemblyDefinition assembly)
		{
		}

		public virtual void ProcessType (TypeDefinition type)
		{
		}

		public virtual void ProcessField (FieldDefinition field)
		{
		}

		public virtual void ProcessMethod (MethodDefinition method)
		{
		}

		public virtual void ProcessProperty (PropertyDefinition property)
		{
		}

		public virtual void ProcessEvent (EventDefinition @event)
		{
		}
	}
}