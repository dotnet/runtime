
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ManagedToNativeComInteropStubAttribute : Attribute
    {
        public ManagedToNativeComInteropStubAttribute(Type classType, string methodName)
        {
            ClassType = classType;
            MethodName = methodName;
        }

        public Type ClassType { get; }
        public string MethodName { get; }
    }
}
