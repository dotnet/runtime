// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is a System.Type value which represents generic parameter (basically result of typeof(T))
	/// Its actual type is unknown, but it can have annotations.
	/// </summary>
	sealed partial record GenericParameterValue : ValueWithDynamicallyAccessedMembers
	{
		public readonly GenericParameterProxy GenericParameter;

		public partial bool HasDefaultConstructorConstraint ();
	}
}
