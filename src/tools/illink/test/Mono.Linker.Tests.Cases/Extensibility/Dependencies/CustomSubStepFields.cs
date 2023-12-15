using System;
using Mono.Cecil;
using Mono.Linker.Steps;

class CustomSubStep : BaseSubStep
{
	public override SubStepTargets Targets => SubStepTargets.Field;

	public override void ProcessField (FieldDefinition field)
	{
		Annotations.Mark (field);
	}
}
