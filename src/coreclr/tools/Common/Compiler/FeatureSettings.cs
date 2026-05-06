// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.XPath;

namespace ILCompiler
{
    public static class FeatureSettings
    {
        public static bool ShouldProcessElement(XPathNavigator nav, IReadOnlyDictionary<string, bool> featureSwitchValues)
        {
            var feature = GetAttribute(nav, "feature");
            if (string.IsNullOrEmpty(feature))
                return true;

            var value = GetAttribute(nav, "featurevalue");
            if (string.IsNullOrEmpty(value))
            {
                //context.LogError(null, DiagnosticId.XmlFeatureDoesNotSpecifyFeatureValue, documentLocation, feature);
                return false;
            }

            if (!bool.TryParse(value, out bool bValue))
            {
                //context.LogError(null, DiagnosticId.XmlUnsupportedNonBooleanValueForFeature, documentLocation, feature);
                return false;
            }

            var isDefault = GetAttribute(nav, "featuredefault");
            bool bIsDefault = false;
            if (!string.IsNullOrEmpty(isDefault) && (!bool.TryParse(isDefault, out bIsDefault) || !bIsDefault))
            {
                //context.LogError(null, DiagnosticId.XmlDocumentLocationHasInvalidFeatureDefault, documentLocation);
                return false;
            }

            if (!featureSwitchValues.TryGetValue(feature, out bool featureSetting))
                return bIsDefault;

            return bValue == featureSetting;
        }

        public static string GetAttribute(XPathNavigator nav, string attribute)
        {
            return nav.GetAttribute(attribute, string.Empty);
        }
    }
}
