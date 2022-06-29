// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	public struct CapturedReferenceValue : IEquatable<CapturedReferenceValue>
	{
		public readonly IOperation? Reference;

		public CapturedReferenceValue (IOperation operation)
		{
			switch (operation.Kind) {
			case OperationKind.PropertyReference:
			case OperationKind.LocalReference:
			case OperationKind.FieldReference:
			case OperationKind.ParameterReference:
			case OperationKind.ArrayElementReference:
			case OperationKind.ImplicitIndexerReference:
				break;
			case OperationKind.None:
			case OperationKind.InstanceReference:
			case OperationKind.Invocation:
			case OperationKind.EventReference:
			case OperationKind.Invalid:
				// These will just be ignored when referenced later.
				break;
			default:
				throw new NotImplementedException (operation.Kind.ToString ());
			}
			Reference = operation;
		}

		public bool Equals (CapturedReferenceValue other) => Reference == other.Reference;
	}


	public struct CapturedReferenceLattice : ILattice<CapturedReferenceValue>
	{
		public CapturedReferenceValue Top => new CapturedReferenceValue ();

		public CapturedReferenceValue Meet (CapturedReferenceValue left, CapturedReferenceValue right)
		{
			if (left.Equals (right))
				return left;
			if (left.Reference == null)
				return right;
			if (right.Reference == null)
				return left;
			// Both non-null and different shouldn't happen.
			// We assume that a flow capture can capture only a single property.
			throw new InvalidOperationException ();
		}
	}
}