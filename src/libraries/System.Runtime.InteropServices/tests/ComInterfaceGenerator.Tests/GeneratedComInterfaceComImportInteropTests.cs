// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    [ConditionalClass(typeof(GeneratedComInterfaceComImportInteropTests), nameof(IsSupported))]
    public unsafe partial class GeneratedComInterfaceComImportInteropTests
    {
        public static bool IsSupported =>
            RemoteExecutor.IsSupported
            && PlatformDetection.IsWindows
            && PlatformDetection.IsNotMonoRuntime
            && PlatformDetection.IsNotNativeAot;

        [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "new_get_and_set_int")]
        private static partial IGetAndSetInt NewNativeObject();

        [ComImport]
        [Guid(_guid)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IGetAndSetIntComImport
        {
            int GetInt();

            public void SetInt(int x);

            public const string _guid = "2c3f9903-b586-46b1-881b-adfce9af47b1";
        }

        [Fact]
        public void CallComImportInterfaceMethodsOnGeneratedComObject()
        {
            using var _ = RemoteExecutor.Invoke(() =>
            {
                IGetAndSetInt obj = NewNativeObject();
#pragma warning disable SYSLIB1099 // Casting between a 'ComImport' type and a source-generated COM type is not supported
                IGetAndSetIntComImport runtimeObj = (IGetAndSetIntComImport)obj;
#pragma warning restore SYSLIB1099 // Casting between a 'ComImport' type and a source-generated COM type is not supported
                obj.SetInt(1234);
                Assert.Equal(1234, runtimeObj.GetInt());
                runtimeObj.SetInt(4321);
                Assert.Equal(4321, obj.GetInt());

            }, new RemoteInvokeOptions
            {
                RuntimeConfigurationOptions =
                {
                    { "System.Runtime.InteropServices.Marshalling.EnableGeneratedComInterfaceComImportInterop", true }
                }
            });
        }
    }
}
