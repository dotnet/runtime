using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class RemoveSecurity : BaseSubStep {

		public override SubStepTargets Targets {
			get {
				return SubStepTargets.Assembly
					| SubStepTargets.Type
					| SubStepTargets.Method;
			}
		}

		public override bool IsActiveFor (AssemblyDefinition assembly)
		{
			return Annotations.GetAction (assembly) == AssemblyAction.Link;
		}

		public override void ProcessAssembly (AssemblyDefinition assembly)
		{
			ProcessSecurityProvider (assembly);
		}

		public override void ProcessType (TypeDefinition type)
		{
			ProcessSecurityProvider (type);
		}

		public override void ProcessMethod (MethodDefinition method)
		{
			ProcessSecurityProvider (method);
		}

		static void ProcessSecurityProvider (ISecurityDeclarationProvider provider)
		{
			if (!provider.HasSecurityDeclarations)
				return;

			provider.SecurityDeclarations.Clear ();
		}
	}
}
