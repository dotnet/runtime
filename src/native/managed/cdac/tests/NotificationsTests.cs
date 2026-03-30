// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class NotificationsTests
{
    private const ulong ModuleLoad = 1;
    private const ulong ModuleUnload = 2;
    private const ulong Exception = 5;
    private const ulong Gc = 6;
    private const ulong ExceptionCatcherEnter = 7;
    private const ulong Jit2 = 8;

    private static INotifications CreateContract()
    {
        var target = new TestPlaceholderTarget(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true }, (_, _) => -1);
        return ((IContractFactory<INotifications>)new NotificationsFactory()).CreateContract(target, 1);
    }

    private static ReadOnlySpan<TargetPointer> MakeExInfo(params ulong[] values)
    {
        TargetPointer[] arr = new TargetPointer[values.Length];
        for (int i = 0; i < values.Length; i++)
            arr[i] = new TargetPointer(values[i]);
        return arr;
    }

    [Theory]
    [InlineData(ModuleLoad, NotificationType.ModuleLoad)]
    [InlineData(ModuleUnload, NotificationType.ModuleUnload)]
    [InlineData(Jit2, NotificationType.Jit2)]
    [InlineData(Exception, NotificationType.Exception)]
    [InlineData(Gc, NotificationType.Gc)]
    [InlineData(ExceptionCatcherEnter, NotificationType.ExceptionCatcherEnter)]
    [InlineData(0ul, NotificationType.Unknown)]
    [InlineData(3ul, NotificationType.Unknown)] // JIT_NOTIFICATION (legacy, not handled)
    [InlineData(99ul, NotificationType.Unknown)]
    public void GetNotificationType_ReturnsExpectedType(ulong rawType, NotificationType expected)
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(rawType);
        Assert.Equal(expected, contract.GetNotificationType(exInfo));
    }

    [Fact]
    public void GetNotificationType_EmptySpan_ReturnsUnknown()
    {
        INotifications contract = CreateContract();
        Assert.Equal(NotificationType.Unknown, contract.GetNotificationType(ReadOnlySpan<TargetPointer>.Empty));
    }

    [Fact]
    public void ParseModuleLoadNotification_ReturnsModuleAddress()
    {
        INotifications contract = CreateContract();
        ulong expectedModule = 0x1234_5678_9ABC_DEF0ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleLoad, expectedModule);

        contract.ParseModuleLoadNotification(exInfo, out TargetPointer moduleAddress);

        Assert.Equal(expectedModule, moduleAddress.Value);
    }

    [Fact]
    public void ParseModuleUnloadNotification_ReturnsModuleAddress()
    {
        INotifications contract = CreateContract();
        ulong expectedModule = 0xDEAD_BEEF_0000_0001ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleUnload, expectedModule);

        contract.ParseModuleUnloadNotification(exInfo, out TargetPointer moduleAddress);

        Assert.Equal(expectedModule, moduleAddress.Value);
    }

    [Fact]
    public void ParseJITNotification_ReturnsAddresses()
    {
        INotifications contract = CreateContract();
        ulong expectedMethodDesc = 0x0000_1111_2222_3333ul;
        ulong expectedNativeCode = 0x0000_4444_5555_6666ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Jit2, expectedMethodDesc, expectedNativeCode);

        contract.ParseJITNotification(exInfo, out TargetPointer methodDescAddress, out TargetPointer nativeCodeAddress);

        Assert.Equal(expectedMethodDesc, methodDescAddress.Value);
        Assert.Equal(expectedNativeCode, nativeCodeAddress.Value);
    }

    [Fact]
    public void ParseExceptionNotification_ReturnsThreadAddress()
    {
        INotifications contract = CreateContract();
        ulong expectedThread = 0x0000_CAFE_BABE_0000ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Exception, expectedThread);

        contract.ParseExceptionNotification(exInfo, out TargetPointer threadAddress);

        Assert.Equal(expectedThread, threadAddress.Value);
    }

    [Fact]
    public void ParseGCNotification_ReturnsEventData()
    {
        INotifications contract = CreateContract();
        ulong gcMarkEndType = (ulong)GcEventType.MarkEnd;
        int condemnedGeneration = 2;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Gc, gcMarkEndType, (ulong)condemnedGeneration);

        bool result = contract.ParseGCNotification(exInfo, out GcEventData eventData);

        Assert.True(result);
        Assert.Equal(GcEventType.MarkEnd, eventData.EventType);
        Assert.Equal(condemnedGeneration, eventData.CondemnedGeneration);
    }

    [Fact]
    public void ParseGCNotification_ReturnsFalseForUnsupportedEventType()
    {
        INotifications contract = CreateContract();
        ulong unsupportedGcEventType = (ulong)GcEventType.MarkEnd + 1;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Gc, unsupportedGcEventType, 0);

        bool result = contract.ParseGCNotification(exInfo, out GcEventData eventData);

        Assert.False(result);
    }

    [Fact]
    public void ParseExceptionCatcherEnterNotification_ReturnsAddressAndOffset()
    {
        INotifications contract = CreateContract();
        ulong expectedMethodDesc = 0x0000_AAAA_BBBB_CCCCul;
        uint expectedOffset = 0x42;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ExceptionCatcherEnter, expectedMethodDesc, expectedOffset);

        contract.ParseExceptionCatcherEnterNotification(exInfo, out TargetPointer methodDescAddress, out uint nativeOffset);

        Assert.Equal(expectedMethodDesc, methodDescAddress.Value);
        Assert.Equal(expectedOffset, nativeOffset);
    }
}

