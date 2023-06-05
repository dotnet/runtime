// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal class VirtualMethodIndexAttributeProvider : IComInterfaceAttributeProvider
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
                => "[global::System.Runtime.InteropServices.Marshalling.VirtualMethodIndexAttribute("
                        + index.ToString()
                        + (ImplicitThisParameter.HasValue ? $", ImplicitThisParameter = {ImplicitThisParameter.Value.ToString().ToLower()}" : "")
                        + (Direction is not null ? $", Direction = {typeof(MarshalDirection).FullName}.{Direction.Value}" : "")
                        + (StringMarshalling is not null ? $", StringMarshalling = {typeof(StringMarshalling).FullName}.{StringMarshalling!.Value}" : "")
                        + (StringMarshallingCustomType is not null ? $", StringMarshallingCustomType = typeof({StringMarshallingCustomType!.FullName})" : "")
                        + (SetLastError is not null ? $", SetLastError = {SetLastError.Value.ToString().ToLower()}" : "")
                        + (ExceptionMarshalling is not null ? $", ExceptionMarshalling = {typeof(ExceptionMarshalling).FullName}.{ExceptionMarshalling.Value}" : "")
                        + (ExceptionMarshallingType is not null ? $", ExceptionMarshallingCustomType = typeof({ExceptionMarshallingType!.FullName})" : "")
                        + ")]";

        public string UnmanagedObjectUnwrapper(Type t) => $"[global::System.Runtime.InteropServices.Marshalling.UnmanagedObjectUnwrapperAttribute<{t.FullName!.Replace('+', '.')}>]";

        public string GeneratedComInterface(StringMarshalling? stringMarshalling = null, Type? stringMarshallingCustomType = null) => "";

        public string AdditionalUserRequiredInterfaces(string userDefinedInterfaceName) => """
            partial interface INativeAPI : IUnmanagedInterfaceType
            {
                static unsafe void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation => null;
            }
            """;
    }
}
