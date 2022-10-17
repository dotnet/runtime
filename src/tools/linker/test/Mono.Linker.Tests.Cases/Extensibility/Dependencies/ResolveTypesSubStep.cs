using System;
using Mono.Cecil;
using Mono.Linker.Steps;

class ResolveTypesSubStep : BaseStep
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

		if (type.Name == "TypeWithFields") {
			foreach (var field in type.Fields)
				ProcessField (field);
		}
	}

	public void ProcessField (FieldDefinition field)
	{
		if (field.FieldType.Resolve () == null)
			throw new Exception($"Unresolved field type {field.FieldType} for field {field}!");
	}
}
