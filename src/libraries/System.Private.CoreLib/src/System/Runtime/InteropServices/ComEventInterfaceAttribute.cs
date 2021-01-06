// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public sealed class ComEventInterfaceAttribute : Attribute
    {
        private const DynamicallyAccessedMemberTypes EventProviderAccessedMemberTypes =
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields;

        public ComEventInterfaceAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type SourceInterface,
            [DynamicallyAccessedMembers(EventProviderAccessedMemberTypes)] Type EventProvider)
        {
            this.SourceInterface = SourceInterface;
            this.EventProvider = EventProvider;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        public Type SourceInterface { get; }
        [DynamicallyAccessedMembers(EventProviderAccessedMemberTypes)]
        public Type EventProvider { get; }
    }
}
