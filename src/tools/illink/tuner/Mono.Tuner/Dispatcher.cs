using System;
using System.Collections;
using System.Collections.Generic;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	[Flags]
	public enum SubStepTargets {
		None = 0,

		Assembly = 1,
		Type = 2,
		Field = 4,
		Method = 8,
		Property = 16,
		Event = 32,
	}

	public interface ISubStep {

		SubStepTargets Targets { get; }

		void Initialize (LinkContext context);
		bool IsActiveFor (AssemblyDefinition assembly);

		void ProcessAssembly (AssemblyDefinition assembly);
		void ProcessType (TypeDefinition type);
		void ProcessField (FieldDefinition field);
		void ProcessMethod (MethodDefinition method);
		void ProcessProperty (PropertyDefinition property);
		void ProcessEvent (EventDefinition @event);
	}

	public abstract class BaseSubStep : ISubStep {

		protected LinkContext context;

		public AnnotationStore Annotations {
			get { return context.Annotations; }
		}

		public abstract SubStepTargets Targets { get; }

		void ISubStep.Initialize (LinkContext context)
		{
			this.context = context;
		}

		public virtual bool IsActiveFor (AssemblyDefinition assembly)
		{
			return true;
		}

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

	public class SubStepDispatcher : IStep, IEnumerable<ISubStep> {

		List<ISubStep> substeps = new List<ISubStep> ();

		List<ISubStep> on_assemblies;
		List<ISubStep> on_types;
		List<ISubStep> on_fields;
		List<ISubStep> on_methods;
		List<ISubStep> on_properties;
		List<ISubStep> on_events;

		public void Add (ISubStep substep)
		{
			substeps.Add (substep);
		}

		public void Process (LinkContext context)
		{
			InitializeSubSteps (context);

			BrowseAssemblies (context.GetAssemblies ());
		}

		static bool HasSubSteps (List<ISubStep> substeps)
		{
			return substeps != null && substeps.Count > 0;
		}

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

		void BrowseTypes (ICollection types)
		{
			foreach (TypeDefinition type in types) {
				DispatchType (type);

				if (type.HasFields && HasSubSteps (on_fields))
					BrowseFields (type.Fields);

				if (type.HasMethods && HasSubSteps (on_methods))
					BrowseMethods (type.Methods);

				if (type.HasProperties && HasSubSteps (on_properties))
					BrowseProperties (type.Properties);

				if (type.HasEvents && HasSubSteps (on_events))
					BrowseEvents (type.Events);

				if (type.HasNestedTypes)
					BrowseTypes (type.NestedTypes);
			}
		}

		void BrowseFields (ICollection fields)
		{
			foreach (FieldDefinition field in fields)
				DispatchField (field);
		}

		void BrowseMethods (ICollection methods)
		{
			foreach (MethodDefinition method in methods)
				DispatchMethod (method);
		}

		void BrowseProperties (ICollection properties)
		{
			foreach (PropertyDefinition property in properties)
				DispatchProperty (property);
		}

		void BrowseEvents (ICollection events)
		{
			foreach (EventDefinition @event in events)
				DispatchEvent (@event);
		}

		void DispatchAssembly (AssemblyDefinition assembly)
		{
			foreach (var substep in on_assemblies)
				substep.ProcessAssembly (assembly);
		}

		void DispatchType (TypeDefinition type)
		{
			foreach (var substep in on_types)
				substep.ProcessType (type);
		}

		void DispatchField (FieldDefinition field)
		{
			foreach (var substep in on_fields)
				substep.ProcessField (field);
		}

		void DispatchMethod (MethodDefinition method)
		{
			foreach (var substep in on_methods)
				substep.ProcessMethod (method);
		}

		void DispatchProperty (PropertyDefinition property)
		{
			foreach (var substep in on_properties)
				substep.ProcessProperty (property);
		}

		void DispatchEvent (EventDefinition @event)
		{
			foreach (var substep in on_events)
				substep.ProcessEvent (@event);
		}

		void InitializeSubSteps (LinkContext context)
		{
			foreach (var substep in substeps)
				substep.Initialize (context);
		}

		void CategorizeSubSteps (AssemblyDefinition assembly)
		{
			on_assemblies = null;
			on_types = null;
			on_fields = null;
			on_methods = null;
			on_properties = null;
			on_events = null;

			foreach (var substep in substeps)
				CategorizeSubStep (substep, assembly);
		}

		void CategorizeSubStep (ISubStep substep, AssemblyDefinition assembly)
		{
			if (!substep.IsActiveFor (assembly))
				return;

			CategorizeTarget (substep, SubStepTargets.Assembly, ref on_assemblies);
			CategorizeTarget (substep, SubStepTargets.Type, ref on_types);
			CategorizeTarget (substep, SubStepTargets.Field, ref on_fields);
			CategorizeTarget (substep, SubStepTargets.Method, ref on_methods);
			CategorizeTarget (substep, SubStepTargets.Property, ref on_properties);
			CategorizeTarget (substep, SubStepTargets.Event, ref on_events);
		}

		static void CategorizeTarget (ISubStep substep, SubStepTargets target, ref List<ISubStep> list)
		{
			if (!Targets (substep, target))
				return;

			if (list == null)
				list = new List<ISubStep> ();

			list.Add (substep);
		}

		static bool Targets (ISubStep substep, SubStepTargets target)
		{
			return (substep.Targets & target) == target;
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		public IEnumerator<ISubStep> GetEnumerator ()
		{
			return substeps.GetEnumerator ();
		}
	}
}
