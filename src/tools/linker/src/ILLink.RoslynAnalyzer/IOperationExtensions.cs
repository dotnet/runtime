// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	// Copied from https://github.com/dotnet/roslyn/blob/9c6d864baca08d7572871701ab583cec18279426/src/Compilers/Core/Portable/Operations/OperationExtensions.cs
	internal static partial class IOperationExtensions
	{
		/// <summary>
		/// Returns the <see cref="ValueUsageInfo"/> for the given operation.
		/// This extension can be removed once https://github.com/dotnet/roslyn/issues/25057 is implemented.
		/// </summary>
		public static ValueUsageInfo GetValueUsageInfo (this IOperation operation, ISymbol containingSymbol)
		{
			/*
            |    code                  | Read | Write | ReadableRef | WritableRef | NonReadWriteRef |
            | x.Prop = 1               |      |  ✔️   |             |             |                 |
            | x.Prop += 1              |  ✔️  |  ✔️   |             |             |                 |
            | x.Prop++                 |  ✔️  |  ✔️   |             |             |                 |
            | Foo(x.Prop)              |  ✔️  |       |             |             |                 |
            | Foo(x.Prop),             |      |       |     ✔️      |             |                 |
               where void Foo(in T v)
            | Foo(out x.Prop)          |      |       |             |     ✔️      |                 |
            | Foo(ref x.Prop)          |      |       |     ✔️      |     ✔️      |                 |
            | nameof(x)                |      |       |             |             |       ✔️        | ️
            | sizeof(x)                |      |       |             |             |       ✔️        | ️
            | typeof(x)                |      |       |             |             |       ✔️        | ️
            | out var x                |      |  ✔️   |             |             |                 | ️
            | case X x:                |      |  ✔️   |             |             |                 | ️
            | obj is X x               |      |  ✔️   |             |             |                 |
            | ref var x =              |      |       |     ✔️      |     ✔️      |                 |
            | ref readonly var x =     |      |       |     ✔️      |             |                 |

            */
			if (operation is ILocalReferenceOperation localReference &&
				localReference.IsDeclaration &&
				!localReference.IsImplicit) // Workaround for https://github.com/dotnet/roslyn/issues/30753
			{
				// Declaration expression is a definition (write) for the declared local.
				return ValueUsageInfo.Write;
			} else if (operation is IDeclarationPatternOperation) {
				while (operation.Parent is IBinaryPatternOperation ||
					   operation.Parent is INegatedPatternOperation ||
					   operation.Parent is IRelationalPatternOperation) {
					operation = operation.Parent;
				}

				switch (operation.Parent) {
				case IPatternCaseClauseOperation:
					// A declaration pattern within a pattern case clause is a
					// write for the declared local.
					// For example, 'x' is defined and assigned the value from 'obj' below:
					//      switch (obj)
					//      {
					//          case X x:
					//
					return ValueUsageInfo.Write;

				case IRecursivePatternOperation:
					// A declaration pattern within a recursive pattern is a
					// write for the declared local.
					// For example, 'x' is defined and assigned the value from 'obj' below:
					//      (obj) switch
					//      {
					//          (X x) => ...
					//      };
					//
					return ValueUsageInfo.Write;

				case ISwitchExpressionArmOperation:
					// A declaration pattern within a switch expression arm is a
					// write for the declared local.
					// For example, 'x' is defined and assigned the value from 'obj' below:
					//      obj switch
					//      {
					//          X x => ...
					//
					return ValueUsageInfo.Write;

				case IIsPatternOperation:
					// A declaration pattern within an is pattern is a
					// write for the declared local.
					// For example, 'x' is defined and assigned the value from 'obj' below:
					//      if (obj is X x)
					//
					return ValueUsageInfo.Write;

				case IPropertySubpatternOperation:
					// A declaration pattern within a property sub-pattern is a
					// write for the declared local.
					// For example, 'x' is defined and assigned the value from 'obj.Property' below:
					//      if (obj is { Property : int x })
					//
					return ValueUsageInfo.Write;

				default:
					Debug.Fail ("Unhandled declaration pattern context");

					// Conservatively assume read/write.
					return ValueUsageInfo.ReadWrite;
				}
			}

			if (operation.Parent is IAssignmentOperation assignmentOperation &&
				assignmentOperation.Target == operation) {
				return operation.Parent.IsAnyCompoundAssignment ()
					? ValueUsageInfo.ReadWrite
					: ValueUsageInfo.Write;
			} else if (operation.Parent is IIncrementOrDecrementOperation) {
				return ValueUsageInfo.ReadWrite;
			} else if (operation.Parent is IParenthesizedOperation parenthesizedOperation) {
				// Note: IParenthesizedOperation is specific to VB, where the parens cause a copy, so this cannot be classified as a write.
				Debug.Assert (parenthesizedOperation.Language == LanguageNames.VisualBasic);

				return parenthesizedOperation.GetValueUsageInfo (containingSymbol) &
					~(ValueUsageInfo.Write | ValueUsageInfo.Reference);
			} else if (operation.Parent is INameOfOperation ||
					   operation.Parent is ITypeOfOperation ||
					   operation.Parent is ISizeOfOperation) {
				return ValueUsageInfo.Name;
			} else if (operation.Parent is IArgumentOperation argumentOperation) {
				switch (argumentOperation.Parameter?.RefKind) {
				case RefKind.RefReadOnly:
					return ValueUsageInfo.ReadableReference;

				case RefKind.Out:
					return ValueUsageInfo.WritableReference;

				case RefKind.Ref:
					return ValueUsageInfo.ReadableWritableReference;

				default:
					return ValueUsageInfo.Read;
				}
			} else if (operation.Parent is IReturnOperation returnOperation) {
				return returnOperation.GetRefKind (containingSymbol) switch {
					RefKind.RefReadOnly => ValueUsageInfo.ReadableReference,
					RefKind.Ref => ValueUsageInfo.ReadableWritableReference,
					_ => ValueUsageInfo.Read,
				};
			} else if (operation.Parent is IConditionalOperation conditionalOperation) {
				if (operation == conditionalOperation.WhenTrue
					|| operation == conditionalOperation.WhenFalse) {
					return GetValueUsageInfo (conditionalOperation, containingSymbol);
				} else {
					return ValueUsageInfo.Read;
				}
			} else if (operation.Parent is IReDimClauseOperation reDimClauseOperation &&
				  reDimClauseOperation.Operand == operation) {
				return (reDimClauseOperation.Parent as IReDimOperation)?.Preserve == true
					? ValueUsageInfo.ReadWrite
					: ValueUsageInfo.Write;
			} else if (operation.Parent is IDeclarationExpressionOperation declarationExpression) {
				return declarationExpression.GetValueUsageInfo (containingSymbol);
			} else if (operation.IsInLeftOfDeconstructionAssignment (out _)) {
				return ValueUsageInfo.Write;
			} else if (operation.Parent is IVariableInitializerOperation variableInitializerOperation) {
				if (variableInitializerOperation.Parent is IVariableDeclaratorOperation variableDeclaratorOperation) {
					switch (variableDeclaratorOperation.Symbol.RefKind) {
					case RefKind.Ref:
						return ValueUsageInfo.ReadableWritableReference;

					case RefKind.RefReadOnly:
						return ValueUsageInfo.ReadableReference;
					}
				}
			}

			return ValueUsageInfo.Read;
		}

		public static RefKind GetRefKind (this IReturnOperation operation, ISymbol containingSymbol)
		{
			var containingMethod = TryGetContainingAnonymousFunctionOrLocalFunction (operation) ?? (containingSymbol as IMethodSymbol);
			return containingMethod?.RefKind ?? RefKind.None;
		}

		public static IMethodSymbol? TryGetContainingAnonymousFunctionOrLocalFunction (this IOperation? operation)
		{
			operation = operation?.Parent;
			while (operation != null) {
				switch (operation.Kind) {
				case OperationKind.AnonymousFunction:
					return ((IAnonymousFunctionOperation) operation).Symbol;

				case OperationKind.LocalFunction:
					return ((ILocalFunctionOperation) operation).Symbol;
				}

				operation = operation.Parent;
			}

			return null;
		}

		public static bool IsInLeftOfDeconstructionAssignment (this IOperation operation, out IDeconstructionAssignmentOperation? deconstructionAssignment)
		{
			deconstructionAssignment = null;

			var previousOperation = operation;
			var current = operation.Parent;

			while (current != null) {
				switch (current.Kind) {
				case OperationKind.DeconstructionAssignment:
					deconstructionAssignment = (IDeconstructionAssignmentOperation) current;
					return deconstructionAssignment.Target == previousOperation;

				case OperationKind.Tuple:
				case OperationKind.Conversion:
				case OperationKind.Parenthesized:
					previousOperation = current;
					current = current.Parent;
					continue;

				default:
					return false;
				}
			}

			return false;
		}

		/// <summary>
		/// Retursn true if the given operation is a regular compound assignment,
		/// i.e. <see cref="ICompoundAssignmentOperation"/> such as <code>a += b</code>,
		/// or a special null coalescing compoud assignment, i.e. <see cref="ICoalesceAssignmentOperation"/>
		/// such as <code>a ??= b</code>.
		/// </summary>
		public static bool IsAnyCompoundAssignment (this IOperation operation)
		{
			switch (operation) {
			case ICompoundAssignmentOperation:
			case ICoalesceAssignmentOperation:
				return true;

			default:
				return false;
			}
		}

		/// <summary>
		/// Finds the symbol of the caller to the current operation, helps to find out the symbol in cases where the operation passes
		/// through a lambda or a local function.
		/// </summary>
		/// <param name="operation">The operation to find the symbol for.</param>
		/// <param name="owningSymbol">The owning symbol of the entire operation context.</param>
		/// <returns>The symbol of the caller to the operation</returns>
		public static ISymbol FindContainingSymbol (this IOperation operation, ISymbol owningSymbol)
		{
			var parent = operation.Parent;
			while (parent is not null) {
				switch (parent) {
				case IAnonymousFunctionOperation lambda:
					return lambda.Symbol;

				case ILocalFunctionOperation local:
					return local.Symbol;

				case IMethodBodyBaseOperation:
				case IPropertyReferenceOperation:
				case IFieldReferenceOperation:
				case IEventReferenceOperation:
					return owningSymbol;

				default:
					parent = parent.Parent;
					break;
				}
			}

			return owningSymbol;
		}
	}
}
