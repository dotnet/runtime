// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        internal const int DefaultMaxDecryptionDepth = 64;
        internal const string DangerousMaxRecursionDepthAppContextSwitch = "System.Security.Cryptography.Xml.DangerousMaxRecursionDepth";
        internal const string AllowDangerousEncryptedXmlTransformsAppContextSwitch = "System.Security.Cryptography.Xml.AllowDangerousEncryptedXmlTransforms";
        internal const string MaxTransformsPerChainAppContextSwitch = "System.Security.Cryptography.Xml.MaxTransformsPerChain";
        internal const string MaxDecryptedDataElementsAppContextSwitch = "System.Security.Cryptography.Xml.MaxDecryptedDataElements";
        internal const string AllowUnsafeTruncatedHmacSignatureVerificationAppContextSwitch = "Switch.System.Security.Cryptography.Xml.SignedXml.AllowUnsafeTruncatedHmacSignatureVerification";

        internal const int DefaultMaxTransformsPerChain = 20;
        internal const int DefaultMaxDecryptedDataElements = 100;

        /// <summary>
        /// Gets the maximum recursion depth for recursive XML operations.
        /// Configurable via AppContext data "System.Security.Cryptography.Xml.DangerousMaxRecursionDepth".
        /// Default value is 64. A value of 0 means infinite (no limit).
        /// </summary>
        internal static int DangerousMaxRecursionDepth { get; } =
            GetInt32Config(DangerousMaxRecursionDepthAppContextSwitch, DefaultMaxDecryptionDepth, allowNegative: false);

        /// <summary>
        /// Gets whether to enforce safe transforms for XML encryption by default.
        /// Configurable via AppContext switch "System.Security.Cryptography.Xml.AllowDangerousEncryptedXmlTransforms".
        /// Default value is false.
        /// </summary>
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
        /// Configurable via AppContext data "System.Security.Cryptography.Xml.MaxEncryptedDataReferences".
        /// Default value is 100. A value of 0 means infinite (no limit).
        /// </summary>
        internal static int MaxDecryptedDataElements { get; } =
            GetInt32Config(MaxDecryptedDataElementsAppContextSwitch, DefaultMaxDecryptedDataElements, allowNegative: false);

        /// <summary>
        /// Gets whether HMAC signature verification accepts truncated signature values.
        /// Configurable via AppContext switch "Switch.System.Security.Cryptography.Xml.SignedXml.AllowUnsafeTruncatedHmacSignatureVerification".
        /// Default value is false.
        /// </summary>
        internal static bool AllowUnsafeTruncatedHmacSignatureVerification { get; } =
            GetBooleanConfig(AllowUnsafeTruncatedHmacSignatureVerificationAppContextSwitch, defaultValue: false);

        /// <summary>
        /// Gets an integer configuration value from AppContext data.
        /// </summary>
        /// <param name="appContextName">The AppContext data key name.</param>
        /// <param name="defaultValue">The default value if not configured or invalid.</param>
        /// <param name="allowNegative">Whether to allow negative values.</param>
        /// <returns>The configured value or the default.</returns>
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

        /// <summary>
        /// Gets a boolean configuration value from AppContext switch.
        /// </summary>
        /// <param name="appContextName">The AppContext switch name.</param>
        /// <param name="defaultValue">The default value if not configured or invalid.</param>
        /// <returns>The configured value or the default.</returns>
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
