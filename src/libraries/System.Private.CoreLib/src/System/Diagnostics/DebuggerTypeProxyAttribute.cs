// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DebuggerTypeProxyAttribute : Attribute
    {
        private Type? _target;

        public DebuggerTypeProxyAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            ProxyTypeName = type.AssemblyQualifiedName!;
        }

        public DebuggerTypeProxyAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string typeName)
        {
            ProxyTypeName = typeName;
        }

        // The Proxy is only invoked by the debugger, so it needs to have its
        // members preserved
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public string ProxyTypeName { get; }

        public Type? Target
        {
            get => _target;
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                TargetTypeName = value.AssemblyQualifiedName;
                _target = value;
            }
        }

        public string? TargetTypeName { get; set; }
    }
}
