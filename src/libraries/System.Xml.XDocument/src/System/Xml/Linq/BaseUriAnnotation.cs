// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml.Linq
{
    class BaseUriAnnotation
    {
        internal readonly string baseUri;

        public BaseUriAnnotation(string baseUri)
        {
            this.baseUri = baseUri;
        }
    }
}