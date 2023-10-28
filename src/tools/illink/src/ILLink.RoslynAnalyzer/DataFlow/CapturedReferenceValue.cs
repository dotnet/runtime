// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	public readonly struct CapturedReferenceValue : IEquatable<CapturedReferenceValue>
	{
		public readonly IOperation Reference;

		public CapturedReferenceValue (IOperation operation)
		{
			switch (operation.Kind) {
			case OperationKind.PropertyReference:
			case OperationKind.EventReference:
			case OperationKind.LocalReference:
			case OperationKind.FieldReference:
			case OperationKind.ParameterReference:
			case OperationKind.ArrayElementReference:
			case OperationKind.InlineArrayAccess:
			case OperationKind.ImplicitIndexerReference:
				break;
			case OperationKind.None:
			case OperationKind.InstanceReference:
			case OperationKind.Invocation:
			case OperationKind.Invalid:
				// These will just be ignored when referenced later.
				break;
			default:
				// Assert on anything else as it means we need to implement support for it
				// but do not throw here as it means new Roslyn version could cause the analyzer to crash
				// which is not fixable by the user. The analyzer is not going to be 100% correct no matter what we do
				// so effectively ignoring constructs it doesn't understand is OK.
				Debug.Fail ($"{operation.GetType ()}: {operation.Syntax.GetLocation ().GetLineSpan ()}");
				break;
			}
			Reference = operation;
		}

		public bool Equals (CapturedReferenceValue other) => Reference == other.Reference;

		public override bool Equals (object obj)
			=> obj is CapturedReferenceValue inst && Equals (inst);

		public override int GetHashCode ()
			=> Reference?.GetHashCode () ?? 0;
	}
}
