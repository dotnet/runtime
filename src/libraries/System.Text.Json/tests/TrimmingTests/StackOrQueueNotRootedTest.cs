// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Text.Json;

/// <summary>
/// Tests that the System.Collections.NonGeneric dll is not dragged into a user's app
/// when excercising a code path that would check if an input type to JsonSerializer is
/// or derives from System.Collections.[Stack|Queue].
/// The reason ConcurrentStack/Queue are chosen is that the checks for assigning their
/// converters in the IEnumerableConverterFactory occurs after checks for Stack and Queue.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // Test serialization.
        var stack = new ConcurrentStack<int>();
        string json = JsonSerializer.Serialize(stack); // "[]"
        if (json != "[]")
        {
            return -1;
        }

        // Test deserialization.
        ConcurrentQueue<int> queue = JsonSerializer.Deserialize<ConcurrentQueue<int>>(json);
        if (!queue.IsEmpty)
        {
            return -1;
        }

        Type stackOrQueueType = GetTypeIfExists("System.Collections.Stack, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089") ??
            GetTypeIfExists("System.Collections.Queue, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

        return stackOrQueueType == null ? 100 : -1;
    }

    private static Type GetTypeIfExists(string name) => Type.GetType(name, false);
}
