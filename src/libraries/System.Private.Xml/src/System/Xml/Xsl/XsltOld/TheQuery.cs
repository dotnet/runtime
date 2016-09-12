// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using MS.Internal.Xml.XPath;

    internal sealed class TheQuery
    {
        internal InputScopeManager _ScopeManager;
        private CompiledXpathExpr _CompiledQuery;

        internal CompiledXpathExpr CompiledQuery { get { return _CompiledQuery; } }

        internal TheQuery(CompiledXpathExpr compiledQuery, InputScopeManager manager)
        {
            _CompiledQuery = compiledQuery;
            _ScopeManager = manager.Clone();
        }
    }
}
