// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public sealed class ComEventInterfaceAttribute : Attribute
    {
        public ComEventInterfaceAttribute(Type SourceInterface, Type EventProvider)
        {
            this.SourceInterface = SourceInterface;
            this.EventProvider = EventProvider;
        }

        public Type SourceInterface { get; }
        public Type EventProvider { get; }
    }
}