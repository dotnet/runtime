// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if XMLSERIALIZERGENERATOR
namespace Microsoft.XmlSerializer.Generator
#else
namespace System.Xml.Serialization
#endif
{
    using System.Xml;

    /// <include file='doc\IXmlTextParser.uex' path='docs/doc[@for="IXmlTextParser"]/*' />
    ///<internalonly/>
    /// <devdoc>
    /// <para>This class is <see langword='interface'/> .</para>
    /// </devdoc>
#if XMLSERIALIZERGENERATOR
    internal interface IXmlTextParser
#else
    public interface IXmlTextParser
#endif
    {
        /// <include file='doc\IXmlTextParser.uex' path='docs/doc[@for="IXmlTextParser.Normalized"]/*' />
        /// <internalonly/>
        bool Normalized { get; set; }

        /// <include file='doc\IXmlTextParser.uex' path='docs/doc[@for="IXmlTextParser.WhitespaceHandling"]/*' />
        /// <internalonly/>
        WhitespaceHandling WhitespaceHandling { get; set; }
    }
}
