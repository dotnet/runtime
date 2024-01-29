// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Unit.Tests
{
    /// <summary>
    /// Provides methods for adding attributes in a snippet.
    /// </summary>
    internal interface IComInterfaceAttributeProvider
    {
        /// <summary>
        /// Returns the [VirtualMethodIndexAttribute] to be put into a snippet if desired. Otherwise, returns <see cref="string.Empty" />.
        /// </summary>
        string VirtualMethodIndex(
            int index,
            bool? ImplicitThisParameter = null,
            MarshalDirection? Direction = null,
            StringMarshalling? StringMarshalling = null,
            Type? StringMarshallingCustomType = null,
            bool? SetLastError = null,
            ExceptionMarshalling? ExceptionMarshalling = null,
            Type? ExceptionMarshallingType = null);

        /// <summary>
        /// Returns the [UnmanagedObjectUnwrapper] to be put into a snippet if desired. Otherwise, returns <see cref="string.Empty" />.
        /// </summary>
        string UnmanagedObjectUnwrapper(Type t);

        /// <summary>
        /// Returns the [GeneratedComInterface] to be put into a snippet, if desired. Otherwise, returns <see cref="string.Empty" />.
        /// </summary>
        string GeneratedComInterface(StringMarshalling? stringMarshalling = null, Type? stringMarshallingCustomType = null);

        /// <summary>
        /// Returns any additional code to be appended to the snippet that provides any additional interfaces the user must implement
        /// for the generator to function correctly.
        /// </summary>
        string AdditionalUserRequiredInterfaces(string userDefinedInterfaceName);
    }
}
