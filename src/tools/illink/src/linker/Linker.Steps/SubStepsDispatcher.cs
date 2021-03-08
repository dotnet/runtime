// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{
	//
	// Generic steps dispatcher is intended to by used by custom linker step which
	// consist of multiple steps. It simplifies their implementation as well as the
	// way how to hook them into the pipeline of existing steps.
	//
	public abstract class SubStepsDispatcher : IStep
	{
		readonly List<ISubStep> substeps;

		List<ISubStep> on_assemblies;
		List<ISubStep> on_types;
		List<ISubStep> on_fields;
		List<ISubStep> on_methods;
		List<ISubStep> on_properties;
		List<ISubStep> on_events;

		protected SubStepsDispatcher ()
		{
			substeps = new List<ISubStep> ();
		}

		protected SubStepsDispatcher (IEnumerable<ISubStep> subSteps)
		{
			substeps = new List<ISubStep> (subSteps);
		}

		public void Add (ISubStep substep)
		{
			substeps.Add (substep);
		}

		void IStep.Process (LinkContext context)
		{
			InitializeSubSteps (context);

			BrowseAssemblies (context.GetAssemblies ());
		}

		static bool HasSubSteps (List<ISubStep> substeps) => substeps?.Count > 0;

		void BrowseAssemblies (IEnumerable<AssemblyDefinition> assemblies)
		{
			foreach (var assembly in assemblies) {
				CategorizeSubSteps (assembly);

				if (HasSubSteps (on_assemblies))
					DispatchAssembly (assembly);

				if (!ShouldDispatchTypes ())
					continue;

				BrowseTypes (assembly.MainModule.Types);
			}
		}

		bool ShouldDispatchTypes ()
		{
			return HasSubSteps (on_types)
				|| HasSubSteps (on_fields)
				|| HasSubSteps (on_methods)
				|| HasSubSteps (on_properties)
				|| HasSubSteps (on_events);
		}

		void BrowseTypes (Collection<TypeDefinition> types)
		{
			foreach (TypeDefinition type in types) {
				DispatchType (type);

				if (type.HasFields && HasSubSteps (on_fields)) {
					foreach (FieldDefinition field in type.Fields)
						DispatchField (field);
				}

				if (type.HasMethods && HasSubSteps (on_methods)) {
					foreach (MethodDefinition method in type.Methods)
						DispatchMethod (method);
				}

				if (type.HasProperties && HasSubSteps (on_properties)) {
					foreach (PropertyDefinition property in type.Properties)
						DispatchProperty (property);
				}

				if (type.HasEvents && HasSubSteps (on_events)) {
					foreach (EventDefinition @event in type.Events)
						DispatchEvent (@event);
				}

				if (type.HasNestedTypes)
					BrowseTypes (type.NestedTypes);
			}
		}

		void DispatchAssembly (AssemblyDefinition assembly)
		{
			foreach (var substep in on_assemblies) {
				substep.ProcessAssembly (assembly);
			}
		}

		void DispatchType (TypeDefinition type)
		{
			foreach (var substep in on_types) {
				substep.ProcessType (type);
			}
		}

		void DispatchField (FieldDefinition field)
		{
			foreach (var substep in on_fields) {
				substep.ProcessField (field);
			}
		}

		void DispatchMethod (MethodDefinition method)
		{
			foreach (var substep in on_methods) {
				substep.ProcessMethod (method);
			}
		}

		void DispatchProperty (PropertyDefinition property)
		{
			foreach (var substep in on_properties) {
				substep.ProcessProperty (property);
			}
		}

		void DispatchEvent (EventDefinition @event)
		{
			foreach (var substep in on_events) {
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
			on_assemblies = new List<ISubStep> ();
			on_types = new List<ISubStep> ();
			on_fields = new List<ISubStep> ();
			on_methods = new List<ISubStep> ();
			on_properties = new List<ISubStep> ();
			on_events = new List<ISubStep> ();

			foreach (var substep in substeps)
				CategorizeSubStep (substep, assembly);
		}

		void CategorizeSubStep (ISubStep substep, AssemblyDefinition assembly)
		{
			if (!substep.IsActiveFor (assembly))
				return;

			CategorizeTarget (substep, SubStepTargets.Assembly, on_assemblies);
			CategorizeTarget (substep, SubStepTargets.Type, on_types);
			CategorizeTarget (substep, SubStepTargets.Field, on_fields);
			CategorizeTarget (substep, SubStepTargets.Method, on_methods);
			CategorizeTarget (substep, SubStepTargets.Property, on_properties);
			CategorizeTarget (substep, SubStepTargets.Event, on_events);
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