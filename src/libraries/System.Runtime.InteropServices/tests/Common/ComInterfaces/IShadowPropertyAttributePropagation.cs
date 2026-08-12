// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(IID)]
    internal partial interface IShadowAttributePropagationBase
    {
        public const string IID = "67DFE7D0-95C3-4463-B59F-0825EB08AF24";

        [ShadowAttributeMarker(7)]
        int MarkedValue { get; set; }

        [MarshalUsing(typeof(ShadowAttributeIntMarshaller))]
        int MarshalledValue { get; set; }

        [ShadowAttributeMarker(13)]
        [MarshalUsing(typeof(ShadowAttributeIntMarshaller))]
        int MarkedAndMarshalledValue { get; set; }
    }

    [GeneratedComInterface]
    [Guid(IID)]
    internal partial interface IShadowAttributePropagationDerived : IShadowAttributePropagationBase
    {
        public new const string IID = "3463BFF4-7AAD-41B3-9110-E7E0D587E474";
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class ShadowAttributeMarkerAttribute : Attribute
    {
        public ShadowAttributeMarkerAttribute(int tag)
        {
            Tag = tag;
        }

        public int Tag { get; }
    }

    [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedIn, typeof(ShadowAttributeIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.UnmanagedToManagedIn, typeof(ShadowAttributeIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedOut, typeof(ShadowAttributeIntMarshaller))]
    [CustomMarshaller(typeof(int), MarshalMode.UnmanagedToManagedOut, typeof(ShadowAttributeIntMarshaller))]
    internal static class ShadowAttributeIntMarshaller
    {
        public static int ConvertToUnmanaged(int managed) => managed;
        public static int ConvertToManaged(int unmanaged) => unmanaged;
    }
}
