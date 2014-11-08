// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class ParentQuery : CacheAxisQuery
    {
        public ParentQuery(Query qyInput, string Name, string Prefix, XPathNodeType Type) : base(qyInput, Name, Prefix, Type) { }
        private ParentQuery(ParentQuery other) : base(other) { }

        public override object Evaluate(XPathNodeIterator context)
        {
            base.Evaluate(context);

            XPathNavigator input;
            while ((input = qyInput.Advance()) != null)
            {
                input = input.Clone();

                if (input.MoveToParent())
                {
                    if (matches(input))
                    {
                        Insert(outputBuffer, input);
                    }
                }
            }
            return this;
        }

        public override XPathNodeIterator Clone() { return new ParentQuery(this); }
    }
}
