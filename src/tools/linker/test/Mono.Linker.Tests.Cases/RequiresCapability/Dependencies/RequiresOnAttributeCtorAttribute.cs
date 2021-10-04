// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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