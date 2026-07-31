// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataRequestTests
{
    public enum RequestType
    {
        Task,
        MethodDefinition,
        MethodInstance,
        Value,
    }

    [Theory]
    [InlineData(RequestType.Task, 3u)]
    [InlineData(RequestType.MethodDefinition, 1u)]
    [InlineData(RequestType.MethodInstance, 1u)]
    [InlineData(RequestType.Value, 3u)]
    public void Request_ReturnsRevision(RequestType type, uint expectedRevision)
    {
        object instance = CreateInstance(type);
        uint revision = 0;

        int hr = Request(
            instance,
            (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION,
            0,
            null,
            sizeof(uint),
            (byte*)&revision);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(expectedRevision, revision);
    }

    [Theory]
    [InlineData(RequestType.Task)]
    [InlineData(RequestType.MethodDefinition)]
    [InlineData(RequestType.MethodInstance)]
    [InlineData(RequestType.Value)]
    public void Request_InvalidArguments_ReturnsInvalidArgument(RequestType type)
    {
        object instance = CreateInstance(type);
        byte input = 0;
        uint revision = 0;
        byte* output = (byte*)&revision;
        uint revisionRequest = (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION;

        Assert.Equal(HResults.E_INVALIDARG, Request(instance, revisionRequest, 1, null, sizeof(uint), output));
        Assert.Equal(HResults.E_INVALIDARG, Request(instance, revisionRequest, 0, &input, sizeof(uint), output));
        Assert.Equal(HResults.E_INVALIDARG, Request(instance, uint.MaxValue, 0, null, sizeof(uint), output));
        Assert.Equal(HResults.E_INVALIDARG, Request(instance, revisionRequest, 0, null, 0, output));
    }

    [Theory]
    [InlineData(RequestType.Task)]
    [InlineData(RequestType.MethodDefinition)]
    [InlineData(RequestType.MethodInstance)]
    [InlineData(RequestType.Value)]
    public void Request_NullOutputBuffer_ReturnsPointerError(RequestType type)
    {
        object instance = CreateInstance(type);

        int hr = Request(
            instance,
            (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION,
            0,
            null,
            sizeof(uint),
            null);

        Assert.Equal(HResults.E_POINTER, hr);
    }

    private static object CreateInstance(RequestType type)
        => type switch
        {
            RequestType.Task => new ClrDataTask(default, null!, null),
            RequestType.MethodDefinition => new ClrDataMethodDefinition(null!, default, 0, null),
            RequestType.MethodInstance => new ClrDataMethodInstance(null!, default, default, null),
            RequestType.Value => new ClrDataValue(null!, 0, Array.Empty<NativeVarLocation>(), null),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

    private static int Request(
        object instance,
        uint reqCode,
        uint inBufferSize,
        byte* inBuffer,
        uint outBufferSize,
        byte* outBuffer)
        => instance switch
        {
            IXCLRDataTask task => task.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer),
            IXCLRDataMethodDefinition methodDefinition => methodDefinition.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer),
            IXCLRDataMethodInstance methodInstance => methodInstance.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer),
            IXCLRDataValue value => value.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer),
            _ => throw new ArgumentException("Unsupported CLR data instance.", nameof(instance)),
        };
}
