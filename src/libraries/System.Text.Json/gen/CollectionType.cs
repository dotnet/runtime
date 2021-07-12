// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Text.Json.SourceGeneration
{
    internal enum CollectionType
    {
        NotApplicable,
        Array,
        List,
        IEnumerable,
        IList,
        GenericIList,
        ISet,
        GenericICollection,
        GenericStack,
        GenericQueue,
        ConcurrentStack,
        ConcurrentQueue,
        GenericIEnumerable,
        StackOrQueue,
        ImmutableCollection,
        IDictionary,
        Dictionary,
        ImmutableDictionary,
        GenericIDictionary,
        IReadOnlyDictionary
    }
}
