// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;

namespace System.Runtime.CompilerServices
{
    [Serializable, AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class StateMachineAttribute : Attribute
    {
        public Type StateMachineType { get; private set; }

        public StateMachineAttribute(Type stateMachineType)
        {
            this.StateMachineType = stateMachineType;
        }
    }
}
