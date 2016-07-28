// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt
{
    /// <summary>
    /// Several annotations are created and attached to Qil nodes during Xslt compilation.
    /// </summary>
    internal class XsltAnnotation : ListBase<object>
    {
        private object _arg0, _arg1, _arg2;


        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Create and initialize XsltAnnotation for the specified node.
        /// Allow properties to be read and written.
        /// </summary>
        public static XsltAnnotation Write(QilNode nd)
        {
            XsltAnnotation ann = nd.Annotation as XsltAnnotation;

            if (ann == null)
            {
                ann = new XsltAnnotation();
                nd.Annotation = ann;
            }

            return ann;
        }

        private XsltAnnotation()
        {
        }


        //-----------------------------------------------
        // ListBase implementation
        //-----------------------------------------------

        /// <summary>
        /// List of annotations can be updated.
        /// </summary>
        public override bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Return the count of sub-annotations maintained by this annotation.
        /// </summary>
        public override int Count
        {
            get { return 3; }
        }

        /// <summary>
        /// Return the annotation at the specified index.
        /// </summary>
        public override object this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return _arg0;
                    case 1: return _arg1;
                    case 2: return _arg2;
                }

                throw new IndexOutOfRangeException();
            }
            set
            {
                switch (index)
                {
                    case 0: _arg0 = value; return;
                    case 1: _arg1 = value; return;
                    case 2: _arg2 = value; return;
                }
                throw new IndexOutOfRangeException();
            }
        }
    }
}

