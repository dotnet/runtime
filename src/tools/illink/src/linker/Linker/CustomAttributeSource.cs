// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	class CustomAttributeSource
	{
		readonly List<XmlCustomAttributeSource> _sources;

		public CustomAttributeSource (LinkContext context)
		{
			_sources = new List<XmlCustomAttributeSource> ();
			if (context.AttributeDefinitions?.Count > 0) {
				foreach (string a in context.AttributeDefinitions) {
					XmlCustomAttributeSource xmlAnnotations = new XmlCustomAttributeSource (context);
					xmlAnnotations.ParseXml (a);
					_sources.Add (xmlAnnotations);
				}
			}
		}

		public IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes) {
				foreach (var customAttribute in provider.CustomAttributes)
					yield return customAttribute;
			}

			if (_sources.Count > 0) {
				foreach (var source in _sources) {
					if (source.HasCustomAttributes (provider)) {
						foreach (var customAttribute in source.GetCustomAttributes (provider))
							yield return customAttribute;
					}
				}
			}
		}

		public bool HasCustomAttributes (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes)
				return true;

			if (_sources.Count > 0) {
				foreach (var source in _sources) {
					if (source.HasCustomAttributes (provider)) {
						return true;
					}
				}
			}

			return false;
		}
	}
}
