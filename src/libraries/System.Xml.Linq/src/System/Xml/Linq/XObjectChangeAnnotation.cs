// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Linq
{
    internal class XObjectChangeAnnotation
    {
        internal EventHandler<XObjectChangeEventArgs> changing;
        internal EventHandler<XObjectChangeEventArgs> changed;

        public XObjectChangeAnnotation()
        {
        }
    }
}
