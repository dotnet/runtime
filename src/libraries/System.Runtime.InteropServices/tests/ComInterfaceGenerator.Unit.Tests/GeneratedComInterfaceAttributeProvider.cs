// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal class GeneratedComInterfaceAttributeProvider : IComInterfaceAttributeProvider
    {
        public string VirtualMethodIndex(
            int index,
            bool? ImplicitThisParameter = null,
            MarshalDirection? Direction = null,
            StringMarshalling? StringMarshalling = null,
            Type? StringMarshallingCustomType = null,
            bool? SetLastError = null,
            ExceptionMarshalling? ExceptionMarshalling = null,
            Type? ExceptionMarshallingType = null)
                => "";

        public string UnmanagedObjectUnwrapper(Type t) => "";

        public string GeneratedComInterface => @$"[global::System.Runtime.InteropServices.Marshalling.GeneratedComInterface, global::System.Runtime.InteropServices.Guid(""0A52B77C-E08B-4274-A1F4-1A2BF2C07E60"")]";

        public string AdditionalUserRequiredInterfaces(string userDefinedInterfaceName) => "";
    }
}
