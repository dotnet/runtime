// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable 618 // Must test deprecated features

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

    /// <summary>
    /// Managed definition of CoClass 
    /// </summary>
    [ComImport]
    [CoClass(typeof(DispatchTestingClass))]
    [Guid("a5e04c1c-474e-46d2-bbc0-769d04e12b54")]
    internal interface DispatchTesting : Server.Contract.IDispatchTesting
    {
    }

    /// <summary>
    /// Managed activation for CoClass
    /// </summary>
    [ComImport]
    [Guid(Server.Contract.Guids.DispatchTesting)]
    internal class DispatchTestingClass
    {
    }

    /// <summary>
    /// Managed definition of CoClass 
    /// </summary>
    [ComImport]
    [CoClass(typeof(AggregationTestingClass))]
    [Guid("98cc27f0-d521-4f79-8b63-e980e3a92974")]
    internal interface AggregationTesting : Server.Contract.IAggregationTesting
    {
    }

    /// <summary>
    /// Managed activation for CoClass
    /// </summary>
    [ComImport]
    [Guid(Server.Contract.Guids.AggregationTesting)]
    internal class AggregationTestingClass
    {
    }

    /// <summary>
    /// Managed definition of CoClass 
    /// </summary>
    [ComImport]
    [CoClass(typeof(ColorTestingClass))]
    [Guid("E6D72BA7-0936-4396-8A69-3B76DA1108DA")]
    internal interface ColorTesting : Server.Contract.IColorTesting
    {
    }

    /// <summary>
    /// Managed activation for CoClass
    /// </summary>
    [ComImport]
    [Guid(Server.Contract.Guids.ColorTesting)]
    internal class ColorTestingClass
    {
    }

    /// <summary>
    /// Managed definition of CoClass
    /// </summary>
    [ComImport]
    [CoClass(typeof(LicenseTestingClass))]
    [Guid("6C9E230E-411F-4219-ABFD-E71F2B84FD50")]
    internal interface LicenseTesting : Server.Contract.ILicenseTesting
    {
    }

    /// <summary>
    /// Managed activation for CoClass
    /// </summary>
    [ComImport]
    [Guid(Server.Contract.Guids.LicenseTesting)]
    internal class LicenseTestingClass
    {
    }

/** Implement when main line C# compiler supports default interfaces.

    /// <summary>
    /// Managed definition of CoClass
    /// </summary>
    [ComImport]
    [CoClass(typeof(DefaultInterfaceTestingClass))]
    [Guid("FB6DF997-4CEF-4DF7-ADBD-E7FA395A7E0C")]
    internal interface DefaultInterfaceTesting : Server.Contract.IDefaultInterfaceTesting
    {
    }

    /// <summary>
    /// Managed activation for CoClass
    /// </summary>
    [ComImport]
    [Guid(Server.Contract.Guids.DefaultInterfaceTesting)]
    internal class DefaultInterfaceTestingClass
    {
    }
*/

    /// <summary>
    /// Managed definition of CoClass
    /// </summary>
    /// <remarks>
    /// This interface is used to test consumption of the NET server from a NET client only.
    /// </remarks>
    [ComImport]
    [CoClass(typeof(ConsumeNETServerTestingClass))]
    [Guid("CCBC1915-3252-4F6B-98AA-411CE6213D94")]
    internal interface ConsumeNETServerTesting : Server.Contract.IConsumeNETServer
    {
    }

    /// <summary>
    /// Managed activation for CoClass
    /// </summary>
    /// <remarks>
    /// This interface is used to test consumption of the NET server from a NET client only.
    /// </remarks>
    [ComImport]
    [Guid(Server.Contract.Guids.ConsumeNETServerTesting)]
    internal class ConsumeNETServerTestingClass
    {
    }

    [ComImport]
    [CoClass(typeof(InspectableTestingClass))]
    [Guid("3021236a-2a9e-4a29-bf14-533842c55262")]
    internal interface InspectableTesting : Server.Contract.IInspectableTesting
    {
    }

    [ComImport]
    [Guid(Server.Contract.Guids.InspectableTesting)]
    internal class InspectableTestingClass
    {
    }

    [ComImport]
    [CoClass(typeof(TrackMyLifetimeTestingClass))]
    [Guid("57f396a1-58a0-425f-8807-9f938a534984")]
    internal interface TrackMyLifetimeTesting : Server.Contract.ITrackMyLifetimeTesting
    {
    }

    [ComImport]
    [Guid(Server.Contract.Guids.TrackMyLifetimeTesting)]
    internal class TrackMyLifetimeTestingClass
    {
    }
}

#pragma warning restore 618 // Must test deprecated features
#pragma warning restore IDE1006 // Naming Styles
