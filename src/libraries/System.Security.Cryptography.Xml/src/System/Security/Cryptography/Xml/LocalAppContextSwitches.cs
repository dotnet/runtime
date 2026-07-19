// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        internal const int DefaultMaxRecursionDepth = 64;
        internal const string DangerousMaxRecursionDepthAppContextSwitch = "System.Security.Cryptography.Xml.DangerousMaxRecursionDepth";
        internal const string AllowDangerousEncryptedXmlTransformsAppContextSwitch = "System.Security.Cryptography.Xml.AllowDangerousEncryptedXmlTransforms";
        internal const string MaxTransformsPerChainAppContextSwitch = "System.Security.Cryptography.Xml.MaxTransformsPerChain";
        internal const string MaxDecryptedDataElementsAppContextSwitch = "System.Security.Cryptography.Xml.MaxDecryptedDataElements";

        internal const int DefaultMaxTransformsPerChain = 20;
        internal const int DefaultMaxDecryptedDataElements = 100;

        // 0 disables the limit for compatibility. Negative values fall back to the default.
        internal static int DangerousMaxRecursionDepth { get; } =
            GetInt32Config(DangerousMaxRecursionDepthAppContextSwitch, DefaultMaxRecursionDepth, allowNegative: false);

        internal static bool AllowDangerousEncryptedXmlTransforms { get; } =
            GetBooleanConfig(AllowDangerousEncryptedXmlTransformsAppContextSwitch, defaultValue: false);

        /// <summary>
        /// Gets the maximum number of transforms allowed in a deserialized transform chain.
        /// Configurable via AppContext data "System.Security.Cryptography.Xml.MaxTransformsPerChain".
        /// Default value is 20. A value of 0 means infinite (no limit).
        /// </summary>
        internal static int MaxTransformsPerChain { get; } =
            GetInt32Config(MaxTransformsPerChainAppContextSwitch, DefaultMaxTransformsPerChain, allowNegative: false);

        /// <summary>
        /// Gets the maximum number of <c>EncryptedData</c> references that may be processed during a single
        /// <see cref="System.Security.Cryptography.Xml.XmlDecryptionTransform"/> operation.
        /// Configurable via AppContext data "System.Security.Cryptography.Xml.MaxDecryptedDataElements".
        /// Default value is 100. A value of 0 means infinite (no limit).
        /// </summary>
        internal static int MaxDecryptedDataElements { get; } =
            GetInt32Config(MaxDecryptedDataElementsAppContextSwitch, DefaultMaxDecryptedDataElements, allowNegative: false);

        internal static int MaxReferencesPerSignedInfo { get; } =
            GetInt32Config("System.Security.Cryptography.MaxReferencesPerSignedInfo", defaultValue: 100);

        private static int GetInt32Config(string appContextName, int defaultValue, bool allowNegative = true)
        {
            object? data = AppContext.GetData(appContextName);

            if (data is null)
            {
                return defaultValue;
            }

            int value;

            try
            {
                value = Convert.ToInt32(data, CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }

            return (allowNegative || value >= 0) ? value : defaultValue;
        }

        private static bool GetBooleanConfig(string appContextName, bool defaultValue)
        {
            if (AppContext.TryGetSwitch(appContextName, out bool isEnabled))
            {
                return isEnabled;
            }

            return defaultValue;
        }
    }
}
