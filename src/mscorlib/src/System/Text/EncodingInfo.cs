// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    using System;
    using System.Text;


    [Serializable]
    public sealed class EncodingInfo
    {
        int     iCodePage;          // Code Page #
        String  strEncodingName;    // Short name (web name)
        String  strDisplayName;     // Full localized name

        internal EncodingInfo(int codePage, string name, string displayName)
        {
            this.iCodePage = codePage;
            this.strEncodingName = name;
            this.strDisplayName = displayName;
        }


        public int CodePage
        {
            get
            {
                return iCodePage;
            }
        }


        public String Name
        {
            get
            {
                return strEncodingName;
            }
        }


        public String DisplayName
        {
            get
            {
                return strDisplayName;
            }
        }


        public Encoding GetEncoding()
        {
            return Encoding.GetEncoding(this.iCodePage);
        }

        public override bool Equals(Object value)
        {
            EncodingInfo that = value as EncodingInfo;
            if (that != null)
            {
                return (this.CodePage == that.CodePage);
            }
            return (false);
        }
        
        public override int GetHashCode()
        {
            return this.CodePage;
        }
        
    }
}
