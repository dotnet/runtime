// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml.Linq
{
    class XObjectChangeAnnotation
    {
        internal EventHandler<XObjectChangeEventArgs> changing;
        internal EventHandler<XObjectChangeEventArgs> changed;

        public XObjectChangeAnnotation()
        {
        }
    }
}