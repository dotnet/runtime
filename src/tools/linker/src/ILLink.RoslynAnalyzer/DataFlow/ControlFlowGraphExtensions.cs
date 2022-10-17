// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Copied from https://github.com/dotnet/roslyn/blob/fdd40b21d59c13e8fa6c718c7aaf9d50634da754/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/ControlFlowGraphExtensions.cs
	internal static partial class ControlFlowGraphExtensions
	{
		public static BasicBlock EntryBlock (this ControlFlowGraph cfg)
		{
			var firstBlock = cfg.Blocks[0];
			Debug.Assert (firstBlock.Kind == BasicBlockKind.Entry);
			return firstBlock;
		}

		public static BasicBlock ExitBlock (this ControlFlowGraph cfg)
		{
			var lastBlock = cfg.Blocks.Last ();
			Debug.Assert (lastBlock.Kind == BasicBlockKind.Exit);
			return lastBlock;
		}

		public static IEnumerable<IOperation> DescendantOperations (this ControlFlowGraph cfg)
			=> cfg.Blocks.SelectMany (b => b.DescendantOperations ());

		public static IEnumerable<T> DescendantOperations<T> (this ControlFlowGraph cfg, OperationKind operationKind)
			where T : IOperation
			=> cfg.DescendantOperations ().Where (d => d?.Kind == operationKind).Cast<T> ();
	}
}
