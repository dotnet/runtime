// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

// P21 (D1): the write barrier of LXR logs the OLD value of every overwritten reference field, also when the new value
// is null. The framework is ReadyToRun compiled. These scenarios clear references through framework code (the stores
// are in the precompiled code of CoreLib) and check what LXR saw: every cleared reference must produce a decrement
// entry, and the objects that only the cleared references kept alive must die in the next reference-counting pauses.
// If a precompiled method did not keep the barrier for its null store, the decrement is missing and the cell fails.
internal static partial class Program
{
    private const int StoreObjects = 2000;

    private struct RefPair
    {
        internal object? First;
        internal int Padding;
        internal object? Second;
    }

    private static object[] MakeObjects(int count)
    {
        object[] objects = new object[count];
        for (int index = 0; index < count; index++)
        {
            objects[index] = new Node(index);
        }
        return objects;
    }

    // Each scenario builds a structure that holds StoreObjects objects only through the references that the returned
    // action clears, and returns the action. The count says how many references the action clears.
    private static (Action Clear, int References) PrepareListClear()
    {
        List<object> list = new(MakeObjects(StoreObjects));
        return (() => list.Clear(), StoreObjects);
    }

    private static (Action Clear, int References) PrepareListRemoveAt()
    {
        List<object> list = new(MakeObjects(StoreObjects));
        return (() =>
        {
            while (list.Count > 0)
            {
                list.RemoveAt(list.Count - 1);
            }
        }, StoreObjects);
    }

    private static (Action Clear, int References) PrepareArrayClear()
    {
        object[] array = MakeObjects(StoreObjects);
        return (() => Array.Clear(array), StoreObjects);
    }

    private static (Action Clear, int References) PrepareSpanClear()
    {
        object[] array = MakeObjects(StoreObjects);
        return (() => array.AsSpan().Clear(), StoreObjects);
    }

    private static (Action Clear, int References) PrepareSpanFillNull()
    {
        object?[] array = MakeObjects(StoreObjects);
        return (() => array.AsSpan().Fill(null), StoreObjects);
    }

    private static (Action Clear, int References) PrepareArrayCopyNulls()
    {
        object?[] array = MakeObjects(StoreObjects);
        object?[] nulls = new object?[StoreObjects];
        return (() => Array.Copy(nulls, array, StoreObjects), StoreObjects);
    }

    private static (Action Clear, int References) PrepareSpanCopyNulls()
    {
        object?[] array = MakeObjects(StoreObjects);
        object?[] nulls = new object?[StoreObjects];
        return (() => nulls.AsSpan().CopyTo(array), StoreObjects);
    }

    private static (Action Clear, int References) PrepareArraySetValueNull()
    {
        object[] array = MakeObjects(StoreObjects);
        return (() =>
        {
            for (int index = 0; index < array.Length; index++)
            {
                array.SetValue(null, index);
            }
        }, StoreObjects);
    }

    private static (Action Clear, int References) PrepareDictionaryRemove()
    {
        Dictionary<int, object> dictionary = new();
        object[] objects = MakeObjects(StoreObjects);
        for (int index = 0; index < objects.Length; index++)
        {
            dictionary[index] = objects[index];
        }
        return (() =>
        {
            for (int index = 0; index < StoreObjects; index++)
            {
                dictionary.Remove(index);
            }
        }, StoreObjects);
    }

    private static (Action Clear, int References) PrepareStructArrayClear()
    {
        RefPair[] array = new RefPair[StoreObjects / 2];
        object[] objects = MakeObjects(StoreObjects);
        for (int index = 0; index < array.Length; index++)
        {
            array[index].First = objects[2 * index];
            array[index].Second = objects[2 * index + 1];
        }
        return (() => Array.Clear(array), StoreObjects);
    }

    private static (Action Clear, int References) PrepareStructArrayCopyEmpty()
    {
        RefPair[] array = new RefPair[StoreObjects / 2];
        RefPair[] empty = new RefPair[StoreObjects / 2];
        object[] objects = MakeObjects(StoreObjects);
        for (int index = 0; index < array.Length; index++)
        {
            array[index].First = objects[2 * index];
            array[index].Second = objects[2 * index + 1];
        }
        return (() => Array.Copy(empty, array, array.Length), StoreObjects);
    }

    private static (Action Clear, int References) PrepareStructFieldAssign()
    {
        RefPair[] array = new RefPair[StoreObjects / 2];
        object[] objects = MakeObjects(StoreObjects);
        for (int index = 0; index < array.Length; index++)
        {
            array[index].First = objects[2 * index];
            array[index].Second = objects[2 * index + 1];
        }
        return (() =>
        {
            RefPair empty = default;
            for (int index = 0; index < array.Length; index++)
            {
                array[index] = empty;
            }
        }, StoreObjects);
    }

