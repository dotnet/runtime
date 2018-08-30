// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE1006 // Naming Styles

namespace Server.Contract.Servers
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Managed definition of CoClass 
    /// </summary>
    [ComImport]
    [CoClass(typeof(NumericTestingClass))]
    [Guid("05655A94-A915-4926-815D-A9EA648BAAD9")]
    internal interface NumericTesting : Server.Contract.INumericTesting
    {
    }

    /// <summary>
    /// Managed activation for CoClass
    /// </summary>
    [ComImport]
    [Guid(Server.Contract.Guids.NumericTesting)]
    internal class NumericTestingClass
    {
    }

    /// <summary>
    /// Managed definition of CoClass 
    /// </summary>
    [ComImport]
    [CoClass(typeof(ArrayTestingClass))]
    [Guid("7731CB31-E063-4CC8-BCD2-D151D6BC8F43")]
    internal interface ArrayTesting : Server.Contract.IArrayTesting
    {
    }

    /// <summary>
    /// Managed activation for CoClass
    /// </summary>
    [ComImport]
    [Guid(Server.Contract.Guids.ArrayTesting)]
    internal class ArrayTestingClass
    {
    }

    /// <summary>
    /// Managed definition of CoClass 
    /// </summary>
    [ComImport]
    [CoClass(typeof(StringTestingClass))]
    [Guid("7044C5C0-C6C6-4713-9294-B4A4E86D58CC")]
    internal interface StringTesting : Server.Contract.IStringTesting
    {
    }

    /// <summary>
    /// Managed activation for CoClass
    /// </summary>
    [ComImport]
    [Guid(Server.Contract.Guids.StringTesting)]
    internal class StringTestingClass
    {
    }

    /// <summary>
    /// Managed definition of CoClass 
    /// </summary>
    [ComImport]
    [CoClass(typeof(ErrorMarshalTestingClass))]
    [Guid("592386A5-6837-444D-9DE3-250815D18556")]
    internal interface ErrorMarshalTesting : Server.Contract.IErrorMarshalTesting
    {
    }

    /// <summary>
    /// Managed activation for CoClass
    /// </summary>
    [ComImport]
    [Guid(Server.Contract.Guids.ErrorMarshalTesting)]
    internal class ErrorMarshalTestingClass
    {
    }
}

#pragma warning restore IDE1006 // Naming Styles
