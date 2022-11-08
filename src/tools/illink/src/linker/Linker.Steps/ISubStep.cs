// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public interface ISubStep
	{
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
}
