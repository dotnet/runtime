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
		public IMethodSymbol Method { get; }

		public ControlFlowGraph ControlFlowGraph { get; }

		public MethodBodyValue (IMethodSymbol method, ControlFlowGraph cfg)
		{
			Method = method;
			ControlFlowGraph = cfg;
		}

		public bool Equals (MethodBodyValue other)
		{
			if (!ReferenceEquals (Method, other.Method))
				return false;

			Debug.Assert (ControlFlowGraph == other.ControlFlowGraph);
			return true;
		}
	}
}