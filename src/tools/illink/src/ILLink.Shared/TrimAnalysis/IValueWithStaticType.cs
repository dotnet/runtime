// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	internal interface IValueWithStaticType
	{
		/// <summary>
		/// The static type of the value, represented as closely as possible, but not always exact.  It can be null, for
		/// example, when the analysis is imprecise or operating on malformed code.
		/// </summary>
		TypeProxy? StaticType { get; }
	}
}
