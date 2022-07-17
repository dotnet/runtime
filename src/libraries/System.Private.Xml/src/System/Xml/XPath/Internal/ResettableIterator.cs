// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal abstract class ResettableIterator : XPathNodeIterator
    {
        // the best place for this constructors to be is XPathNodeIterator, to avoid DCR at this time let's ground them here
        public ResettableIterator()
        {
            base.count = -1;
        }
        protected ResettableIterator(ResettableIterator other)
        {
            base.count = other.count;
        }
        protected void ResetCount()
        {
            base.count = -1;
        }

        public abstract void Reset();

        // Construct extension: CurrentPosition should return 0 if MoveNext() wasn't called after Reset()
        // (behavior is not defined for XPathNodeIterator)
        public abstract override int CurrentPosition { get; }
    }
}
