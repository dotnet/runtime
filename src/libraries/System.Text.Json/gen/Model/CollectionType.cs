// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.SourceGeneration
{
    public enum CollectionType
    {
        NotApplicable,
        // Dictionary types
        IDictionary,
        Dictionary,
        ImmutableDictionary,
        IDictionaryOfTKeyTValue,
        IReadOnlyDictionary,
        // Non-dictionary types
        Array,
        List,
        IEnumerable,
        IList,
        IListOfT,
        ISet,
        ICollectionOfT,
        StackOfT,
        QueueOfT,
        ConcurrentStack,
        ConcurrentQueue,
        IAsyncEnumerableOfT,
        IEnumerableOfT,
        Stack,
        Queue,
        ImmutableEnumerable
    }
}
