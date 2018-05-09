// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    internal class CodePageDataItem
    {
        private readonly int _codePage;
        private readonly int _uiFamilyCodePage;
        private readonly string _webName;
        private readonly uint _flags;
        private string _displayNameResourceKey;

        internal CodePageDataItem(int codePage, int uiFamilyCodePage, string webName, uint flags)
        {
            _codePage = codePage;
            _uiFamilyCodePage = uiFamilyCodePage;
            _webName = webName;
            _flags = flags;
        }

        public int CodePage
        {
            get { return _codePage; }
        }

        public int UIFamilyCodePage
        {
            get { return _uiFamilyCodePage; }
        }

        public String WebName
        {
            get { return _webName; }
        }

        public String HeaderName
        {
            get { return _webName; } // all the code pages used on unix only have a single name
        }

        public String BodyName
        {
            get { return _webName; } // all the code pages used on unix only have a single name
        }

        public uint Flags
        {
            get { return _flags; }
        }

        // PAL ends here

        public string DisplayNameResourceKey
        {
            get
            {
                if (_displayNameResourceKey == null)
                {
                    _displayNameResourceKey = "Globalization_cp_" + CodePage;
                }

                return _displayNameResourceKey;
            }
        }
    }
}
