// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal abstract XmlNode CurrentNode { get; }

        internal abstract bool Next();
        internal abstract bool NextRight();

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
        internal bool OnRowElement()
        {
            XmlBoundElement be = CurrentNode as XmlBoundElement;
            return (be != null) && (be.Row != null);
        }
    }
}
