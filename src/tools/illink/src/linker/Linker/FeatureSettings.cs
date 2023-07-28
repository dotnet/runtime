// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml.XPath;
using ILLink.Shared;

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
				context.LogError (null, DiagnosticId.XmlFeatureDoesNotSpecifyFeatureValue, documentLocation, feature);
				return false;
			}

			if (!bool.TryParse (value, out bool bValue)) {
				context.LogError (null, DiagnosticId.XmlUnsupportedNonBooleanValueForFeature, documentLocation, feature);
				return false;
			}

			var isDefault = GetAttribute (nav, "featuredefault");
			bool bIsDefault = false;
			if (!string.IsNullOrEmpty (isDefault) && (!bool.TryParse (isDefault, out bIsDefault) || !bIsDefault)) {
				context.LogError (null, DiagnosticId.XmlDocumentLocationHasInvalidFeatureDefault, documentLocation);
				return false;
			}

			if (!context.FeatureSettings.TryGetValue (feature, out bool featureSetting))
				return bIsDefault;

			return bValue == featureSetting;
		}

		public static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, string.Empty);
		}
	}
}
