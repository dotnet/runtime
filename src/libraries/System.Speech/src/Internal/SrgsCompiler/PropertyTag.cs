// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    internal sealed class PropertyTag : ParseElement, IPropertyTag
    {
        #region Constructors

        internal PropertyTag(ParseElement parent, Backend backend)
            : base(parent._rule)
        {
        }

        #endregion

        #region Internal Methods
        // The probability that this item will be repeated.
        void IPropertyTag.NameValue(IElement parent, string name, object value)
        {
            //Return if the Tag content is empty
            string sValue = value as string;
            if (string.IsNullOrEmpty(name) && (value == null || (sValue != null && string.IsNullOrEmpty((sValue).Trim()))))
            {
                return;
            }

            // Build semantic properties to attach to epsilon transition.
            // <tag>Name=</tag>             pszValue = null     vValue = VT_EMPTY
            // <tag>Name="string"</tag>     pszValue = "string" vValue = VT_EMPTY
            // <tag>Name=true</tag>         pszValue = null     vValue = VT_BOOL
            // <tag>Name=123</tag>          pszValue = null     vValue = VT_I4
            // <tag>Name=3.14</tag>         pszValue = null     vValue = VT_R8

            if (!string.IsNullOrEmpty(name))
            {
                // Set property name
                _propInfo._pszName = name;
            }
            else
            {
                // If no property, set the name to the anonymous property name
                _propInfo._pszName = "=";
            }

            // Set property value
            _propInfo._comValue = value;
#pragma warning disable 0618 // VarEnum is obsolete
            if (value == null)
            {
                _propInfo._comType = VarEnum.VT_EMPTY;
            }
            else if (sValue != null)
            {
                _propInfo._comType = VarEnum.VT_EMPTY;
            }
            else if (value is int)
            {
                _propInfo._comType = VarEnum.VT_I4;
            }
            else if (value is double)
            {
                _propInfo._comType = VarEnum.VT_R8;
            }
            else if (value is bool)
            {
                _propInfo._comType = VarEnum.VT_BOOL;
            }
            else
            {
                // should never get here
                System.Diagnostics.Debug.Assert(false);
            }
#pragma warning restore 0618
        }

        void IElement.PostParse(IElement parentElement)
        {
            ParseElementCollection parent = (ParseElementCollection)parentElement;
            _propInfo._ulId = (uint)parent._rule._iSerialize2;

            // Attach the semantic properties on the parent element.
            parent.AddSementicPropertyTag(_propInfo);
        }

        #endregion

        #region Private Fields

        private CfgGrammar.CfgProperty _propInfo = new();

        #endregion
    }
}
