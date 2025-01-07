// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Wrapper type which holds a method (or local function or lambda method),
	// along with its control flow graph. It implements IEquatable for the method.
	public readonly struct MethodBodyValue : IEquatable<MethodBodyValue>
	{
		// Usually an IMethodSymbol, but may also be an IFieldSymbol or IPropertySymbol
		// for field initializers.
		public ISymbol OwningSymbol { get; }

		public ControlFlowGraph ControlFlowGraph { get; }

		public MethodBodyValue (ISymbol owningSymbol, ControlFlowGraph cfg)
		{
			Debug.Assert (owningSymbol is (IMethodSymbol or IFieldSymbol or IPropertySymbol));
			OwningSymbol = owningSymbol;
			ControlFlowGraph = cfg;
		}

		public bool Equals (MethodBodyValue other)
		{
			if (!ReferenceEquals (OwningSymbol, other.OwningSymbol))
				return false;

			Debug.Assert (ControlFlowGraph == other.ControlFlowGraph);
			return true;
		}

		public override bool Equals (object obj)
			=> obj is MethodBodyValue inst && Equals (inst);

		public override int GetHashCode () => SymbolEqualityComparer.Default.GetHashCode (OwningSymbol);
	}
}
