// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// Acts as the base class for all values that represent a reference to another value. These should only be held in a ref type or on the stack as a result of a 'load address' instruction (e.g. ldloca).
	/// </summary>
	public abstract record ReferenceValue : SingleValue { }
}
