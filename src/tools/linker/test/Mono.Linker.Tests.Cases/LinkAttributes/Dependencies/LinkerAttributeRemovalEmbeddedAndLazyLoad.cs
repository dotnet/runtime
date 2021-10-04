// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
