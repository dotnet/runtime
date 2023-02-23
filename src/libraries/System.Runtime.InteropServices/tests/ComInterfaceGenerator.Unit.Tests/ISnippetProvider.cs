// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal interface ISnippetProvider
    {
        public GeneratorKind Generator { get; }

        public string VirtualMethodIndex(
            int index,
            bool? ImplicitThisParameter = null,
            MarshalDirection? Direction = null,
            StringMarshalling? StringMarshalling = null,
            Type? StringMarshallingCustomType = null,
            bool? SetLastError = null,
            ExceptionMarshalling? ExceptionMarshalling = null,
            Type? ExceptionMarshallingType = null)
                => Generator switch
                {
                    GeneratorKind.ComInterfaceGenerator => "",
                    GeneratorKind.VTableIndexStubGenerator =>
                        "[global::System.Runtime.InteropServices.Marshalling.VirtualMethodIndexAttribute("
                        + index.ToString()
                        + (ImplicitThisParameter.HasValue ? $", ImplicitThisParameter = {ImplicitThisParameter.Value.ToString().ToLower()}" : "")
                        + (Direction is not null ? $", Direction = {typeof(MarshalDirection).FullName}.{Direction.Value}" : "")
                        + (StringMarshalling is not null ? $", StringMarshalling = {typeof(StringMarshalling).FullName}.{StringMarshalling!.Value}" : "")
                        + (StringMarshallingCustomType is not null ? $", StringMarshallingCustomType = {StringMarshallingCustomType!.FullName}" : "")
                        + (SetLastError is not null ? $", SetLastError = {SetLastError.Value.ToString().ToLower()}" : "")
                        + (ExceptionMarshalling is not null ? $", ExceptionMarshalling = {typeof(ExceptionMarshalling).FullName}.{ExceptionMarshalling.Value}" : "")
                        + (ExceptionMarshallingType is not null ? $", ExceptionMarshallingCustomType = {ExceptionMarshallingType!.FullName}" : "")
                        + ")]",
                    _ => throw new NotImplementedException()
                };

        public string UnmanagedObjectUnwrapper(Type t) => Generator switch
        {
            GeneratorKind.VTableIndexStubGenerator => $"[global::System.Runtime.InteropServices.Marshalling.UnmanagedObjectUnwrapperAttribute<{t.FullName!.Replace('+', '.')}>]",
            GeneratorKind.ComInterfaceGenerator => "",
            _ => throw new NotImplementedException(),
        };

        public string GeneratedComInterface => Generator switch
        {
            GeneratorKind.VTableIndexStubGenerator => "",
            GeneratorKind.ComInterfaceGenerator => $"[global::System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute]",
            _ => throw new NotImplementedException(),
        };
    }
}
