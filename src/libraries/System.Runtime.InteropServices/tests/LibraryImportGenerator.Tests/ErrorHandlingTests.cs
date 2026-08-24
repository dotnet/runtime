// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Xunit;

namespace System.Runtime.InteropServices
{
    internal enum ErrorLocation
    {
        ReturnValue = 0,
        LastParameter = 1,
        SystemError = 2,
        HiddenReturnValue = 3,
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ErrorHandlerAttribute : Attribute
    {
        public ErrorHandlerAttribute(Type marshallerType, ErrorLocation errorLocation)
        {
        }
    }
}

namespace LibraryImportGenerator.IntegrationTests
{
    internal readonly record struct CustomError(int Value);
    internal readonly record struct CleanupInput(int Value);
    internal readonly record struct SystemError(int Value);
    internal readonly record struct TrackedOutput(int Value);

    internal sealed class CustomErrorException(int error) : Exception
    {
        public int Error { get; } = error;
    }

    [CustomMarshaller(typeof(CustomError), MarshalMode.Default, typeof(CustomErrorMarshaller))]
    internal static class CustomErrorMarshaller
    {
        public static int ConvertToUnmanaged(CustomError error) => error.Value;

        public static CustomError ConvertToManaged(int error)
        {
            if (error < 0)
            {
                throw new CustomErrorException(error);
            }

            return new CustomError(error);
        }
    }

    [CustomMarshaller(typeof(SystemError), MarshalMode.Default, typeof(SystemErrorMarshaller))]
    internal static class SystemErrorMarshaller
    {
        public static int ConvertToUnmanaged(SystemError error) => error.Value;

        public static SystemError ConvertToManaged(int error)
        {
            if (error < 0)
            {
                throw new CustomErrorException(error);
            }

            return new SystemError(error);
        }
    }

    [CustomMarshaller(typeof(CleanupInput), MarshalMode.ManagedToUnmanagedIn, typeof(CleanupInputMarshaller))]
    internal static class CleanupInputMarshaller
    {
        public static bool FreeCalled { get; set; }

        public static nint ConvertToUnmanaged(CleanupInput input)
        {
            nint value = Marshal.AllocCoTaskMem(sizeof(int));
            Marshal.WriteInt32(value, input.Value);
            return value;
        }

        public static void Free(nint value)
        {
            FreeCalled = true;
            Marshal.FreeCoTaskMem(value);
        }
    }

    [CustomMarshaller(typeof(TrackedOutput), MarshalMode.Default, typeof(TrackedOutputMarshaller))]
    internal static class TrackedOutputMarshaller
    {
        public static bool ConvertToManagedCalled { get; set; }

        public static int ConvertToUnmanaged(TrackedOutput value) => value.Value;

        public static TrackedOutput ConvertToManaged(int value)
        {
            ConvertToManagedCalled = true;
            return new TrackedOutput(value);
        }
    }

    partial class NativeExportsNE
    {
        internal static partial class ErrorHandling
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_error")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.ReturnValue)]
            public static partial CustomError ReturnError(int error);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_error")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.ReturnValue)]
            public static partial void HandleReturnError(int error);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_error_out")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.LastParameter)]
            public static partial void ErrorInOutParameter(int error, out CustomError errorValue);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_error_ref")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.LastParameter)]
            public static partial void ErrorInRefParameter(int error, ref CustomError errorValue);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_constant_error_out")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.LastParameter)]
            public static partial void InjectedErrorParameter();

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_error")]
            [ErrorHandler(typeof(SystemErrorMarshaller), ErrorLocation.SystemError)]
            public static partial void HandleSystemError(int error, byte shouldSetError);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_error")]
            [ErrorHandler(typeof(SystemErrorMarshaller), ErrorLocation.SystemError)]
            public static partial void ReturnSystemError(int error, byte shouldSetError, out SystemError errorValue);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_error_with_output")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.ReturnValue)]
            public static partial void ReturnErrorBeforeOutput(
                int error,
                [MarshalUsing(typeof(TrackedOutputMarshaller))] out TrackedOutput output);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_error_with_input")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.ReturnValue)]
            public static partial void ReturnErrorWithAllocatedInput(
                int error,
                [MarshalUsing(typeof(CleanupInputMarshaller))] CleanupInput input);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_output_and_error_out")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.LastParameter)]
            public static partial void LastParameterErrorBeforeOutput(
                int error,
                [MarshalUsing(typeof(TrackedOutputMarshaller))] out TrackedOutput output);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_error_with_output")]
            [ErrorHandler(typeof(SystemErrorMarshaller), ErrorLocation.SystemError)]
            public static partial void SystemErrorBeforeOutput(
                int error,
                [MarshalUsing(typeof(TrackedOutputMarshaller))] out TrackedOutput output);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_error")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.HiddenReturnValue)]
            public static partial void HandleHiddenReturnError(int error);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_error_with_hidden_return")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.HiddenReturnValue)]
            [return: MarshalUsing(typeof(TrackedOutputMarshaller))]
            public static partial TrackedOutput HiddenReturnValue(int error);
        }
    }

