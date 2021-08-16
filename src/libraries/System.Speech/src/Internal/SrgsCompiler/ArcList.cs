// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Speech.Internal.SrgsCompiler
{
#if DEBUG
    [DebuggerDisplay("Count #{Count}")]
    [DebuggerTypeProxy(typeof(ArcListDebugDisplay))]
#endif
    internal class ArcList : RBList
    {
        #region Internal Methods

        /// <summary>
        /// Build a List with all the arcs
        /// </summary>
        internal List<Arc> ToList()
        {
            List<Arc> collection = new();
            foreach (Arc arc in this)
            {
                collection.Add(arc);
            }
            return collection;
        }

        protected override int CompareTo(object arc1, object arc2)
        {
            return Arc.CompareContentForKey((Arc)arc1, (Arc)arc2);
        }

        #endregion

        #region Internal Properties

        internal new Arc First
        {
            get
            {
                return (Arc)base.First;
            }
        }

        #endregion

        #region Private Members

#if DEBUG
        private int Count
        {
            get
            {
                int count = 0;
                foreach (Arc arc in this)
                {
                    count++;
                }
                return count;
            }
        }

        // Used by the debugger display attribute
        private sealed class ArcListDebugDisplay
        {
            public ArcListDebugDisplay(ArcList item)
            {
                _item = item;
            }
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public Arc[] AKeys
            {
                get
                {
                    Arc[] item = new Arc[_item.Count];
                    int i = 0;
                    foreach (Arc arc in _item)
                    {
                        item[i++] = arc;
                    }
                    return item;
                }
            }

            private ArcList _item;
        }
#endif
        #endregion
    }
}
