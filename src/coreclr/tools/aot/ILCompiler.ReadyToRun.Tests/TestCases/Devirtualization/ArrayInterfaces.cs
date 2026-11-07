// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

public static class ArrayInterfaces
{
    public static int ListValueType(int[] array, int index) => ((IList<int>)array)[index];

    public static string ListReferenceType(string[] array, int index) => ((IList<string>)array)[index];

    public static object CovariantList(string[] array, int index) => ((IList<object>)array)[index];

    public static int CollectionCount(int[] array) => ((ICollection<int>)array).Count;

    public static int ReadOnlyCollectionCount(string[] array) => ((IReadOnlyCollection<string>)array).Count;

    public static int ReadOnlyList(int[] array, int index) => ((IReadOnlyList<int>)array)[index];

    public static IEnumerator<int> EnumerableValueType(int[] array) => ((IEnumerable<int>)array).GetEnumerator();

    public static IEnumerator<string> EnumerableReferenceType(string[] array) => ((IEnumerable<string>)array).GetEnumerator();

    public static IEnumerator<object> CovariantEnumerable(string[] array) => ((IEnumerable<object>)array).GetEnumerator();

    public static T SharedGeneric<T>(T[] array, int index) where T : class => ((IList<T>)array)[index];

    public static int NonGenericCollectionCount(int[] array) => ((ICollection)array).Count;
}
