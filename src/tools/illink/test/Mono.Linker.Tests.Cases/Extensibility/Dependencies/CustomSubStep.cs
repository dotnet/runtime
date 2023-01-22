using System;
using Mono.Cecil;
using Mono.Linker.Steps;

class CustomSubStep : BaseSubStep
{
	public override SubStepTargets Targets => SubStepTargets.Type;

	public override void ProcessType (TypeDefinition type)
	{
		Annotations.Mark (type);
	}
}
