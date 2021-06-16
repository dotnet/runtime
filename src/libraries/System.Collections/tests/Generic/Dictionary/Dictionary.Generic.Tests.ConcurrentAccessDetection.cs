// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Generic.Dictionary
{
    public class DictionaryConcurrentAccessDetectionTests
    {
        private async Task DictionaryConcurrentAccessDetection<TKey, TValue>(Dictionary<TKey, TValue> dictionary, bool isValueType, object comparer, Action<Dictionary<TKey, TValue>> add, Action<Dictionary<TKey, TValue>> get, Action<Dictionary<TKey, TValue>> remove, Action<Dictionary<TKey, TValue>> removeOutParam)
        {
            Task task = Task.Factory.StartNew(() =>
            {
                // Get the Dictionary into a corrupted state, as if it had been corrupted by concurrent access.
                // We this deterministically by clearing the _entries array using reflection;
                // this means that every Entry struct has a 'next' field of zero, which causes the infinite loop
                // that we want Dictionary to break out of
                FieldInfo entriesType = dictionary.GetType().GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
                Array entriesInstance = (Array)entriesType.GetValue(dictionary);
                Array entryArray = (Array)Activator.CreateInstance(entriesInstance.GetType(), new object[] { ((IDictionary)dictionary).Count });
                entriesType.SetValue(dictionary, entryArray);

                Assert.Equal(comparer, dictionary.GetType().GetField("_comparer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dictionary));
                Assert.Equal(isValueType, dictionary.GetType().GetGenericArguments()[0].IsValueType);
                Assert.Throws<InvalidOperationException>(() => add(dictionary));
                Assert.Throws<InvalidOperationException>(() => get(dictionary));
                Assert.Throws<InvalidOperationException>(() => remove(dictionary));
                Assert.Throws<InvalidOperationException>(() => removeOutParam(dictionary));
            }, TaskCreationOptions.LongRunning);

            // If Dictionary regresses, we do not want to hang here indefinitely
            await task.WaitAsync(TimeSpan.FromSeconds(60));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(null)]
        [InlineData(typeof(CustomEqualityComparerInt32ValueType))]
        public async Task DictionaryConcurrentAccessDetection_ValueTypeKey(Type comparerType)
        {
            IEqualityComparer<int> customComparer = null;

            Dictionary<int, int> dic = comparerType == null ?
                new Dictionary<int, int>() :
                new Dictionary<int, int>((customComparer = (IEqualityComparer<int>)Activator.CreateInstance(comparerType)));

            dic.Add(1, 1);

            await DictionaryConcurrentAccessDetection(dic,
                typeof(int).IsValueType,
                customComparer,
                d => d.Add(1, 1),
                d => { var v = d[1]; },
                d => d.Remove(1),
                d => d.Remove(1, out int value));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(null)]
        [InlineData(typeof(CustomEqualityComparerDummyRefType))]
        public async Task DictionaryConcurrentAccessDetection_ReferenceTypeKey(Type comparerType)
        {
            IEqualityComparer<DummyRefType> customComparer = null;

            Dictionary<DummyRefType, DummyRefType> dic = comparerType == null ?
                new Dictionary<DummyRefType, DummyRefType>() :
                new Dictionary<DummyRefType, DummyRefType>((customComparer = (IEqualityComparer<DummyRefType>)Activator.CreateInstance(comparerType)));

            var keyValueSample = new DummyRefType() { Value = 1 };

            dic.Add(keyValueSample, keyValueSample);

            await DictionaryConcurrentAccessDetection(dic,
                typeof(DummyRefType).IsValueType,
                customComparer,
                d => d.Add(keyValueSample, keyValueSample),
                d => { var v = d[keyValueSample]; },
                d => d.Remove(keyValueSample),
                d => d.Remove(keyValueSample, out DummyRefType value));
        }
    }

    // We use a custom type instead of string because string use optimized comparer https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/shared/System/Collections/Generic/Dictionary.cs#L79
    // We want to test case with _comparer = null
    public class DummyRefType
    {
        public int Value { get; set; }
        public override bool Equals(object obj)
        {
            return ((DummyRefType)obj).Equals(this.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public class CustomEqualityComparerDummyRefType : EqualityComparer<DummyRefType>
    {
        public override bool Equals(DummyRefType x, DummyRefType y)
        {
            return x.Value == y.Value;
        }

        public override int GetHashCode(DummyRefType obj)
        {
            return obj.GetHashCode();
        }
    }

    public class CustomEqualityComparerInt32ValueType : EqualityComparer<int>
    {
        public override bool Equals(int x, int y)
        {
            return EqualityComparer<int>.Default.Equals(x, y);
        }

        public override int GetHashCode(int obj)
        {
            return EqualityComparer<int>.Default.GetHashCode(obj);
        }
    }
}
