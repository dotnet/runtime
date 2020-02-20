using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Tuner;

using Mono.Cecil;

namespace Mono.Tuner {

	public abstract class RemoveAttributesBase : BaseSubStep {

		public override SubStepTargets Targets {
			get {
				return SubStepTargets.Assembly
					| SubStepTargets.Type
					| SubStepTargets.Field
					| SubStepTargets.Method
					| SubStepTargets.Property
					| SubStepTargets.Event;
			}
		}

		public override bool IsActiveFor (AssemblyDefinition assembly)
		{
			return Annotations.GetAction (assembly) == AssemblyAction.Link;
		}

		public override void ProcessAssembly (AssemblyDefinition assembly)
		{
			ProcessAttributeProvider (assembly);
			ProcessAttributeProvider (assembly.MainModule);
		}

		public override void ProcessType (TypeDefinition type)
		{
			ProcessAttributeProvider (type);
			ProcessGenericParameterProvider (type);

			if (type.HasInterfaces)
				ProcessAttributeProviderCollection (type.Interfaces);
		}

		void ProcessAttributeProviderCollection (IList list)
		{
			for (int i = 0; i < list.Count; i++)
				ProcessAttributeProvider ((ICustomAttributeProvider) list [i]);
		}

		public override void ProcessField (FieldDefinition field)
		{
			ProcessAttributeProvider (field);
		}

		void ProcessGenericParameterProvider (IGenericParameterProvider provider)
		{
			if (!provider.HasGenericParameters)
				return;

			foreach (var parameter in provider.GenericParameters) {
				ProcessAttributeProvider (parameter);

				if (!parameter.HasConstraints)
					continue;

				foreach (var constraint in parameter.Constraints)
					ProcessAttributeProvider (constraint);
			}
		}

		public override void ProcessMethod (MethodDefinition method)
		{
			ProcessMethodAttributeProvider (method);
		}

		void ProcessMethodAttributeProvider (MethodDefinition method)
		{
			ProcessAttributeProvider (method);
			ProcessAttributeProvider (method.MethodReturnType);

			if (method.HasParameters)
				ProcessAttributeProviderCollection (method.Parameters);

			ProcessGenericParameterProvider (method);
		}

		public override void ProcessProperty (PropertyDefinition property)
		{
			ProcessAttributeProvider (property);
		}

		public override void ProcessEvent (EventDefinition @event)
		{
			ProcessAttributeProvider (@event);
		}

		void ProcessAttributeProvider (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			for (int i = 0; i < provider.CustomAttributes.Count; i++) {
				var attrib = provider.CustomAttributes [i];
				if (!IsRemovedAttribute (attrib))
					continue;

				WillRemoveAttribute (provider, attrib);
				provider.CustomAttributes.RemoveAt (i--);
			}
		}

		protected abstract bool IsRemovedAttribute (CustomAttribute attribute);
		protected virtual void WillRemoveAttribute (ICustomAttributeProvider provider, CustomAttribute attribute) { }
	}
}
