// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    /// <include file='doc\IXmlLineInfo.uex' path='docs/doc[@for="IXmlLineInfo"]/*' />
    public interface IXmlLineInfo
    {
        /// <include file='doc\IXmlLineInfo.uex' path='docs/doc[@for="IXmlLineInfo.HasLineInfo"]/*' />
        bool HasLineInfo();
        /// <include file='doc\IXmlLineInfo.uex' path='docs/doc[@for="IXmlLineInfo.LineNumber"]/*' />
        int LineNumber { get; }
        /// <include file='doc\IXmlLineInfo.uex' path='docs/doc[@for="IXmlLineInfo.LinePosition"]/*' />
        int LinePosition { get; }
    }
}// namespace
