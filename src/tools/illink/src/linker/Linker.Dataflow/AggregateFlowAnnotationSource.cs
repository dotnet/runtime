// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	class AggregateFlowAnnotationSource : IFlowAnnotationSource
	{
		private readonly List<IFlowAnnotationSource> _sources;

		public AggregateFlowAnnotationSource (IEnumerable<IFlowAnnotationSource> sources)
		{
			_sources = new List<IFlowAnnotationSource> (sources);
		}

		public DynamicallyAccessedMemberTypes GetFieldAnnotation (FieldDefinition field)
		{
			return _sources.Aggregate (DynamicallyAccessedMemberTypes.None, (r, s) => r | s.GetFieldAnnotation (field));
		}

		public DynamicallyAccessedMemberTypes GetParameterAnnotation (MethodDefinition method, int index)
		{
			return _sources.Aggregate (DynamicallyAccessedMemberTypes.None, (r, s) => r | s.GetParameterAnnotation (method, index));
		}

		public DynamicallyAccessedMemberTypes GetPropertyAnnotation (PropertyDefinition property)
		{
			return _sources.Aggregate (DynamicallyAccessedMemberTypes.None, (r, s) => r | s.GetPropertyAnnotation (property));
		}

		public DynamicallyAccessedMemberTypes GetReturnParameterAnnotation (MethodDefinition method)
		{
			return _sources.Aggregate (DynamicallyAccessedMemberTypes.None, (r, s) => r | s.GetReturnParameterAnnotation (method));
		}

		public DynamicallyAccessedMemberTypes GetThisParameterAnnotation (MethodDefinition method)
		{
			return _sources.Aggregate (DynamicallyAccessedMemberTypes.None, (r, s) => r | s.GetThisParameterAnnotation (method));
		}
	}
}
