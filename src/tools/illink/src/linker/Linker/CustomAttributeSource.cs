// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	public class CustomAttributeSource
	{
		private Dictionary<ICustomAttributeProvider, IEnumerable<CustomAttribute>> _xmlCustomAttributes;

		public CustomAttributeSource ()
		{
			_xmlCustomAttributes = new Dictionary<ICustomAttributeProvider, IEnumerable<CustomAttribute>> ();
		}

		public void AddCustomAttributes (ICustomAttributeProvider provider, IEnumerable<CustomAttribute> customAttributes)
		{
			if (!_xmlCustomAttributes.ContainsKey (provider))
				_xmlCustomAttributes[provider] = customAttributes;
			else
				_xmlCustomAttributes[provider] = _xmlCustomAttributes[provider].Concat (customAttributes);
		}

		public IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes) {
				foreach (var customAttribute in provider.CustomAttributes)
					yield return customAttribute;
			}

			if (_xmlCustomAttributes.TryGetValue (provider, out var annotations)) {
				foreach (var customAttribute in annotations)
					yield return customAttribute;
			}
		}

		public bool HasCustomAttributes (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes)
				return true;

			if (_xmlCustomAttributes.ContainsKey (provider))
				return true;

			return false;
		}
	}
}
