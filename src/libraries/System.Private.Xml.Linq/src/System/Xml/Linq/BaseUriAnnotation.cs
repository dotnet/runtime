// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Linq
{
    internal sealed class BaseUriAnnotation
    {
        internal string baseUri;

        public BaseUriAnnotation(string baseUri)
        {
            this.baseUri = baseUri;
        }
    }
}
