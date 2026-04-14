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
        var target = new TestPlaceholderTarget.Builder(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true })
            .UseReader((_, _) => -1)
            .AddContract<INotifications>(version: 1)
            .Build();
        return target.Contracts.Notifications;
    }

    private static ReadOnlySpan<TargetPointer> MakeExInfo(params ulong[] values)
    {
        TargetPointer[] arr = new TargetPointer[values.Length];
        for (int i = 0; i < values.Length; i++)
            arr[i] = new TargetPointer(values[i]);
        return arr;
    }

    [Theory]
    [InlineData(0ul)]
    [InlineData(3ul)] // JIT_NOTIFICATION (legacy, not handled)
    [InlineData(99ul)]
    public void TryParseNotification_UnknownType_ReturnsFalse(ulong rawType)
    {
        INotifications contract = CreateContract();
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(rawType);
        Assert.False(contract.TryParseNotification(exInfo, out NotificationData? notification));
        Assert.Null(notification);
    }

    [Fact]
    public void TryParseNotification_EmptySpan_ReturnsFalse()
    {
        INotifications contract = CreateContract();
        Assert.False(contract.TryParseNotification(ReadOnlySpan<TargetPointer>.Empty, out NotificationData? notification));
        Assert.Null(notification);
    }

    [Fact]
    public void TryParseNotification_ModuleLoad_ReturnsModuleLoadData()
    {
        INotifications contract = CreateContract();
        ulong expectedModule = 0x1234_5678_9ABC_DEF0ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleLoad, expectedModule);

        Assert.True(contract.TryParseNotification(exInfo, out NotificationData? notification));

        ModuleLoadNotificationData moduleLoad = Assert.IsType<ModuleLoadNotificationData>(notification);
        Assert.Equal(NotificationType.ModuleLoad, moduleLoad.Type);
        Assert.Equal(expectedModule, moduleLoad.ModuleAddress.Value);
    }

    [Fact]
    public void TryParseNotification_ModuleUnload_ReturnsModuleUnloadData()
    {
        INotifications contract = CreateContract();
        ulong expectedModule = 0xDEAD_BEEF_0000_0001ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ModuleUnload, expectedModule);

        Assert.True(contract.TryParseNotification(exInfo, out NotificationData? notification));

        ModuleUnloadNotificationData moduleUnload = Assert.IsType<ModuleUnloadNotificationData>(notification);
        Assert.Equal(NotificationType.ModuleUnload, moduleUnload.Type);
        Assert.Equal(expectedModule, moduleUnload.ModuleAddress.Value);
    }

    [Fact]
    public void TryParseNotification_Jit_ReturnsJitData()
    {
        INotifications contract = CreateContract();
        ulong expectedMethodDesc = 0x0000_1111_2222_3333ul;
        ulong expectedNativeCode = 0x0000_4444_5555_6666ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Jit2, expectedMethodDesc, expectedNativeCode);

        Assert.True(contract.TryParseNotification(exInfo, out NotificationData? notification));

        JitNotificationData jit = Assert.IsType<JitNotificationData>(notification);
        Assert.Equal(NotificationType.Jit2, jit.Type);
        Assert.Equal(expectedMethodDesc, jit.MethodDescAddress.Value);
        Assert.Equal(expectedNativeCode, jit.NativeCodeAddress.Value);
    }

    [Fact]
    public void TryParseNotification_Exception_ReturnsExceptionData()
    {
        INotifications contract = CreateContract();
        ulong expectedThread = 0x0000_CAFE_BABE_0000ul;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Exception, expectedThread);

        Assert.True(contract.TryParseNotification(exInfo, out NotificationData? notification));

        ExceptionNotificationData exception = Assert.IsType<ExceptionNotificationData>(notification);
        Assert.Equal(NotificationType.Exception, exception.Type);
        Assert.Equal(expectedThread, exception.ThreadAddress.Value);
    }

    [Fact]
    public void TryParseNotification_Gc_SupportedEvent_ReturnsGcData()
    {
        INotifications contract = CreateContract();
        ulong gcMarkEndType = (ulong)GcEventType.MarkEnd;
        int condemnedGeneration = 2;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Gc, gcMarkEndType, (ulong)condemnedGeneration);

        Assert.True(contract.TryParseNotification(exInfo, out NotificationData? notification));

        GcNotificationData gc = Assert.IsType<GcNotificationData>(notification);
        Assert.Equal(NotificationType.Gc, gc.Type);
        Assert.True(gc.IsSupportedEvent);
        Assert.Equal(GcEventType.MarkEnd, gc.EventData.EventType);
        Assert.Equal(condemnedGeneration, gc.EventData.CondemnedGeneration);
    }

    [Fact]
    public void TryParseNotification_Gc_UnsupportedEvent_ReturnsGcDataWithFalseSupported()
    {
        INotifications contract = CreateContract();
        ulong unsupportedGcEventType = (ulong)GcEventType.MarkEnd + 1;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(Gc, unsupportedGcEventType, 0);

        Assert.True(contract.TryParseNotification(exInfo, out NotificationData? notification));

        GcNotificationData gc = Assert.IsType<GcNotificationData>(notification);
        Assert.False(gc.IsSupportedEvent);
    }

    [Fact]
    public void TryParseNotification_ExceptionCatcherEnter_ReturnsData()
    {
        INotifications contract = CreateContract();
        ulong expectedMethodDesc = 0x0000_AAAA_BBBB_CCCCul;
        uint expectedOffset = 0x42;
        ReadOnlySpan<TargetPointer> exInfo = MakeExInfo(ExceptionCatcherEnter, expectedMethodDesc, expectedOffset);

        Assert.True(contract.TryParseNotification(exInfo, out NotificationData? notification));

        ExceptionCatcherEnterNotificationData catcherEnter = Assert.IsType<ExceptionCatcherEnterNotificationData>(notification);
        Assert.Equal(NotificationType.ExceptionCatcherEnter, catcherEnter.Type);
        Assert.Equal(expectedMethodDesc, catcherEnter.MethodDescAddress.Value);
        Assert.Equal(expectedOffset, catcherEnter.NativeOffset);
    }
}

