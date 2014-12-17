// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Text;

namespace System.Xml.XPath
{
    [DebuggerDisplay("Position={CurrentPosition}, Current={debuggerDisplayProxy}")]
    public abstract class XPathNodeIterator : IEnumerable
    {
        internal int count = -1;

        public abstract XPathNodeIterator Clone();
        public abstract bool MoveNext();
        public abstract XPathNavigator Current { get; }
        public abstract int CurrentPosition { get; }
        public virtual int Count
        {
            get
            {
                if (count == -1)
                {
                    XPathNodeIterator clone = this.Clone();
                    while (clone.MoveNext()) ;
                    count = clone.CurrentPosition;
                }
                return count;
            }
        }
        public virtual IEnumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        private object debuggerDisplayProxy { get { return Current == null ? null : (object)new XPathNavigator.DebuggerDisplayProxy(Current); } }

        /// <summary>
        /// Implementation of a resetable enumerator that is linked to the XPathNodeIterator used to create it.
        /// </summary>
        private class Enumerator : IEnumerator
        {
            private XPathNodeIterator original;     // Keep original XPathNodeIterator in case Reset() is called
            private XPathNodeIterator current;
            private bool iterationStarted;

            public Enumerator(XPathNodeIterator original)
            {
                this.original = original.Clone();
            }

            public virtual object Current
            {
                get
                {
                    // 1. Do not reuse the XPathNavigator, as we do in XPathNodeIterator
                    // 2. Throw exception if current position is before first node or after the last node
                    if (this.iterationStarted)
                    {
                        // Current is null if iterator is positioned after the last node
                        if (this.current == null)
                            throw new InvalidOperationException(SR.Format(SR.Sch_EnumFinished, string.Empty));

                        return this.current.Current.Clone();
                    }

                    // User must call MoveNext before accessing Current property
                    throw new InvalidOperationException(SR.Sch_EnumNotStarted);
                }
            }

            public virtual bool MoveNext()
            {
                // Delegate to XPathNodeIterator
                if (!this.iterationStarted)
                {
                    // Reset iteration to original position
                    this.current = this.original.Clone();
                    this.iterationStarted = true;
                }

                if (this.current == null || !this.current.MoveNext())
                {
                    // Iteration complete
                    this.current = null;
                    return false;
                }
                return true;
            }

            public virtual void Reset()
            {
                this.iterationStarted = false;
            }
        }

        private struct DebuggerDisplayProxy
        {
            private XPathNodeIterator nodeIterator;

            public DebuggerDisplayProxy(XPathNodeIterator nodeIterator)
            {
                this.nodeIterator = nodeIterator;
            }

            public override string ToString()
            {
                // Position={CurrentPosition}, Current={Current == null ? null : (object) new XPathNavigator.DebuggerDisplayProxy(Current)}
                StringBuilder sb = new StringBuilder();
                sb.Append("Position=");
                sb.Append(nodeIterator.CurrentPosition);
                sb.Append(", Current=");
                if (nodeIterator.Current == null)
                {
                    sb.Append("null");
                }
                else
                {
                    sb.Append('{');
                    sb.Append(new XPathNavigator.DebuggerDisplayProxy(nodeIterator.Current).ToString());
                    sb.Append('}');
                }
                return sb.ToString();
            }
        }
    }
}
