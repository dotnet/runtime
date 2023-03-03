// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Linker
{
	/// <summary>
	/// This attribute name will be the name hardcoded in linker which will remove all 
	/// attribute usages but not the attribute definition
	/// </summary>
	[AttributeUsage (
		AttributeTargets.Class, Inherited = false)]
	public sealed class RemoveAttributeInstancesAttribute : Attribute
	{
		public RemoveAttributeInstancesAttribute (Collection<CustomAttributeArgument> args)
		{
			if (args.Count == 0) {
				Arguments = Array.Empty<CustomAttributeArgument> ();
				return;
			}
			var arg = args[0];
			if (arg.Value is CustomAttributeArgument[] innerArgs)
				Arguments = innerArgs.Select (arg => (CustomAttributeArgument) arg.Value).ToArray ();
			else
				Arguments = new CustomAttributeArgument[] { (CustomAttributeArgument) arg.Value };
		}

		public CustomAttributeArgument[] Arguments { get; }

		// This might be also useful to add later
		// public bool ExactArgumentsOnly { get; set; }
	}
}
