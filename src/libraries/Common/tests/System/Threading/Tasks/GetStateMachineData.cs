// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// Fragile trick for getting a description of the current state of a .NET Core async method state machine.
/// To use, first await FetchAsync to get back an object:
///     object box = await GetStateMachineData.FetchAsync();
/// and then when the description is desired, use:
///     string description = GetStateMachineData.Describe(box);
/// </summary>
namespace System.Threading.Tasks
{
    internal sealed class GetStateMachineData : ICriticalNotifyCompletion
    {
        private object _box;

        private GetStateMachineData() { }
        public static GetStateMachineData FetchAsync() => new GetStateMachineData();

        public GetStateMachineData GetAwaiter() => this;
        public bool IsCompleted => false;
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation)
        {
            _box = continuation.Target;
            Task.Run(continuation);
        }
        public object GetResult() => _box;

        public static string Describe(object box)
        {
            if (box is null)
            {
                return "(Couldn't get state machine box.)";
            }

            FieldInfo stateMachineField = box.GetType().GetField("StateMachine", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (stateMachineField is null)
            {
                return $"(Couldn't get state machine field from {box}.";
            }

            IAsyncStateMachine stateMachine = stateMachineField.GetValue(box) as IAsyncStateMachine;
            if (stateMachine is null)
            {
                return $"(Null state machine from {box}.)";
            }

            Type stateMachineType = stateMachine.GetType();
            FieldInfo[] fields = stateMachineType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var sb = new StringBuilder();
            sb.AppendLine(stateMachineType.FullName);
            foreach (FieldInfo fi in fields)
            {
                sb.AppendLine($"    {fi.Name}: {ToString(fi.GetValue(stateMachine))}");
            }
            return sb.ToString();
        }

        private static string ToString(object value)
        {
            if (value is null)
            {
                return "(null)";
            }

            if (value is Task t)
            {
                return $"Status: {t.Status}, Exception: {t.Exception?.InnerException}";
            }

            return value.ToString();
        }
    }
}
