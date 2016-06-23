// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.IO;
using System.Text;
using System.Xml.XPath;

namespace System.Xml.Xsl
{
    /// <summary>
    /// Executable query object that is produced by a QilExpression -> Executable generator.  Implementations
    /// should be stateless so that Execute can be concurrently called by multiple threads.
    /// </summary>
    internal abstract class XmlCommand
    {
        /// <devdoc>
        ///     <para>
        ///         Default serialization options that will be used if the user does not supply
        ///         an XmlWriter at execution time.
        ///     </para>
        /// </devdoc>
        public abstract XmlWriterSettings DefaultWriterSettings { get; }

        /// <devdoc>
        ///     <para>
        ///         Executes the query over the provided XPathNavigator with the given XsltArgumentList
        ///         as run-time parameters. The results are output to the provided XmlWriter.
        ///     </para>
        /// </devdoc>
        public abstract void Execute(IXPathNavigable contextDocument, XmlResolver dataSources, XsltArgumentList argumentList, XmlWriter results);

        /// <devdoc>
        ///     <para>
        ///         Executes the query over the provided XPathNavigator with the given XsltArgumentList
        ///         as run-time parameters. The results are output to the provided TextWriter.
        ///     </para>
        /// </devdoc>
        public abstract void Execute(IXPathNavigable contextDocument, XmlResolver dataSources, XsltArgumentList argumentList, TextWriter results);

        /// <devdoc>
        ///     <para>
        ///         Executes the query over the provided XPathNavigator with the given XsltArgumentList
        ///         as run-time parameters. The results are output to the provided Stream.
        ///     </para>
        /// </devdoc>
        public abstract void Execute(IXPathNavigable contextDocument, XmlResolver dataSources, XsltArgumentList argumentList, Stream results);

        /// <devdoc>
        ///     <para>
        ///         Executes the query by accessing datasources via the XmlResolver and using
        ///         run-time parameters as provided by the XsltArgumentList. The default document
        ///         is mapped into the XmlResolver with the provided name. The results are output
        ///         to the provided XmlWriter.
        ///     </para>
        /// </devdoc>
        public abstract void Execute(XmlReader contextDocument, XmlResolver dataSources, XsltArgumentList argumentList, XmlWriter results);

        /// <devdoc>
        ///     <para>
        ///         Executes the query by accessing datasources via the XmlResolver and using
        ///         run-time parameters as provided by the XsltArgumentList. The default document
        ///         is mapped into the XmlResolver with the provided name. The results are output
        ///         to the provided TextWriter.
        ///     </para>
        /// </devdoc>
        public abstract void Execute(XmlReader contextDocument, XmlResolver dataSources, XsltArgumentList argumentList, TextWriter results);

        /// <devdoc>
        ///     <para>
        ///         Executes the query by accessing datasources via the XmlResolver and using
        ///         run-time parameters as provided by the XsltArgumentList. The default document
        ///         is mapped into the XmlResolver with the provided name. The results are output
        ///         to the provided Stream.
        ///     </para>
        /// </devdoc>
        public abstract void Execute(XmlReader contextDocument, XmlResolver dataSources, XsltArgumentList argumentList, Stream results);

        /// <devdoc>
        ///     <para>
        ///         Executes the query by accessing datasources via the XmlResolver and using
        ///         run-time parameters as provided by the XsltArgumentList. The default document
        ///         is mapped into the XmlResolver with the provided name. The results are returned
        ///         as an IList.
        ///     </para>
        /// </devdoc>
        public abstract IList Evaluate(XmlReader contextDocument, XmlResolver dataSources, XsltArgumentList argumentList);
    }
}
