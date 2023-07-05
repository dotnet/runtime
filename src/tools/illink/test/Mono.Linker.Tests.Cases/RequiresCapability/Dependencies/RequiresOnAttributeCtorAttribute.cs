// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.RequiresCapability.Dependencies
{
	public class RequiresOnAttributeCtorAttribute : Attribute
	{
		[RequiresUnreferencedCode ("Message from attribute's ctor.")]
		public RequiresOnAttributeCtorAttribute ()
		{
		}
	}
}
