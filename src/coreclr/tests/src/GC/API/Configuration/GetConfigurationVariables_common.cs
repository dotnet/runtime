// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class GetConfigurationVariables_common
{
    public static int AssertConfigurationVariables(IReadOnlyList<KeyValuePair<string, string>> expected)
    {
        // TODO: shouldn't need reflection once this is a public API
        Type gcType = typeof(GC);
        MethodInfo getConfigurationVariables = GetMethod(typeof(GC), "GetConfigurationVariables");
        // TODO: GC.GetConfigurationVariables().ToList();
        IReadOnlyList<KeyValuePair<string, string>> actual = ((IEnumerable<KeyValuePair<string, string>>) getConfigurationVariables.Invoke(null, new object[] { })!).ToList();

        if (!ArraysEqual(expected, actual, new KeyValuePairComparer<string, string>(EqualityComparer<string>.Default, EqualityComparer<string>.Default)))
        {
            // Expected
            // Actual
            Console.Error.WriteLine($"Expected : {ShowPairs(expected)}\nActual   : {ShowPairs(actual)}");
            return 1;
        }

        return 100;
    }

    public static KeyValuePair<K, V> Pair<K, V>(K k, V v) =>
        new KeyValuePair<K, V>(k, v);

    private static string ShowPairs(IEnumerable<KeyValuePair<string, string>> pairs) =>
        string.Join(", ", from pair in pairs select $"{pair.Key} => {pair.Value}");

    private static MethodInfo GetMethod(Type t, string name) =>
        t.GetMethod(name, BindingFlags.Public | BindingFlags.Static)
        ?? throw new Exception($"Type {t} has no public static method {name}");

    private static bool ArraysEqual<T>(IReadOnlyList<T> a, IReadOnlyList<T> b, IEqualityComparer<T> cmp) =>
        a.Count == b.Count
        && a.Zip(b).All(ab => cmp.Equals(ab.First, ab.Second));

    private class KeyValuePairComparer<K, V> : IEqualityComparer<KeyValuePair<K, V>>
    {
        private IEqualityComparer<K> compareK;
        private IEqualityComparer<V> compareV;

        public KeyValuePairComparer(IEqualityComparer<K> compareK, IEqualityComparer<V> compareV)
        {
            this.compareK = compareK;
            this.compareV = compareV;
        }

        bool IEqualityComparer<KeyValuePair<K, V>>.Equals(KeyValuePair<K, V> x, KeyValuePair<K, V> y) =>
            compareK.Equals(x.Key, y.Key) && compareV.Equals(x.Value, y.Value);

        int IEqualityComparer<KeyValuePair<K, V>>.GetHashCode(KeyValuePair<K, V> obj) =>
            throw new NotImplementedException();
    }
}
