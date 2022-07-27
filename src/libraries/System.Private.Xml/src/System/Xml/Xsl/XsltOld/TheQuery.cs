// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Xml.XPath;
using MS.Internal.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class TheQuery
    {
        internal InputScopeManager _ScopeManager;
        private readonly CompiledXpathExpr _CompiledQuery;

        internal CompiledXpathExpr CompiledQuery { get { return _CompiledQuery; } }

        internal TheQuery(CompiledXpathExpr compiledQuery, InputScopeManager manager)
        {
            _CompiledQuery = compiledQuery;
            _ScopeManager = manager.Clone();
        }
    }
}
