// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct GenericParameterProxy
	{
		public GenericParameterProxy (GenericParameter genericParameter) => GenericParameter = genericParameter;

		public static implicit operator GenericParameterProxy (GenericParameter genericParameter) => new (genericParameter);

		internal partial bool HasDefaultConstructorConstraint () => GenericParameter.HasDefaultConstructorConstraint;

		public readonly GenericParameter GenericParameter;

		public override string ToString () => GenericParameter.ToString ();
	}
}
