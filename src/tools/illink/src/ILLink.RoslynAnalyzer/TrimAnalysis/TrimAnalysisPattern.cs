// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	// TODO: create abstraction for operation/location suitable for use by
	// roslyn or linker to facilitate sharing
	public readonly record struct TrimAnalysisPattern (MultiValue Source, MultiValue Target, IOperation Operation);
}