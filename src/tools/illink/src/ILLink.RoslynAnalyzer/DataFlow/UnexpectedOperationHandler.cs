// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis
{
	internal static class UnexpectedOperationHandler
	{
		// No-op in release builds, but fails in debug builds when
		// encountering an unexpected operation. InvalidOperation is skipped because
		// it is expected that any part of the control-flow graph may contain an
		// InvalidOperation for code that doesn't compile (for example, in an intermediate
		// state while editing).
		[Conditional ("DEBUG")]
		public static void Handle (IOperation operation)
		{
			// NoneOperation represents operations which are unimplemented by Roslyn
			// (don't have specific I*Operation types), such as pointer dereferences.
			if (operation.Kind is OperationKind.None)
				return;

			if (operation.Kind is OperationKind.Invalid)
				return;

			// It's also possible to hit a case where the operation is an unexpected operation kind,
			// but the code is in an invalid state where the unexpected operation is not IInvalidOperation, yet one
			// of its child operations is. For example:
			//
			//     a + = 3;
			//
			// This is represented as an assignment where the target is an IBinaryOperation (a +) whose right-hand side
			// is an IInvalidOperation. The assignment logic doesn't support assigning to a binary operation,
			// but this should still not fail.
			foreach (var descendant in operation.Descendants()) {
				if (descendant.Kind is OperationKind.Invalid)
					return;
			}

			// Throw on anything else as it means we need to implement support for it
			// but do not throw in Release builds as it means new Roslyn version could cause the analyzer to crash
			// which is not fixable by the user. The analyzer is not going to be 100% correct no
			// matter what we do so effectively ignoring constructs it doesn't understand is OK.
			// This is surfaced as warning AD0001 in Debug builds.
			throw new NotImplementedException ($"Unexpected operation type {operation.GetType ()}: {operation.Syntax.GetLocation ().GetLineSpan ()}");
		}
	}
}
