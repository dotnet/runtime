// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;
using Mono.Linker;

namespace ILLink.Shared.TrimAnalysis
{
	internal sealed partial record SystemTypeValue : SingleValue
	{
		public SystemTypeValue (in TypeProxy representedType, ITryResolveMetadata resolver)
		{
			RepresentedType = representedType;
			this.resolver = resolver;
		}

		private readonly ITryResolveMetadata resolver;

		public bool Equals (SystemTypeValue? other) => other is not null && TypeReferenceEqualityComparer.AreEqual (RepresentedType.Type, other.RepresentedType.Type, resolver);

		public override int GetHashCode () => TypeReferenceEqualityComparer.GetHashCodeFor (RepresentedType.Type);
	}
}
