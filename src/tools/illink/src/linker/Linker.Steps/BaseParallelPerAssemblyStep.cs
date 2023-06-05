// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Mono.Cecil;

namespace Mono.Linker.Steps;

public abstract class BaseParallelPerAssemblyStep : IStep
{
	public void Process (LinkContext context)
	{
		Initialize(context);

		if (!ConditionToProcess (context))
			return;

		BeginProcess (context);

		ProcessAssemblies(context);

		EndProcess (context);
	}

	void ProcessAssemblies (LinkContext context)
	{
		var safeContext = new ParallelSafeLinkContext (context);
		Parallel.ForEach(context.GetAssemblies(), asm => ProcessAssembly(safeContext, asm));
	}

	protected virtual void Initialize (LinkContext context)
	{
	}

	protected virtual void ProcessAssembly (ParallelSafeLinkContext context, AssemblyDefinition assembly)
	{
	}

	protected virtual bool ConditionToProcess (LinkContext context)
	{
		return true;
	}

	protected virtual void BeginProcess (LinkContext context)
	{
	}

	protected virtual void EndProcess (LinkContext context)
	{
	}
}
