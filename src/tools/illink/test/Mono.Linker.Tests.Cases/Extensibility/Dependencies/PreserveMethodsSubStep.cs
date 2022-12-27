using System;
using Mono.Cecil;
using Mono.Linker.Steps;

class PreserveMethodsSubStep : BaseStep
{

	protected override void Process ()
	{
		foreach (var assembly in Context.GetAssemblies ()) {
			foreach (var type in assembly.MainModule.Types)
				ProcessType (type);
		}
	}

	void ProcessType (TypeDefinition type)
	{
		if (type.HasNestedTypes) {
			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}


		foreach (var method in type.Methods) {
			if (method.Name == "PreservedForType")
				Annotations.AddPreservedMethod (type, method);

			ProcessMethod (method);
		}
	}

	public void ProcessMethod (MethodDefinition method)
	{
		if (method.Name == "MarkedMethod")
			Annotations.Mark (method);

		foreach (var m in method.DeclaringType.Methods) {
			if (m.Name == $"PreservedForMethod_{method.Name}")
				Annotations.AddPreservedMethod (method, m);
		}
	}
}
