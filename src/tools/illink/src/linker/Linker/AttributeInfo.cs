// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
