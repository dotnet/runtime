// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public abstract class EncryptedType
    {
        [ThreadStatic]
        private static int t_depth;

        private string? _id;
        private string? _type;
        private string? _mimeType;
        private string? _encoding;
        private EncryptionMethod? _encryptionMethod;
        private EncryptionPropertyCollection? _props;
        internal XmlElement? _cachedXml;

        [MemberNotNullWhen(true, nameof(_cachedXml))]
        internal bool CacheValid
        {
            get
            {
                return (_cachedXml != null);
            }
        }

        public virtual string? Id
        {
            get { return _id; }
            set
            {
                _id = value;
                _cachedXml = null;
            }
        }

        public virtual string? Type
        {
            get { return _type; }
            set
            {
                _type = value;
                _cachedXml = null;
            }
        }

        public virtual string? MimeType
        {
            get { return _mimeType; }
            set
            {
                _mimeType = value;
                _cachedXml = null;
            }
        }

        public virtual string? Encoding
        {
            get { return _encoding; }
            set
            {
                _encoding = value;
                _cachedXml = null;
            }
        }

        [AllowNull]
        public KeyInfo KeyInfo
        {
            get => field ??= new KeyInfo();
            set => field = value;
        }

        internal static void IncrementLoadXmlCurrentThreadDepth()
        {
            Debug.Assert(t_depth >= 0, "LoadXml current thread depth is negative.");
            int maxDepth = LocalAppContextSwitches.DangerousMaxRecursionDepth;
            if (maxDepth > 0 && t_depth > maxDepth)
            {
                throw new CryptographicException(SR.Cryptography_Xml_MaxDepthExceeded);
            }

            t_depth++;
        }

        internal static void DecrementLoadXmlCurrentThreadDepth()
        {
            Debug.Assert(t_depth > 0, "LoadXml current thread depth is already 0.");
            t_depth--;
        }

        public virtual EncryptionMethod? EncryptionMethod
        {
            get { return _encryptionMethod; }
            set
            {
                _encryptionMethod = value;
                _cachedXml = null;
            }
        }

        public virtual EncryptionPropertyCollection EncryptionProperties => _props ??= new EncryptionPropertyCollection();

        public void AddProperty(EncryptionProperty ep)
        {
            EncryptionProperties.Add(ep);
        }

        public virtual CipherData CipherData
        {
            get => field ??= new CipherData();
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                field = value;
                _cachedXml = null;
            }
        }

        [RequiresDynamicCode(CryptoHelpers.XsltRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        public abstract void LoadXml(XmlElement value);
        public abstract XmlElement GetXml();
    }
}
