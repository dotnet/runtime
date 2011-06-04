using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public abstract class ApplyPreserveAttributeBase : BaseSubStep {

		protected abstract string PreserveAttribute { get; }

		public override SubStepTargets Targets {
			get {
				return SubStepTargets.Type
					| SubStepTargets.Field
					| SubStepTargets.Method
					| SubStepTargets.Property
					| SubStepTargets.Event;
			}
		}

		public override bool IsActiveFor (AssemblyDefinition assembly)
		{
			return !Profile.IsSdkAssembly (assembly) && Annotations.GetAction (assembly) == AssemblyAction.Link;
		}

		public override void ProcessType (TypeDefinition type)
		{
			TryApplyPreserveAttribute (type);
		}

		public override void ProcessField (FieldDefinition field)
		{
			var attribute = GetPreserveAttribute (field);
			if (attribute == null)
				return;

			Mark (field, attribute);
		}

		public override void ProcessMethod (MethodDefinition method)
		{
			MarkMethodIfPreserved (method);
		}

		public override void ProcessProperty (PropertyDefinition property)
		{
			var attribute = GetPreserveAttribute (property);
			if (attribute == null)
				return;

			MarkMethod (property.GetMethod, attribute);
			MarkMethod (property.SetMethod, attribute);
		}

		public override void ProcessEvent (EventDefinition @event)
		{
			var attribute = GetPreserveAttribute (@event);
			if (attribute == null)
				return;

			MarkMethod (@event.AddMethod, attribute);
			MarkMethod (@event.InvokeMethod, attribute);
			MarkMethod (@event.RemoveMethod, attribute);
		}

		void MarkMethodIfPreserved (MethodDefinition method)
		{
			var attribute = GetPreserveAttribute (method);
			if (attribute == null)
				return;

			MarkMethod (method, attribute);
		}

		void MarkMethod (MethodDefinition method, CustomAttribute preserve_attribute)
		{
			if (method == null)
				return;

			Mark (method, preserve_attribute);
			Annotations.SetAction (method, MethodAction.Parse);
		}

		void Mark (IMetadataTokenProvider provider, CustomAttribute preserve_attribute)
		{
			if (IsConditionalAttribute (preserve_attribute)) {
				PreserveConditional (provider);
				return;
			}

			PreserveUnconditional (provider);
		}

		void PreserveConditional (IMetadataTokenProvider provider)
		{
			var method = provider as MethodDefinition;
			if (method == null)
				return;

			Annotations.AddPreservedMethod (method.DeclaringType, method);
		}

		static bool IsConditionalAttribute (CustomAttribute attribute)
		{
			if (attribute == null)
				return false;

			foreach (var named_argument in attribute.Fields)
				if (named_argument.Name == "Conditional")
					return (bool) named_argument.Argument.Value;

			return false;
		}

		void PreserveUnconditional (IMetadataTokenProvider provider)
		{
			Annotations.Mark (provider);

			var member = provider as IMemberDefinition;
			if (member == null || member.DeclaringType == null)
				return;

			Mark (member.DeclaringType, null);
		}

		void TryApplyPreserveAttribute (TypeDefinition type)
		{
			var attribute = GetPreserveAttribute (type);
			if (attribute == null)
				return;

			Annotations.Mark (type);

			foreach (var named_argument in attribute.Fields)
				if (named_argument.Name == "AllMembers" && (bool) named_argument.Argument.Value)
					Annotations.SetPreserve (type, TypePreserve.All);
		}

		CustomAttribute GetPreserveAttribute (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return null;

			var attributes = provider.CustomAttributes;

			for (int i = 0; i < attributes.Count; i++) {
				var attribute = attributes [i];

				if (attribute.Constructor.DeclaringType.FullName != PreserveAttribute)
					continue;

				attributes.RemoveAt (i);
				return attribute;
			}

			return null;
		}
	}
}
