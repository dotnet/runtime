// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Speech.Internal.SapiInterop;

namespace System.Speech.Internal.ObjectTokens
{
    internal class ObjectTokenCategory : RegistryDataKey, IEnumerable<ObjectToken>
    {
        #region Constructors

        protected ObjectTokenCategory(string keyId, RegistryDataKey key)
            : base(keyId, key)
        {
        }

        internal static ObjectTokenCategory Create(string sCategoryId)
        {
            RegistryDataKey key = RegistryDataKey.Open(sCategoryId, true);
            return new ObjectTokenCategory(sCategoryId, key);
        }

        #endregion

        #region internal Methods

        internal ObjectToken OpenToken(string keyName)
        {
            // Check if the token is for a voice
            string tokenName = keyName;
            if (!string.IsNullOrEmpty(tokenName) && tokenName.IndexOf("HKEY_", StringComparison.Ordinal) != 0)
            {
                tokenName = string.Format(CultureInfo.InvariantCulture, @"{0}\Tokens\{1}", Id, tokenName);
            }

            return ObjectToken.Open(null, tokenName, false);
        }

        internal IList<ObjectToken> FindMatchingTokens(string requiredAttributes, string optionalAttributes)
        {
            IList<ObjectToken> objectTokenList = new List<ObjectToken>();
            ISpObjectTokenCategory category = null;
            IEnumSpObjectTokens enumTokens = null;

            try
            {
                // Note - enumerated tokens should not be torn down/disposed by us (see SpInitTokenList in spuihelp.h)
                category = (ISpObjectTokenCategory)new SpObjectTokenCategory();
                category.SetId(_sKeyId, false);
                category.EnumTokens(requiredAttributes, optionalAttributes, out enumTokens);

                uint tokenCount;
                enumTokens.GetCount(out tokenCount);
                for (uint index = 0; index < tokenCount; ++index)
                {
                    ISpObjectToken spObjectToken = null;

                    enumTokens.Item(index, out spObjectToken);
                    ObjectToken objectToken = ObjectToken.Open(spObjectToken);
                    objectTokenList.Add(objectToken);
                }
            }
            finally
            {
                if (enumTokens != null)
                {
                    Marshal.ReleaseComObject(enumTokens);
                }
                if (category != null)
                {
                    Marshal.ReleaseComObject(category);
                }
            }

            return objectTokenList;
        }

        #region IEnumerable implementation

        IEnumerator<ObjectToken> IEnumerable<ObjectToken>.GetEnumerator()
        {
            IList<ObjectToken> objectTokenList = FindMatchingTokens(null, null);

            foreach (ObjectToken objectToken in objectTokenList)
            {
                yield return objectToken;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ObjectToken>)this).GetEnumerator();
        }

        #endregion

        #endregion
    }
}
