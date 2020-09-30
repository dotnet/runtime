// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class StateMachineAttribute : Attribute
    {
        public StateMachineAttribute(Type stateMachineType)
        {
            StateMachineType = stateMachineType;
        }

        public Type StateMachineType { get; }
    }
}
