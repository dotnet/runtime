// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Adapted from https://github.com/dotnet/roslyn/
	public abstract class OperationWalker<TArgument, TResult> : OperationVisitor<TArgument, TResult>
	{
		private int _recursionDepth;

		private void VisitChildOperations (IOperation operation, TArgument argument)
		{
			// https://github.com/dotnet/roslyn/issues/49475 would let us use ChildOperations, a struct enumerable.
			foreach (var child in operation.Children)
				Visit (child, argument);
		}

		public override TResult? Visit (IOperation? operation, TArgument argument)
		{
			if (operation != null) {
				_recursionDepth++;
				try {
					StackGuard.EnsureSufficientExecutionStack (_recursionDepth);
					return operation.Accept (this, argument);
				} finally {
					_recursionDepth--;
				}
			}
			return default;
		}

		public override TResult? DefaultVisit (IOperation operation, TArgument argument)
		{
			VisitChildOperations (operation, argument);
			return default;
		}
	}
}