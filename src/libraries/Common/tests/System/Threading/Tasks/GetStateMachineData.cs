// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Fragile trick for getting a description of the current state of a .NET Core async method state machine.
    /// To use, first await FetchAsync to get back an object:
    ///     object box = await GetStateMachineData.FetchAsync();
    /// and then when the description is desired, use:
    ///     string description = GetStateMachineData.Describe(box);
    /// For example:
    ///     using (new Timer(s => Console.WriteLine(GetStateMachineData.Describe(s)), await GetStateMachineData.FetchAsync(), 60_000, 60_000))
    /// </summary>
    internal sealed class GetStateMachineData : ICriticalNotifyCompletion
    {
        private object _box;

        /// <summary>Returns an awaitable whose awaited result will be the boxed state machine for the async method.</summary>
        public static GetStateMachineData FetchAsync() => new GetStateMachineData();

        /// <summary>Creates a string representation of a boxed state machine object.</summary>
        public static string Describe(object box)
        {
            var sb = new StringBuilder();
            var seen = new HashSet<object>();
            Describe(box, sb, seen, 0);
            return sb.ToString();

            static void Describe(object box, StringBuilder sb, HashSet<object> seen, int indentLevel)
            {
                string indent = string.Concat(Enumerable.Repeat("    ", indentLevel));

                // If we were handed a null object (which should only happen from recursion),
                // state that the object was null.
                if (box is null)
                {
                    sb.Append(indent).AppendLine($"(Object was null.)");
                    return;
                }

                // If as we're walking a graph we happen upon a cycle, break it.
                if (!seen.Add(box))
                {
                    sb.Append(indent).AppendLine($"(Object already seen in graph walk.)");
                    return;
                }

                // Try to get the StateMachine field from the AsyncStateMachineBox<>.  If we can't,
                // just print out the details we can and bail.
                FieldInfo stateMachineField = box.GetType().GetField("StateMachine", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (stateMachineField is null)
                {
                    sb.Append(indent).AppendLine($"(Couldn't get state machine field from {box}.)");
                    sb.Append(indent).AppendLine(ToString(box));
                    return;
                }

                // Get the state machine from the StateMachine field.
                IAsyncStateMachine stateMachine = stateMachineField.GetValue(box) as IAsyncStateMachine;
                if (stateMachine is null)
                {
                    sb.Append(indent).AppendLine($"(Null state machine from {box}.)");
                    return;
                }

                // Print out the name of the state machine.
                Type stateMachineType = stateMachine.GetType();
                sb.Append(indent).AppendLine(stateMachineType.FullName);

                // Get all of the fields on the state machine, and print them all out.
                FieldInfo[] fields = stateMachineType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo fi in fields)
                {
                    // Print out the field name and its value.
                    object fiValue = fi.GetValue(stateMachine);
                    sb.Append(indent).AppendLine($"  {fi.Name}: {ToString(fiValue)}");

                    // If the field is an awaiter, recursively examine any tasks it directly references.
                    if (fiValue is ICriticalNotifyCompletion)
                    {
                        foreach (FieldInfo possibleTask in fiValue.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            if (possibleTask.GetValue(fiValue) is Task awaitedTask)
                            {
                                Describe(awaitedTask, sb, seen, indentLevel + 1);
                            }
                        }
                    }
                }
            }

            static string ToString(object value) =>
                value is null ? "(null)" :
                value is Task t ? $"Status: {t.Status}, Exception: {t.Exception?.InnerException}" :
                value.ToString();
        }

        private GetStateMachineData() { }
        public GetStateMachineData GetAwaiter() => this;
        public bool IsCompleted => false;
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation)
        {
            _box = continuation.Target;
            Task.Run(continuation);
        }
        public object GetResult() => _box;
    }
}
