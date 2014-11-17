// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class Variable : AstNode
    {
        private string localname;
        private string prefix;

        public Variable(string name, string prefix)
        {
            this.localname = name;
            this.prefix = prefix;
        }

        public override AstType Type { get { return AstType.Variable; } }
        public override XPathResultType ReturnType { get { return XPathResultType.Any; } }

        public string Localname { get { return localname; } }
        public string Prefix { get { return prefix; } }
    }
}
