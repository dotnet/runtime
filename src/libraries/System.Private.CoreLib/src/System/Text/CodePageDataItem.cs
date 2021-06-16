// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text
{
    internal sealed class CodePageDataItem
    {
        public int UIFamilyCodePage { get; }
        public string WebName { get; }
        public string HeaderName { get; }
        public string BodyName { get; }
        public string DisplayName { get; }
        public uint Flags { get; }

        internal CodePageDataItem(
            int uiFamilyCodePage,
            string webName,
            string headerName,
            string bodyName,
            string displayName,
            uint flags)
        {
            UIFamilyCodePage = uiFamilyCodePage;
            WebName = webName;
            HeaderName = headerName;
            BodyName = bodyName;
            DisplayName = displayName;
            Flags = flags;
        }
    }
}
