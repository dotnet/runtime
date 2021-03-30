// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	public class AttributeInfo
	{
		public Dictionary<ICustomAttributeProvider, CustomAttribute[]> CustomAttributes { get; }

		public AttributeInfo ()
		{
			CustomAttributes = new Dictionary<ICustomAttributeProvider, CustomAttribute[]> ();
		}

		public void AddCustomAttributes (ICustomAttributeProvider provider, CustomAttribute[] customAttributes)
		{
			if (!CustomAttributes.TryGetValue (provider, out var existing)) {
				CustomAttributes.Add (provider, customAttributes);
			} else {
				int existingLength = existing.Length;
				Array.Resize (ref existing, existingLength + customAttributes.Length);
				customAttributes.CopyTo (existing, existingLength);

				CustomAttributes[provider] = existing;
			}
		}
	}
}
