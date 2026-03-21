// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class NotificationsTests
{
    // Internal DACNotify type values (mirrors src/coreclr/vm/util.hpp DACNotify enum).
    private const ulong ModuleLoad = 1;
    private const ulong ModuleUnload = 2;
    private const ulong Exception = 5;
    private const ulong Gc = 6;
    private const ulong ExceptionCatcherEnter = 7;
    private const ulong Jit2 = 8;

    private static INotifications CreateContract()
    {
        // Notifications_1 parse methods are purely computational; no target memory is accessed.
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
    [InlineData(Jit2, NotificationType.Jit)]
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
    public void TryParseModuleLoadNotification_ValidArgs_ReturnsModuleAddress()
    {
        INotifications contract = CreateContract();
        ulong expectedModule = 0x1234_5678_9ABC_DEF0ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleLoad, expectedModule);

        bool result = contract.TryParseModuleLoadNotification(exInfo, out TargetPointer moduleAddress);

        Assert.True(result);
        Assert.Equal(expectedModule, moduleAddress.Value);
    }

    [Fact]
    public void TryParseModuleLoadNotification_WrongType_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleUnload, 0x1000ul);

        Assert.False(contract.TryParseModuleLoadNotification(exInfo, out _));
    }

    [Fact]
    public void TryParseModuleLoadNotification_TooFewArgs_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleLoad); // missing module ptr

        Assert.False(contract.TryParseModuleLoadNotification(exInfo, out _));
    }

    [Fact]
    public void TryParseModuleUnloadNotification_ValidArgs_ReturnsModuleAddress()
    {
        INotifications contract = CreateContract();
        ulong expectedModule = 0xDEAD_BEEF_0000_0001ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleUnload, expectedModule);

        bool result = contract.TryParseModuleUnloadNotification(exInfo, out TargetPointer moduleAddress);

        Assert.True(result);
        Assert.Equal(expectedModule, moduleAddress.Value);
    }

    [Fact]
    public void TryParseModuleUnloadNotification_WrongType_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleLoad, 0x1000ul);

        Assert.False(contract.TryParseModuleUnloadNotification(exInfo, out _));
    }

    [Fact]
    public void TryParseJITNotification_ValidArgs_ReturnsAddresses()
    {
        INotifications contract = CreateContract();
        ulong expectedMethodDesc = 0x0000_1111_2222_3333ul;
        ulong expectedNativeCode = 0x0000_4444_5555_6666ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Jit2, expectedMethodDesc, expectedNativeCode);

        bool result = contract.TryParseJITNotification(exInfo, out TargetPointer methodDescAddress, out TargetPointer nativeCodeAddress);

        Assert.True(result);
        Assert.Equal(expectedMethodDesc, methodDescAddress.Value);
        Assert.Equal(expectedNativeCode, nativeCodeAddress.Value);
    }

    [Fact]
    public void TryParseJITNotification_WrongType_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleLoad, 0x1000ul, 0x2000ul);

        Assert.False(contract.TryParseJITNotification(exInfo, out _, out _));
    }

    [Fact]
    public void TryParseJITNotification_TooFewArgs_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Jit2, 0x1000ul); // missing native code

        Assert.False(contract.TryParseJITNotification(exInfo, out _, out _));
    }

    [Fact]
    public void TryParseExceptionNotification_ValidArgs_ReturnsThreadAddress()
    {
        INotifications contract = CreateContract();
        ulong expectedThread = 0x0000_CAFE_BABE_0000ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Exception, expectedThread);

        bool result = contract.TryParseExceptionNotification(exInfo, out TargetPointer threadAddress);

        Assert.True(result);
        Assert.Equal(expectedThread, threadAddress.Value);
    }

    [Fact]
    public void TryParseExceptionNotification_WrongType_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleLoad, 0x1000ul);

        Assert.False(contract.TryParseExceptionNotification(exInfo, out _));
    }

    [Fact]
    public void TryParseGCNotification_GcMarkEnd_ReturnsEventData()
    {
        INotifications contract = CreateContract();
        ulong gcMarkEndType = (ulong)GcEventType.MarkEnd;
        int condemnedGeneration = 2;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Gc, gcMarkEndType, (ulong)condemnedGeneration);

        bool result = contract.TryParseGCNotification(exInfo, out GcEventData eventData);

        Assert.True(result);
        Assert.Equal(GcEventType.MarkEnd, eventData.EventType);
        Assert.Equal(condemnedGeneration, eventData.CondemnedGeneration);
    }

    [Fact]
    public void TryParseGCNotification_UnknownGcEventType_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        ulong unknownType = 99ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Gc, unknownType, 0ul);

        Assert.False(contract.TryParseGCNotification(exInfo, out _));
    }

    [Fact]
    public void TryParseGCNotification_WrongNotificationType_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleLoad, 0x1000ul, 0ul);

        Assert.False(contract.TryParseGCNotification(exInfo, out _));
    }

    [Fact]
    public void TryParseExceptionCatcherEnterNotification_ValidArgs_ReturnsAddressAndOffset()
    {
        INotifications contract = CreateContract();
        ulong expectedMethodDesc = 0x0000_AAAA_BBBB_CCCCul;
        uint expectedOffset = 0x42;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ExceptionCatcherEnter, expectedMethodDesc, expectedOffset);

        bool result = contract.TryParseExceptionCatcherEnterNotification(exInfo, out TargetPointer methodDescAddress, out uint nativeOffset);

        Assert.True(result);
        Assert.Equal(expectedMethodDesc, methodDescAddress.Value);
        Assert.Equal(expectedOffset, nativeOffset);
    }

    [Fact]
    public void TryParseExceptionCatcherEnterNotification_WrongType_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleLoad, 0x1000ul, 0x42ul);

        Assert.False(contract.TryParseExceptionCatcherEnterNotification(exInfo, out _, out _));
    }

    [Fact]
    public void TryParseExceptionCatcherEnterNotification_TooFewArgs_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ExceptionCatcherEnter, 0x1000ul); // missing offset

        Assert.False(contract.TryParseExceptionCatcherEnterNotification(exInfo, out _, out _));
    }
}
