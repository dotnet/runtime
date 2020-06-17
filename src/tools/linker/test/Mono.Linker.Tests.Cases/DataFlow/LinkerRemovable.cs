// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SetupLinkAttributesFile ("LinkerRemovable.xml")]
	[IgnoreLinkAttributes (false)]
	[KeptMember(".ctor()")]
	class LinkerRemovable
	{
		public static void Main ()
		{
			var instance = new LinkerRemovable ();
			instance._typeWithDefaultConstructor = null;
		}
		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.DefaultConstructor)]
		Type _typeWithDefaultConstructor;
	}
}
