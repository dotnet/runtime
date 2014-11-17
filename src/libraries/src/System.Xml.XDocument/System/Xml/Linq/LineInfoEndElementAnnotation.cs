// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml.Linq
{
    /// <summary>
    /// Instance of this class is used as an annotation on XElement nodes
    /// if that element is not empty element and we want to store the line info
    /// for its end element tag.
    /// </summary>
    class LineInfoEndElementAnnotation : LineInfoAnnotation
    {
        public LineInfoEndElementAnnotation(int lineNumber, int linePosition)
            : base(lineNumber, linePosition)
        { }
    }
}