// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IComInterface<T> where T : unmanaged
    {
    }

    public struct Point1<T> where T : struct
    {
        public T e00;
    }

    public struct Point2<T> where T : struct
    {
        public T e00;
        public T e01;
    }

    public struct Point3<T> where T : struct
    {
        public T e00;
        public T e01;
        public T e02;
    }

    public struct Point4<T> where T : struct
    {
        public T e00;
        public T e01;
        public T e02;
        public T e03;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SequentialClass<T> where T : struct
    {
        public T e00;
    }
}

[SkipOnMono("needs triage")]
[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]

public partial class GenericsTest
{
}
