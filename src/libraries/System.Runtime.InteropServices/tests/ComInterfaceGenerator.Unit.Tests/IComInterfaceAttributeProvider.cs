// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Unit.Tests
{
    /// <summary>
    /// Provides methods for adding attributes in a snippet if the generator requires them, or leaving them out if the generator doesn't require them.
    /// </summary>
    internal interface IComInterfaceAttributeProvider
    {
        public GeneratorKind Generator { get; }

        /// <summary>
        /// Returns the [VirtualMethodIndexAttribute] to be put into a snippet if Generator is <see cref="GeneratorKind.VTableIndexStubGenerator"/>, or an empty string if Generator is <see cref="GeneratorKind.ComInterfaceGenerator"/>.
        /// </summary>
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

        /// <summary>
        /// Returns the [UnmanagedObjectUnwrapper] to be put into a snippet if Generator is <see cref="GeneratorKind.VTableIndexStubGenerator"/>, or an empty string if Generator is <see cref="GeneratorKind.ComInterfaceGenerator"/>.
        /// </summary>
        public string UnmanagedObjectUnwrapper(Type t) => Generator switch
        {
            GeneratorKind.VTableIndexStubGenerator => $"[global::System.Runtime.InteropServices.Marshalling.UnmanagedObjectUnwrapperAttribute<{t.FullName!.Replace('+', '.')}>]",
            GeneratorKind.ComInterfaceGenerator => "",
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Returns the [ComInterfaceTypeAttribute] to be put into a snippet if Generator is <see cref="GeneratorKind.ComInterfaceGenerator"/>, or an empty string if Generator is <see cref="GeneratorKind.VTableIndexStubGenerator"/>.
        /// </summary>
        public string GeneratedComInterface => Generator switch
        {
            GeneratorKind.VTableIndexStubGenerator => "",
            GeneratorKind.ComInterfaceGenerator => $"[global::System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute]",
            _ => throw new NotImplementedException(),
        };
    }
}
