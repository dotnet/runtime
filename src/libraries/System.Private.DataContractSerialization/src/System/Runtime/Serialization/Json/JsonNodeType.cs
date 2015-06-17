// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET_NATIVE || MERGE_DCJS
namespace System.Runtime.Serialization.Json
{
    internal enum JsonNodeType
    {
        None,
        Object,
        Element,
        EndElement,
        QuotedText,
        StandaloneText,
        Collection
    }
}
#endif
