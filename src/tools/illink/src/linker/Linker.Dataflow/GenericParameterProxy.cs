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

		internal partial bool HasEnumConstraint ()
		{
			if (GenericParameter.HasConstraints) {
				foreach (GenericParameterConstraint? constraint in GenericParameter.Constraints) {
					if (constraint.ConstraintType.Name == "Enum" && constraint.ConstraintType.Namespace == "System")
						return true;
				}
			}

			return false;
		}

		public readonly GenericParameter GenericParameter;

		public override string ToString () => GenericParameter.ToString ();
	}
}
