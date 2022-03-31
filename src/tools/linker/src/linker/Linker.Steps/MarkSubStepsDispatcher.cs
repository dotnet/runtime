// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{
	//
	// Dispatcher for SubSteps which only need to run on marked assemblies.
	// This simplifies the implementation of linker custom steps, in the same
	// way that SubStepsDispatcher does, but it implements IMarkHandler
	// and registers a callback that gets invoked during MarkStep when an
	// assembly gets marked.
	//
	public class MarkSubStepsDispatcher : IMarkHandler
	{
		readonly List<ISubStep> substeps;

		CategorizedSubSteps? categorized;
		CategorizedSubSteps Categorized {
			get {
				Debug.Assert (categorized.HasValue);
				return categorized.Value;
			}
		}

		public MarkSubStepsDispatcher (IEnumerable<ISubStep> subSteps)
		{
			substeps = new List<ISubStep> (subSteps);
		}

		public virtual void Initialize (LinkContext context, MarkContext markContext)
		{
			InitializeSubSteps (context);
			markContext.RegisterMarkAssemblyAction (assembly => BrowseAssembly (assembly));
		}

		static bool HasSubSteps (List<ISubStep> substeps) => substeps?.Count > 0;

		void BrowseAssembly (AssemblyDefinition assembly)
		{
			CategorizeSubSteps (assembly);

			if (HasSubSteps (Categorized.on_assemblies))
				DispatchAssembly (assembly);

			if (!ShouldDispatchTypes ())
				return;

			BrowseTypes (assembly.MainModule.Types);
		}

		bool ShouldDispatchTypes ()
		{
			return HasSubSteps (Categorized.on_types)
				|| HasSubSteps (Categorized.on_fields)
				|| HasSubSteps (Categorized.on_methods)
				|| HasSubSteps (Categorized.on_properties)
				|| HasSubSteps (Categorized.on_events);
		}

		void BrowseTypes (Collection<TypeDefinition> types)
		{
			foreach (TypeDefinition type in types) {
				DispatchType (type);

				if (type.HasFields && HasSubSteps (Categorized.on_fields)) {
					foreach (FieldDefinition field in type.Fields)
						DispatchField (field);
				}

				if (type.HasMethods && HasSubSteps (Categorized.on_methods)) {
					foreach (MethodDefinition method in type.Methods)
						DispatchMethod (method);
				}

				if (type.HasProperties && HasSubSteps (Categorized.on_properties)) {
					foreach (PropertyDefinition property in type.Properties)
						DispatchProperty (property);
				}

				if (type.HasEvents && HasSubSteps (Categorized.on_events)) {
					foreach (EventDefinition @event in type.Events)
						DispatchEvent (@event);
				}

				if (type.HasNestedTypes)
					BrowseTypes (type.NestedTypes);
			}
		}

		void DispatchAssembly (AssemblyDefinition assembly)
		{
			foreach (var substep in Categorized.on_assemblies) {
				substep.ProcessAssembly (assembly);
			}
		}

		void DispatchType (TypeDefinition type)
		{
			foreach (var substep in Categorized.on_types) {
				substep.ProcessType (type);
			}
		}

		void DispatchField (FieldDefinition field)
		{
			foreach (var substep in Categorized.on_fields) {
				substep.ProcessField (field);
			}
		}

		void DispatchMethod (MethodDefinition method)
		{
			foreach (var substep in Categorized.on_methods) {
				substep.ProcessMethod (method);
			}
		}

		void DispatchProperty (PropertyDefinition property)
		{
			foreach (var substep in Categorized.on_properties) {
				substep.ProcessProperty (property);
			}
		}

		void DispatchEvent (EventDefinition @event)
		{
			foreach (var substep in Categorized.on_events) {
				substep.ProcessEvent (@event);
			}
		}

		void InitializeSubSteps (LinkContext context)
		{
			foreach (var substep in substeps)
				substep.Initialize (context);
		}

		void CategorizeSubSteps (AssemblyDefinition assembly)
		{
			categorized = new CategorizedSubSteps {
				on_assemblies = new List<ISubStep> (),
				on_types = new List<ISubStep> (),
				on_fields = new List<ISubStep> (),
				on_methods = new List<ISubStep> (),
				on_properties = new List<ISubStep> (),
				on_events = new List<ISubStep> ()
			};

			foreach (var substep in substeps)
				CategorizeSubStep (substep, assembly);
		}

		void CategorizeSubStep (ISubStep substep, AssemblyDefinition assembly)
		{
			if (!substep.IsActiveFor (assembly))
				return;

			CategorizeTarget (substep, SubStepTargets.Assembly, Categorized.on_assemblies);
			CategorizeTarget (substep, SubStepTargets.Type, Categorized.on_types);
			CategorizeTarget (substep, SubStepTargets.Field, Categorized.on_fields);
			CategorizeTarget (substep, SubStepTargets.Method, Categorized.on_methods);
			CategorizeTarget (substep, SubStepTargets.Property, Categorized.on_properties);
			CategorizeTarget (substep, SubStepTargets.Event, Categorized.on_events);
		}

		static void CategorizeTarget (ISubStep substep, SubStepTargets target, List<ISubStep> list)
		{
			if (!Targets (substep, target))
				return;

			list.Add (substep);
		}

		static bool Targets (ISubStep substep, SubStepTargets target) => (substep.Targets & target) == target;
	}
}
