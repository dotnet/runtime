// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker;

public class ParallelSafeLinkContext
{
	private readonly LinkContext _context;
	private readonly ParallelSafeAnnotationStore _annotations;
	private readonly object _logLock = new object ();

	public ParallelSafeLinkContext (LinkContext context)
	{
		_context = context;
		_annotations = new ParallelSafeAnnotationStore (context.Annotations);
	}

	public ParallelSafeAnnotationStore Annotations => _annotations;

	public bool DeterministicOutput => _context.DeterministicOutput;
	public bool LinkSymbols => _context.LinkSymbols;

	public string OutputDirectory => _context.OutputDirectory;

	public string GetAssemblyLocation (AssemblyDefinition assembly) => _context.GetAssemblyLocation (assembly);

	public void LogMessage (string message)
	{
		// Logging is not currently thread safe, so we need to lock
		lock (_logLock)
			_context.LogMessage (message);
	}
}
