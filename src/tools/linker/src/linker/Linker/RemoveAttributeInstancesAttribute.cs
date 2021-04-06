// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using Mono.Cecil;

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
		public RemoveAttributeInstancesAttribute ()
		{
			Arguments = Array.Empty<CustomAttributeArgument> ();
		}

		public RemoveAttributeInstancesAttribute (CustomAttributeArgument value1)
		{
			Arguments = new[] { value1 };
		}

		public CustomAttributeArgument[] Arguments { get; }

		// This might be also useful to add later
		// public bool ExactArgumentsOnly { get; set; }
	}
}
