// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is a known System.Reflection.MethodBase value.  MethodRepresented is the 'value' of the MethodBase.
	/// </summary>
	sealed partial record SystemReflectionMethodBaseValue : SingleValue;
}
