// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILLink.Shared.DataFlow
{
	public interface IControlFlowGraph<TBlock>
		where TBlock : IEquatable<TBlock>
	{
		TBlock Entry { get; }

		IEnumerable<TBlock> Blocks { get; }

		IEnumerable<TBlock> GetPredecessors (TBlock block);
	}
}