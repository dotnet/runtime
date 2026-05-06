// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Linq
{
    /// <summary>
    /// Instance of this class is used as an annotation on XElement nodes
    /// if that element is not empty element and we want to store the line info
    /// for its end element tag.
    /// </summary>
    internal sealed class LineInfoEndElementAnnotation : LineInfoAnnotation
    {
        public LineInfoEndElementAnnotation(int lineNumber, int linePosition)
            : base(lineNumber, linePosition)
        { }
    }
}
