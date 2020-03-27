using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	interface IFlowAnnotationSource
	{
		DynamicallyAccessedMemberKinds GetPropertyAnnotation (PropertyDefinition property);

		// Index refers to the index in the formal parameter list (i.e. there's no index for `this` on instance methods)
		DynamicallyAccessedMemberKinds GetParameterAnnotation (MethodDefinition method, int index);

		DynamicallyAccessedMemberKinds GetReturnParameterAnnotation (MethodDefinition method);

		DynamicallyAccessedMemberKinds GetFieldAnnotation (FieldDefinition field);
	}
}
