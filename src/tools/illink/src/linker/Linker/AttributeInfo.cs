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
		public Dictionary<ICustomAttributeProvider, IEnumerable<CustomAttribute>> CustomAttributes { get; }
		public Dictionary<ICustomAttributeProvider, IEnumerable<Attribute>> InternalAttributes { get; }

		public AttributeInfo ()
		{
			CustomAttributes = new Dictionary<ICustomAttributeProvider, IEnumerable<CustomAttribute>> ();
			InternalAttributes = new Dictionary<ICustomAttributeProvider, IEnumerable<Attribute>> ();
		}

		public void AddCustomAttributes (ICustomAttributeProvider provider, IEnumerable<CustomAttribute> customAttributes)
		{
			if (!CustomAttributes.TryGetValue (provider, out var existing)) {
				CustomAttributes.Add (provider, customAttributes);
			} else {
				CustomAttributes[provider] = existing.Concat (customAttributes);
			}
		}

		public void AddInternalAttributes (ICustomAttributeProvider provider, IEnumerable<Attribute> attributes)
		{
			if (!InternalAttributes.TryGetValue (provider, out var existing)) {
				InternalAttributes.Add (provider, attributes);
			} else {
				InternalAttributes[provider] = existing.Concat (attributes);
			}
		}
	}
}
