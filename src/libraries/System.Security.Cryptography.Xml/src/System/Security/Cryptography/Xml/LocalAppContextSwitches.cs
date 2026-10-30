// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        internal const int DefaultMaxRecursionDepth = 64;
        internal const int DefaultMaxCanonicalizationAttributesPerElement = 512;
        internal const int DefaultMaxCount = 256;
        internal const string DangerousMaxRecursionDepthAppContextSwitch = "System.Security.Cryptography.Xml.DangerousMaxRecursionDepth";
        internal const string DangerousMaxCanonicalizationAttributesPerElementAppContextSwitch = "System.Security.Cryptography.Xml.DangerousMaxCanonicalizationAttributesPerElement";
        internal const string AllowDangerousEncryptedXmlTransformsAppContextSwitch = "System.Security.Cryptography.Xml.AllowDangerousEncryptedXmlTransforms";
        internal const string CryptoXmlMaxInputDocumentDepthAppContextSwitch = "System.Security.Cryptography.Xml.CryptoXmlMaxInputDocumentDepth";
        internal const string CryptoXmlAllowDtdProcessingAppContextSwitch = "System.Security.Cryptography.Xml.CryptoXmlAllowDtdProcessing";
        internal const string MaxTransformsPerChainAppContextSwitch = "System.Security.Cryptography.Xml.MaxTransformsPerChain";
        internal const string MaxDecryptedDataElementsAppContextSwitch = "System.Security.Cryptography.Xml.MaxDecryptedDataElements";
        internal const string DangerousMaxDecryptionCountAppContextSwitch = "System.Security.Cryptography.Xml.DangerousMaxDecryptionCount";
        internal const string AllowUnsafeTruncatedHmacSignatureVerificationAppContextSwitch = "Switch.System.Security.Cryptography.Xml.SignedXml.AllowUnsafeTruncatedHmacSignatureVerification";

        internal const int DefaultMaxTransformsPerChain = 20;
        internal const int DefaultMaxDecryptedDataElements = 100;

        // 0 disables the limit for compatibility. Negative values fall back to the default.
        internal static int DangerousMaxRecursionDepth { get; } =
            GetInt32Config(DangerousMaxRecursionDepthAppContextSwitch, DefaultMaxRecursionDepth, allowNegative: false);

        /// <summary>
        /// Gets the maximum number of attributes, and separately of namespace declarations, that
        /// canonicalization will render for a single element.
        /// Configurable via AppContext data "System.Security.Cryptography.Xml.DangerousMaxCanonicalizationAttributesPerElement".
        /// Default value is 512. A value of 0 means infinite (no limit).
        /// </summary>
        /// <remarks>
        /// Canonicalization scans the pending render lists linearly for each candidate name, so lists
        /// of unbounded size make the work quadratic in the size of a single element, on a path that
        /// processes attacker-supplied documents. Bounding the lists keeps that work constant per
        /// element. The bound also depends on <see cref="DangerousMaxRecursionDepth"/> to limit how
        /// many ancestor declarations can be in scope at once; setting that switch to 0 removes the
        /// depth limit and reintroduces the quadratic behavior.
        /// </remarks>
        internal static int DangerousMaxCanonicalizationAttributesPerElement { get; } =
            GetInt32Config(
                DangerousMaxCanonicalizationAttributesPerElementAppContextSwitch,
                DefaultMaxCanonicalizationAttributesPerElement,
                allowNegative: false);

        /// <summary>
        /// Gets whether to enforce safe transforms for XML encryption by default.
        /// Configurable via AppContext switch "System.Security.Cryptography.Xml.AllowDangerousEncryptedXmlTransforms".
        /// Default value is false.
        /// </summary>
        internal static bool AllowDangerousEncryptedXmlTransforms { get; } =
            GetBooleanConfig(AllowDangerousEncryptedXmlTransformsAppContextSwitch, defaultValue: false);

        /// <summary>
        /// Gets the maximum input-document depth (XmlReader.Depth) accepted by the cryptographic XML
        /// ingestion and decrypt re-parse paths.
        /// Configurable via AppContext data "System.Security.Cryptography.Xml.CryptoXmlMaxInputDocumentDepth".
        /// Defaults to <see cref="DangerousMaxRecursionDepth"/>. A value of 0 means infinite (no limit).
        /// </summary>
        internal static int CryptoXmlMaxInputDocumentDepth { get; } = GetCryptoXmlMaxInputDocumentDepth();

        private static int GetCryptoXmlMaxInputDocumentDepth()
        {
            int value = GetInt32Config(CryptoXmlMaxInputDocumentDepthAppContextSwitch, DangerousMaxRecursionDepth, allowNegative: false);
            return value;
        }

        /// <summary>
        /// Gets whether DTD processing is allowed on the cryptographic XML ingestion and re-parse paths.
        /// Configurable via AppContext switch "System.Security.Cryptography.Xml.CryptoXmlAllowDtdProcessing".
        /// Default value is false.
        /// </summary>
        internal static bool CryptoXmlAllowDtdProcessing { get; } =
            GetBooleanConfig(CryptoXmlAllowDtdProcessingAppContextSwitch, defaultValue: false);

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

        /// <summary>
        /// Gets the maximum number of encrypted elements processed during a decryption operation.
        /// Configurable via AppContext data "System.Security.Cryptography.Xml.DangerousMaxDecryptionCount".
        /// Default value is 256. A value of 0 means infinite (no limit).
        /// </summary>
        internal static int DangerousMaxDecryptionCount { get; } =
            GetInt32Config(DangerousMaxDecryptionCountAppContextSwitch, DefaultMaxCount, allowNegative: false);

        /// <summary>
        /// Gets whether HMAC signature verification accepts truncated signature values.
        /// Configurable via AppContext switch "Switch.System.Security.Cryptography.Xml.SignedXml.AllowUnsafeTruncatedHmacSignatureVerification".
        /// Default value is false.
        /// </summary>
        internal static bool AllowUnsafeTruncatedHmacSignatureVerification { get; } =
            GetBooleanConfig(AllowUnsafeTruncatedHmacSignatureVerificationAppContextSwitch, defaultValue: false);

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
