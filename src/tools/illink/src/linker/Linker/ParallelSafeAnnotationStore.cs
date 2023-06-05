// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker;

public class ParallelSafeAnnotationStore
{
	private readonly AnnotationStore _annotations;

	public ParallelSafeAnnotationStore (AnnotationStore annotations)
	{
		_annotations = annotations;
	}

	public bool ProcessSatelliteAssemblies => _annotations.ProcessSatelliteAssemblies;

	public AssemblyAction GetAction (AssemblyDefinition assembly) => _annotations.GetAction (assembly);
}
