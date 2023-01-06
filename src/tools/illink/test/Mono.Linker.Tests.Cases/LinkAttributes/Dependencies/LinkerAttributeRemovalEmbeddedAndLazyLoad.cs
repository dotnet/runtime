// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.LinkAttributes.Dependencies
{
	[EmbeddedAttributeToBeRemoved]
	public class TypeWithEmbeddedAttributeToBeRemoved
	{
		public TypeWithEmbeddedAttributeToBeRemoved () { }
	}

	public class EmbeddedAttributeToBeRemoved : Attribute
	{
		public EmbeddedAttributeToBeRemoved () { }
	}
}