    private static (Action Clear, int References) PrepareInterlockedExchange()
    {
        object?[] array = MakeObjects(StoreObjects);
        return (() =>
        {
            for (int index = 0; index < array.Length; index++)
            {
                Interlocked.Exchange(ref array[index], null);
            }
        }, StoreObjects);
    }

    private static (Action Clear, int References) PrepareInterlockedCompareExchange()
    {
        object?[] array = MakeObjects(StoreObjects);
        return (() =>
        {
            for (int index = 0; index < array.Length; index++)
            {
                Interlocked.CompareExchange(ref array[index], null, array[index]);
            }
        }, StoreObjects);
    }

    private static (Action Clear, int References) PrepareBoxFieldClear()
    {
        object[] boxes = new object[StoreObjects / 2];
        object[] objects = MakeObjects(StoreObjects);
        for (int index = 0; index < boxes.Length; index++)
        {
            RefPair pair = default;
            pair.First = objects[2 * index];
            pair.Second = objects[2 * index + 1];
            boxes[index] = pair;
        }
        return (() =>
        {
            for (int index = 0; index < boxes.Length; index++)
            {
                // The store goes into a boxed struct on the heap.
                Unsafe.Unbox<RefPair>(boxes[index]) = default;
            }
        }, StoreObjects);
    }

    private static (Action Clear, int References) PrepareBoxFieldAssign()
    {
        object[] boxes = new object[StoreObjects];
        for (int index = 0; index < boxes.Length; index++)
        {
            RefPair pair = default;
            pair.First = new Node(index);
            boxes[index] = pair;
        }
        return (() =>
        {
            for (int index = 0; index < boxes.Length; index++)
            {
                Unsafe.Unbox<RefPair>(boxes[index]).First = null;
            }
        }, StoreObjects);
    }

    private static readonly (string Name, Func<(Action Clear, int References)> Prepare)[] s_frameworkStoreScenarios =
    [
        ("List.Clear", PrepareListClear),
        ("List.RemoveAt", PrepareListRemoveAt),
        ("Array.Clear", PrepareArrayClear),
        ("Span.Clear", PrepareSpanClear),
        ("Span.Fill(null)", PrepareSpanFillNull),
        ("Array.Copy of nulls", PrepareArrayCopyNulls),
        ("Span.CopyTo of nulls", PrepareSpanCopyNulls),
        ("Array.SetValue(null)", PrepareArraySetValueNull),
        ("Dictionary.Remove", PrepareDictionaryRemove),
        ("Array.Clear of a struct array", PrepareStructArrayClear),
        ("Array.Copy of empty structs", PrepareStructArrayCopyEmpty),
        ("struct copy into an array element", PrepareStructFieldAssign),
        ("Interlocked.Exchange", PrepareInterlockedExchange),
        ("Interlocked.CompareExchange", PrepareInterlockedCompareExchange),
        ("struct store into a boxed struct", PrepareBoxFieldClear),
        ("field store into a boxed struct", PrepareBoxFieldAssign),
    ];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Action Clear, int References) PrepareScenario(Func<(Action Clear, int References)> prepare) => prepare();

    private static void VerifyFrameworkOldValueStores(GetMetric metric, Collect collect)
    {
        const uint RefCountPause = 1;
        const uint Deaths = 9;
        const uint DecrementEntries = 24;
        foreach ((string name, Func<(Action Clear, int References)> prepare) in s_frameworkStoreScenarios)
        {
            (Action clear, int references) = PrepareScenario(prepare);
            // Two pauses: the objects become mature, their reference counts are set, and the fields that hold them
            // are marked "not logged yet".
            Check(collect(RefCountPause) == 0 && collect(RefCountPause) == 0, $"{name}: the first pauses failed.");
            ulong deathsBefore = metric(Deaths);
            ulong decrementsBefore = metric(DecrementEntries);
            clear();
            Check(collect(RefCountPause) == 0 && collect(RefCountPause) == 0 && collect(RefCountPause) == 0,
                $"{name}: the pauses after the stores failed.");
            ulong deaths = metric(Deaths) - deathsBefore;
            ulong decrements = metric(DecrementEntries) - decrementsBefore;
            Console.WriteLine($"framework_store {name}: references={references} decrement_entries={decrements} deaths={deaths}");
            Check(decrements >= (ulong)references * 9 / 10,
                $"{name}: the old values of the cleared references were not logged (decrement entries {decrements} of {references}).");
            Check(deaths >= (ulong)references * 9 / 10,
                $"{name}: the objects kept alive only by the cleared references did not die (deaths {deaths} of {references}).");
            GC.KeepAlive(clear);
        }
    }
}
