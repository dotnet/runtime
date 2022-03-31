// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is a known System.Reflection.MethodBase value.  MethodRepresented is the 'value' of the MethodBase.
	/// </summary>
	sealed partial record SystemReflectionMethodBaseValue : SingleValue
	{
		public SystemReflectionMethodBaseValue (MethodProxy methodRepresented) => MethodRepresented = methodRepresented;

		public readonly MethodProxy MethodRepresented;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (MethodRepresented);
	}
}
