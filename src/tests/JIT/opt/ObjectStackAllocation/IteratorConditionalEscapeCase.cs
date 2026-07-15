// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestLibrary;
using Xunit;

public static class IteratorConditionalEscapeCase
{
#if ITERATOR_CEA_DIRECT_LIST
    private const string CaseName = "DirectList";
#elif ITERATOR_CEA_DIRECT_ARRAY
    private const string CaseName = "DirectArray";
#elif ITERATOR_CEA_DIRECT_ENUMERABLE
    private const string CaseName = "DirectEnumerable";
#elif ITERATOR_CEA_DIRECT_IREADONLYLIST
    private const string CaseName = "DirectIReadOnlyList";
#elif ITERATOR_CEA_DIRECT_ILIST
    private const string CaseName = "DirectIList";
#elif ITERATOR_CEA_WHERE_LIST
    private const string CaseName = "WhereList";
#elif ITERATOR_CEA_WHERE_ARRAY
    private const string CaseName = "WhereArray";
#elif ITERATOR_CEA_WHERE_ENUMERABLE
    private const string CaseName = "WhereEnumerable";
#elif ITERATOR_CEA_SELECT_LIST
    private const string CaseName = "SelectList";
#elif ITERATOR_CEA_SELECT_ARRAY
    private const string CaseName = "SelectArray";
#elif ITERATOR_CEA_SELECT_ENUMERABLE
    private const string CaseName = "SelectEnumerable";
#elif ITERATOR_CEA_YIELD_ITERATOR
    private const string CaseName = "YieldIterator";
#elif ITERATOR_CEA_DEFAULT_IF_EMPTY_EMPTY_LIST
    private const string CaseName = "DefaultIfEmptyEmptyList";
#elif ITERATOR_CEA_DEFAULT_IF_EMPTY_EMPTY_ARRAY
    private const string CaseName = "DefaultIfEmptyEmptyArray";
#elif ITERATOR_CEA_DEFAULT_IF_EMPTY_EMPTY_ENUMERABLE
    private const string CaseName = "DefaultIfEmptyEmptyEnumerable";
#elif ITERATOR_CEA_TWO_WHERE_LIST_SERIAL
    private const string CaseName = "TwoWhereListSerial";
#elif ITERATOR_CEA_WHERE_LIST_THEN_SELECT_LIST_SERIAL
    private const string CaseName = "WhereListThenSelectListSerial";
#elif ITERATOR_CEA_TWO_WHERE_DIFFERENT_PARALLEL
    private const string CaseName = "TwoWhereDifferentParallel";
#elif ITERATOR_CEA_IF_FOREACH_DIFFERENT_TYPES_HOT_LIST
    private const string CaseName = "IfForeachDifferentTypesHotList";
#elif ITERATOR_CEA_IF_FOREACH_DIFFERENT_TYPES_HOT_ARRAY
    private const string CaseName = "IfForeachDifferentTypesHotArray";
#elif ITERATOR_CEA_NESTED_FOREACH_DIFFERENT_TYPES
    private const string CaseName = "NestedForeachDifferentTypes";
#elif ITERATOR_CEA_SKIP_LIST
    private const string CaseName = "SkipList";
#elif ITERATOR_CEA_TAKE_ARRAY
    private const string CaseName = "TakeArray";
#else
#error Missing iterator CEA case define.
#endif

    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static int TestEntryPoint()
    {
        return IteratorConditionalEscapeCommon.Run(CaseName);
    }
}
