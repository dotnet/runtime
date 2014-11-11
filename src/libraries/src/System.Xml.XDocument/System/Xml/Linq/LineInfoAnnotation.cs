// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CultureInfo = System.Globalization.CultureInfo;
using Debug = System.Diagnostics.Debug;
using IEnumerable = System.Collections.IEnumerable;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using Enumerable = System.Linq.Enumerable;
using IComparer = System.Collections.IComparer;
using IEqualityComparer = System.Collections.IEqualityComparer;
using StringBuilder = System.Text.StringBuilder;
using Encoding = System.Text.Encoding;
using Interlocked = System.Threading.Interlocked;
using System.Reflection;

namespace System.Xml.Linq
{
    /// <summary>
    /// Instance of this class is used as an annotation on any node
    /// for which we want to store its line information.
    /// Note: on XElement nodes this annotation stores the line info
    ///   for the element start tag. The matching end tag line info
    ///   if present is stored using the LineInfoEndElementAnnotation
    ///   instance annotation.
    /// </summary>
    class LineInfoAnnotation
    {
        internal int lineNumber;
        internal int linePosition;

        public LineInfoAnnotation(int lineNumber, int linePosition)
        {
            this.lineNumber = lineNumber;
            this.linePosition = linePosition;
        }
    }
}