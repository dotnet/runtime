using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Linker.Dataflow
{
	class AggregateFlowAnnotationSource : IFlowAnnotationSource
	{
		private readonly List<IFlowAnnotationSource> _sources;

		public AggregateFlowAnnotationSource(IEnumerable<IFlowAnnotationSource> sources)
		{
			_sources = new List<IFlowAnnotationSource> (sources);
		}

		public DynamicallyAccessedMemberKinds GetFieldAnnotation (FieldDefinition field)
		{
			return _sources.Aggregate<IFlowAnnotationSource, DynamicallyAccessedMemberKinds> (0, (r, s) => r | s.GetFieldAnnotation (field));
		}

		public DynamicallyAccessedMemberKinds GetParameterAnnotation (MethodDefinition method, int index)
		{
			return _sources.Aggregate<IFlowAnnotationSource, DynamicallyAccessedMemberKinds> (0, (r, s) => r | s.GetParameterAnnotation (method, index));
		}

		public DynamicallyAccessedMemberKinds GetPropertyAnnotation (PropertyDefinition property)
		{
			return _sources.Aggregate<IFlowAnnotationSource, DynamicallyAccessedMemberKinds> (0, (r, s) => r | s.GetPropertyAnnotation (property));
		}

		public DynamicallyAccessedMemberKinds GetReturnParameterAnnotation (MethodDefinition method)
		{
			return _sources.Aggregate<IFlowAnnotationSource, DynamicallyAccessedMemberKinds> (0, (r, s) => r | s.GetReturnParameterAnnotation (method));
		}
	}
}
