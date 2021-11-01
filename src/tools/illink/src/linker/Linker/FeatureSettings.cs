// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.XPath;

namespace Mono.Linker
{
	public static class FeatureSettings
	{
		public static bool ShouldProcessElement (XPathNavigator nav, LinkContext context, string documentLocation)
		{
			var feature = GetAttribute (nav, "feature");
			if (string.IsNullOrEmpty (feature))
				return true;

			var value = GetAttribute (nav, "featurevalue");
			if (string.IsNullOrEmpty (value)) {
				context.LogError ($"Failed to process '{documentLocation}'. Feature '{feature}' does not specify a 'featurevalue' attribute.", 1001);
				return false;
			}

			if (!bool.TryParse (value, out bool bValue)) {
				context.LogError ($"Failed to process '{documentLocation}'. Unsupported non-boolean feature definition '{feature}'.", 1002);
				return false;
			}

			var isDefault = GetAttribute (nav, "featuredefault");
			bool bIsDefault = false;
			if (!string.IsNullOrEmpty (isDefault) && (!bool.TryParse (isDefault, out bIsDefault) || !bIsDefault)) {
				context.LogError ($"Failed to process '{documentLocation}'. Unsupported value for featuredefault attribute.", 1014);
				return false;
			}

			if (!context.FeatureSettings.TryGetValue (feature, out bool featureSetting))
				return bIsDefault;

			return bValue == featureSetting;
		}

		public static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, String.Empty);
		}
	}
}
