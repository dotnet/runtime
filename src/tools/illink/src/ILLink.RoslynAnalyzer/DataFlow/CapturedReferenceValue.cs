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
			case OperationKind.InstanceReference:
			case OperationKind.Invocation:
				// These will just be ignored when referenced later.
				break;
			default:
				UnexpectedOperationHandler.Handle (operation);
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
