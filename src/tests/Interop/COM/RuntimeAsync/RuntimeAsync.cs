// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

[ConditionalClass(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
public class RuntimeAsyncBuiltInCom
{
    public const int ExpectedIntValue = 42;
    public const float ExpectedClassFloatValue = 3.14f;
    public const float ExpectedInterfaceFloatValue = 2.71f;

    [Fact]
    public static void RuntimeAsyncThunksDoNotModifyCcwVtable()
    {
        ExposedToCom obj = new();
        Assert.True(RuntimeAsyncNative.ValidateSlotLayoutForDefaultInterface(obj, ExpectedIntValue, ExpectedClassFloatValue));
        Assert.True(RuntimeAsyncNative.ValidateSlotLayoutForInterface(obj, ExpectedInterfaceFloatValue));
    }

    [Fact]
    public static void RuntimeAsyncDoNotModifyRcwVtable()
    {
        using (ComActivationHelpers.RegisterTypeForActivation<TaskComServer>())
        {
            var myObjectType = Type.GetTypeFromCLSID(typeof(TaskComServer).GUID, throwOnError: true)!;
            object obj = Activator.CreateInstance(myObjectType)!;
            ITaskComServer_Imported comObject = (ITaskComServer_Imported)obj;
            TestAsyncMethod(comObject).GetAwaiter().GetResult();

            Assert.Equal(TaskComServer.ExpectedValue, comObject.GetValue());

            static async Task TestAsyncMethod(ITaskComServer_Imported obj)
            {
                await obj.GetTask();
            }
        }
    }

    [Fact]
    public static void IDispatchCallInvokesCorrectMethod()
    {
        using (ComActivationHelpers.RegisterTypeForActivation<TaskComServer>())
        {
            var myObjectType = Type.GetTypeFromCLSID(typeof(TaskComServer).GUID, throwOnError: true)!;
            object obj = Activator.CreateInstance(myObjectType)!;
            ITaskComServer_AsDispatchOnly comObject = (ITaskComServer_AsDispatchOnly)obj;
            TestAsyncMethod(comObject).GetAwaiter().GetResult();

            Assert.Equal(TaskComServer.ExpectedValue, comObject.GetValue());

            static async Task TestAsyncMethod(ITaskComServer_AsDispatchOnly obj)
            {
                await obj.GetTask();
            }
        }
    }
}

public static class RuntimeAsyncNative
{
    [DllImport("RuntimeAsyncNative")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool ValidateSlotLayoutForDefaultInterface([MarshalAs(UnmanagedType.Interface)] object comObject, int expectedIntValue, float expectedFloatValue);

    [DllImport("RuntimeAsyncNative")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool ValidateSlotLayoutForInterface([MarshalAs(UnmanagedType.Interface)] object comObject, float expectedFloatValue);
}

[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IExposedToComInterface
{
    Task AsyncMethodOnInterface();

    float FloatMethodOnInterface();
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
public class ExposedToCom : IExposedToComInterface
{
    public int MyMethod()
    {
        return RuntimeAsyncBuiltInCom.ExpectedIntValue;
    }

    public async Task<int> MyAsyncMethod()
    {
        return await Task.FromResult(1);
    }

    public async Task MyAsyncMethod2()
    {
        await Task.Run(() => { });
    }

    public float MyFloatMethod()
    {
        return RuntimeAsyncBuiltInCom.ExpectedClassFloatValue;
    }

    async Task IExposedToComInterface.AsyncMethodOnInterface()
    {
        await Task.Run(() => { });
    }

    float IExposedToComInterface.FloatMethodOnInterface()
    {
        return RuntimeAsyncBuiltInCom.ExpectedInterfaceFloatValue;
    }
}
