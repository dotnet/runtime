// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace System.Xml
{
    internal sealed class XmlChildEnumerator : IEnumerator
    {
        internal XmlNode container;
        internal XmlNode child;
        internal bool isFirst;

        internal XmlChildEnumerator(XmlNode container)
        {
            this.container = container;
            this.child = container.FirstChild;
            this.isFirst = true;
        }

        bool IEnumerator.MoveNext()
        {
            return this.MoveNext();
        }

        internal bool MoveNext()
        {
            if (isFirst)
            {
                child = container.FirstChild;
                isFirst = false;
            }
            else if (child != null)
            {
                child = child.NextSibling;
            }

            return child != null;
        }

        void IEnumerator.Reset()
        {
            isFirst = true;
            child = container.FirstChild;
        }

        object IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }

        internal XmlNode Current
        {
            get
            {
                if (isFirst || child == null)
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);

                return child;
            }
        }
    }
}
