// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Xml.XPath
{
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;

    internal abstract class ResetableIterator : XPathNodeIterator
    {
        // the best place for this constructors to be is XPathNodeIterator, to avoid DCR at this time let's ground them here
        public ResetableIterator()
        {
            base.count = -1;
        }
        protected ResetableIterator(ResetableIterator other)
        {
            base.count = other.count;
        }
        protected void ResetCount()
        {
            base.count = -1;
        }

        public abstract void Reset();
        public virtual bool MoveToPosition(int pos)
        {
            Reset();
            for (int i = CurrentPosition; i < pos; i++)
            {
                if (!MoveNext())
                {
                    return false;
                }
            }
            return true;
        }

        // Contruct extension: CurrentPosition should return 0 if MoveNext() wasn't called after Reset()
        // (behavior is not defined for XPathNodeIterator)
        public abstract override int CurrentPosition { get; }
    }
}
