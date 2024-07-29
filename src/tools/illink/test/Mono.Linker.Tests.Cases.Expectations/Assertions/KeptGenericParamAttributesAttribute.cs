// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.All, Inherited = false)]
	public class KeptGenericParamAttributesAttribute : KeptAttribute
	{
		public KeptGenericParamAttributesAttribute (GenericParameterAttributes attributes)
		{
		}
	}
}
