// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Base for every paired cDAC/DAC proxy handed out by the validation shim.
/// </summary>
/// <remarks>
/// <para>
/// A proxy owns two objects: the one the production cDAC produced (authoritative — its result is
/// what the caller sees) and the one the legacy DAC produced for the same operation. Either side may
/// be absent: there is no legacy DAC when <c>DOTNET_CDAC_LEGACY_DAC_PATH</c> is unset, and there is
/// no cDAC object when an API fell back to the legacy DAC because the cDAC does not implement it.
/// </para>
/// <para>
/// When the caller hands one of these proxies back in (for example the <c>appDomain</c> argument of
/// <c>IXCLRDataProcess.GetDataByAddress</c>) it is unwrapped to the matching side before each call,
/// so the cDAC only ever sees cDAC objects and the legacy DAC only ever sees legacy DAC objects.
/// </para>
/// </remarks>
internal abstract partial class ShimProxy : ICustomQueryInterface
{
    protected readonly ValidationSession _session;

    protected ShimProxy(ValidationSession session, object? cdacObject, object? dacObject)
    {
        _session = session;
        CDacObject = cdacObject;
        DacObject = dacObject;
    }

    /// <summary>The production cDAC object being proxied, or <c>null</c> for a fallback-only proxy.</summary>
    internal object? CDacObject { get; }

    /// <summary>The legacy DAC object paired with <see cref="CDacObject"/>, when one exists.</summary>
    internal object? DacObject { get; }

    /// <summary>
    /// Decides whether an interface should be exposed. Normally the production cDAC object defines
    /// the surface; for a proxy that only has a legacy DAC object (an API that fell back) the legacy
    /// object defines it instead.
    /// </summary>
    protected CustomQueryInterfaceResult Support(object? cdacInterface, object? dacInterface)
    {
        bool supported = CDacObject is null ? dacInterface is not null : cdacInterface is not null;
        return supported ? CustomQueryInterfaceResult.NotHandled : CustomQueryInterfaceResult.Failed;
    }

    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out nint ppv)
    {
        // Every concrete proxy shadows this with its own public GetInterface; this exists so the base
        // can declare ICustomQueryInterface and satisfy the interface contract for proxies that do not.
        ppv = default;
        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>
    /// Unwraps an input COM object to the production cDAC object it is paired with.
    /// </summary>
    /// <remarks>
    /// Everything the shim receives on these parameters should be an object it previously handed
    /// out. Anything else is a caller-provided implementation the shim cannot pair, so it is passed
    /// through unchanged after asserting — silently swapping in a mismatched object would make the
    /// comparison meaningless.
    /// </remarks>
    internal static T? UnwrapCDac<T>(T? value)
        where T : class
    {
        if (value is null)
            return null;

        if (value is ShimProxy proxy)
            return proxy.CDacObject as T;

        Debug.Fail($"Expected a validation-shim proxy for {typeof(T).Name}; got a caller-provided object. Passing it through to the cDAC unchanged.");
        return value;
    }

    /// <summary>
    /// Unwraps an input COM object to the legacy DAC object it is paired with. Returns <c>null</c>
    /// when the proxy has no legacy DAC side, which suppresses the comparison for that call rather
    /// than handing the legacy DAC an object it did not create.
    /// </summary>
    internal static T? UnwrapDac<T>(T? value)
        where T : class
    {
        if (value is null)
            return null;

        if (value is ShimProxy proxy)
            return proxy.DacObject as T;

        Debug.Fail($"Expected a validation-shim proxy for {typeof(T).Name}; got a caller-provided object. Passing it through to the legacy DAC unchanged.");
        return value;
    }
}