    public class ErrorHandlingTests
    {
        [Fact]
        public void ReturnValueCanBeObserved()
        {
            Assert.Equal(new CustomError(42), NativeExportsNE.ErrorHandling.ReturnError(42));
        }

        [Fact]
        public void ReturnValueCanBeHandledWithoutManagedReturn()
        {
            NativeExportsNE.ErrorHandling.HandleReturnError(0);
            CustomErrorException exception = Assert.Throws<CustomErrorException>(
                () => NativeExportsNE.ErrorHandling.HandleReturnError(-1));
            Assert.Equal(-1, exception.Error);
        }

        [Fact]
        public void LastOutParameterCanBeObserved()
        {
            NativeExportsNE.ErrorHandling.ErrorInOutParameter(42, out CustomError error);
            Assert.Equal(new CustomError(42), error);
        }

        [Fact]
        public void LastRefParameterCanBeObserved()
        {
            CustomError error = new(1);
            NativeExportsNE.ErrorHandling.ErrorInRefParameter(42, ref error);
            Assert.Equal(new CustomError(43), error);
        }

        [Fact]
        public void LastParameterIsInjectedWhenMissing()
        {
            NativeExportsNE.ErrorHandling.InjectedErrorParameter();
        }

        [Fact]
        public void SystemErrorCanBeHandledWithoutManagedOutput()
        {
            NativeExportsNE.ErrorHandling.HandleSystemError(0, shouldSetError: 1);
            CustomErrorException exception = Assert.Throws<CustomErrorException>(
                () => NativeExportsNE.ErrorHandling.HandleSystemError(-2, shouldSetError: 1));
            Assert.Equal(-2, exception.Error);
        }

        [Fact]
        public void SystemErrorCanBeObservedThroughLastOutParameter()
        {
            NativeExportsNE.ErrorHandling.ReturnSystemError(42, shouldSetError: 1, out SystemError error);
            Assert.Equal(new SystemError(42), error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ErrorHandlingPrecedesOtherOutMarshalling(int location)
        {
            TrackedOutputMarshaller.ConvertToManagedCalled = false;

            CustomErrorException exception = location switch
            {
                0 => Assert.Throws<CustomErrorException>(
                    () => NativeExportsNE.ErrorHandling.ReturnErrorBeforeOutput(-3, out _)),
                1 => Assert.Throws<CustomErrorException>(
                    () => NativeExportsNE.ErrorHandling.LastParameterErrorBeforeOutput(-3, out _)),
                2 => Assert.Throws<CustomErrorException>(
                    () => NativeExportsNE.ErrorHandling.SystemErrorBeforeOutput(-3, out _)),
                _ => throw new ArgumentOutOfRangeException(nameof(location)),
            };

            Assert.Equal(-3, exception.Error);
            Assert.False(TrackedOutputMarshaller.ConvertToManagedCalled);
        }

        [Fact]
        public void ErrorHandlingExceptionCleansUpCallerAllocatedMemory()
        {
            CleanupInputMarshaller.FreeCalled = false;

            CustomErrorException exception = Assert.Throws<CustomErrorException>(
                () => NativeExportsNE.ErrorHandling.ReturnErrorWithAllocatedInput(-4, new CleanupInput(42)));

            Assert.Equal(-4, exception.Error);
            Assert.True(CleanupInputMarshaller.FreeCalled);
        }

        [Fact]
        public void HiddenReturnValueWithVoidManagedReturnHandlesNativeReturnError()
        {
            NativeExportsNE.ErrorHandling.HandleHiddenReturnError(0);
            CustomErrorException exception = Assert.Throws<CustomErrorException>(
                () => NativeExportsNE.ErrorHandling.HandleHiddenReturnError(-5));
            Assert.Equal(-5, exception.Error);
        }

        [Fact]
        public void HiddenReturnValueMovesManagedReturnToNativeOutParameter()
        {
            TrackedOutputMarshaller.ConvertToManagedCalled = false;
            Assert.Equal(new TrackedOutput(42), NativeExportsNE.ErrorHandling.HiddenReturnValue(0));
            Assert.True(TrackedOutputMarshaller.ConvertToManagedCalled);

            TrackedOutputMarshaller.ConvertToManagedCalled = false;
            CustomErrorException exception = Assert.Throws<CustomErrorException>(
                () => NativeExportsNE.ErrorHandling.HiddenReturnValue(-5));
            Assert.Equal(-5, exception.Error);
            Assert.False(TrackedOutputMarshaller.ConvertToManagedCalled);
        }
    }
}
