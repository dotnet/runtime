// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    // Iterates over non-attribute nodes
    internal abstract class BaseTreeIterator
    {
        protected DataSetMapper mapper;

        internal BaseTreeIterator(DataSetMapper mapper)
        {
            this.mapper = mapper;
        }

        internal abstract XmlNode? CurrentNode { get; }

        [MemberNotNullWhen(true, nameof(CurrentNode))]
        internal abstract bool Next();

        [MemberNotNullWhen(true, nameof(CurrentNode))]
        internal abstract bool NextRight();

        [MemberNotNullWhen(true, nameof(CurrentNode))]
        internal bool NextRowElement()
        {
            while (Next())
            {
                if (OnRowElement())
                {
                    return true;
                }
            }
            return false;
        }

        [MemberNotNullWhen(true, nameof(CurrentNode))]
        internal bool NextRightRowElement()
        {
            if (NextRight())
            {
                if (OnRowElement())
                {
                    return true;
                }
                return NextRowElement();
            }
            return false;
        }

        // Returns true if the current node is on a row element (head of a region)
        [MemberNotNullWhen(true, nameof(CurrentNode))]
        internal bool OnRowElement()
        {
            return CurrentNode is XmlBoundElement be && be.Row != null;
        }
    }
}
