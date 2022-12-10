// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public abstract class BaseSubStep : ISubStep
	{
		protected AnnotationStore Annotations => Context.Annotations;

		LinkContext? _context { get; set; }
		protected LinkContext Context {
			get {
				Debug.Assert (_context != null);
				return _context;
			}
		}

		public abstract SubStepTargets Targets { get; }

		public virtual void Initialize (LinkContext context)
		{
			_context = context;
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
