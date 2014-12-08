// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class XPathEmptyIterator : ResetableIterator
    {
        private XPathEmptyIterator() { }
        public override XPathNodeIterator Clone() { return this; }

        public override XPathNavigator Current
        {
            get { return null; }
        }

        public override int CurrentPosition
        {
            get { return 0; }
        }

        public override int Count
        {
            get { return 0; }
        }

        public override bool MoveNext()
        {
            return false;
        }

        public override void Reset() { }

        // -- Instance
        public static XPathEmptyIterator Instance = new XPathEmptyIterator();
    }
}
