// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Linq
{
    internal sealed class XObjectChangeAnnotation
    {
        internal EventHandler<XObjectChangeEventArgs>? changing;
        internal EventHandler<XObjectChangeEventArgs>? changed;
    }
}
