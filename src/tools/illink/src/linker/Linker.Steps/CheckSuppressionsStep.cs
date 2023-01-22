// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class CheckSuppressionsStep : BaseSubStep
	{
		public override SubStepTargets Targets {
			get {
				return SubStepTargets.Type |
					SubStepTargets.Field |
					SubStepTargets.Method |
					SubStepTargets.Property |
					SubStepTargets.Event;
			}
		}

		public override bool IsActiveFor (AssemblyDefinition assembly)
		{
			// Only process assemblies which went through marking. 
			// The code relies on MarkStep to identify the useful suppressions.
			// Assemblies which didn't go through marking would not produce any warnings and thus would report all suppressions as redundant.
			var assemblyAction = Annotations.GetAction (assembly);
			return assemblyAction == AssemblyAction.Link || assemblyAction == AssemblyAction.Copy;
		}

		public override void ProcessType (TypeDefinition type)
		{
			Context.Suppressions.GatherSuppressions (type);
		}

		public override void ProcessField (FieldDefinition field)
		{
			Context.Suppressions.GatherSuppressions (field);
		}

		public override void ProcessMethod (MethodDefinition method)
		{
			if (Context.Annotations.GetAction (method) != MethodAction.ConvertToThrow)
				Context.Suppressions.GatherSuppressions (method);
		}

		public override void ProcessProperty (PropertyDefinition property)
		{
			Context.Suppressions.GatherSuppressions (property);
		}

		public override void ProcessEvent (EventDefinition @event)
		{
			Context.Suppressions.GatherSuppressions (@event);
		}
	}
}
