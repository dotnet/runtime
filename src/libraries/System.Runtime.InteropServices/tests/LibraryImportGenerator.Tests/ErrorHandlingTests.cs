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
        HiddenReturnValue = 2,
        HiddenLastParameter = 3,
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

    [CustomMarshaller(typeof(CustomError), MarshalMode.Default, typeof(ObservedCustomErrorMarshaller))]
    internal static class ObservedCustomErrorMarshaller
    {
        public static bool ConvertToManagedCalled { get; set; }

        public static int ConvertToUnmanaged(CustomError error) => error.Value;

        public static CustomError ConvertToManaged(int error)
        {
            ConvertToManagedCalled = true;
            return new CustomError(error + 1000);
        }
    }

    [CustomMarshaller(typeof(CustomError), MarshalMode.ManagedToUnmanagedOut, typeof(StatefulErrorMarshaller.Marshaller))]
    internal static class StatefulErrorMarshaller
    {
        public struct Marshaller
        {
            private int _error;

            public static bool FromUnmanagedCalled { get; set; }
            public static bool ToManagedCalled { get; set; }
            public static bool FreeCalled { get; set; }

            public void FromUnmanaged(int error)
            {
                FromUnmanagedCalled = true;
                _error = error;
            }

            public CustomError ToManaged()
            {
                ToManagedCalled = true;
                if (_error < 0)
                {
                    throw new CustomErrorException(_error);
                }

                return new CustomError(_error);
            }

            public void Free()
            {
                FreeCalled = true;
            }
        }
    }

    [CustomMarshaller(typeof(CustomError), MarshalMode.ManagedToUnmanagedOut, typeof(StatefulResultMarshaller.Marshaller))]
    internal static class StatefulResultMarshaller
    {
        public struct Marshaller
        {
            private int _error;

            public static bool FromUnmanagedCalled { get; set; }
            public static bool ToManagedCalled { get; set; }
            public static bool FreeCalled { get; set; }

            public void FromUnmanaged(int error)
            {
                FromUnmanagedCalled = true;
                _error = error;
            }

            public CustomError ToManaged()
            {
                ToManagedCalled = true;
                return new CustomError(_error);
            }

            public void Free()
            {
                FreeCalled = true;
            }
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
            [return: MarshalUsing(typeof(ObservedCustomErrorMarshaller))]
            public static partial CustomError ReturnError(int error);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_error")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.ReturnValue)]
            public static partial void HandleReturnError(int error);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_error_out")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.LastParameter)]
            public static partial void ErrorInOutParameter(
                int error,
                [MarshalUsing(typeof(ObservedCustomErrorMarshaller))] out CustomError errorValue);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_error_ref")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.LastParameter)]
            public static partial void ErrorInRefParameter(
                int error,
                [MarshalUsing(typeof(ObservedCustomErrorMarshaller))] ref CustomError errorValue);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_constant_error_out")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.HiddenLastParameter)]
            public static partial void InjectedErrorParameter();

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_error_with_output")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.ReturnValue)]
            public static partial void ReturnErrorBeforeOutput(
                int error,
                [MarshalUsing(typeof(TrackedOutputMarshaller))] out TrackedOutput output);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_error_with_output")]
            [ErrorHandler(typeof(StatefulErrorMarshaller), ErrorLocation.ReturnValue)]
            [return: MarshalUsing(typeof(StatefulResultMarshaller))]
            public static partial CustomError StatefulReturnErrorBeforeOutput(
                int error,
                [MarshalUsing(typeof(TrackedOutputMarshaller))] out TrackedOutput output);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_error_with_input")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.ReturnValue)]
            public static partial void ReturnErrorWithAllocatedInput(
                int error,
                [MarshalUsing(typeof(CleanupInputMarshaller))] CleanupInput input);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_output_and_error_out")]
            [ErrorHandler(typeof(CustomErrorMarshaller), ErrorLocation.HiddenLastParameter)]
            public static partial void HiddenLastParameterErrorBeforeOutput(
                int error,
                [MarshalUsing(typeof(TrackedOutputMarshaller))] out TrackedOutput output);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_error_out")]
            [ErrorHandler(typeof(StatefulErrorMarshaller), ErrorLocation.LastParameter)]
            public static partial void StatefulLastParameterError(
                int error,
                [MarshalUsing(typeof(StatefulResultMarshaller))] out CustomError errorValue);

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
            ObservedCustomErrorMarshaller.ConvertToManagedCalled = false;

            Assert.Equal(new CustomError(1042), NativeExportsNE.ErrorHandling.ReturnError(42));
            Assert.True(ObservedCustomErrorMarshaller.ConvertToManagedCalled);
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
            ObservedCustomErrorMarshaller.ConvertToManagedCalled = false;

            NativeExportsNE.ErrorHandling.ErrorInOutParameter(42, out CustomError error);
            Assert.Equal(new CustomError(1042), error);
            Assert.True(ObservedCustomErrorMarshaller.ConvertToManagedCalled);
        }

        [Fact]
        public void LastRefParameterCanBeObserved()
        {
            ObservedCustomErrorMarshaller.ConvertToManagedCalled = false;

            CustomError error = new(1);
            NativeExportsNE.ErrorHandling.ErrorInRefParameter(42, ref error);
            Assert.Equal(new CustomError(1043), error);
            Assert.True(ObservedCustomErrorMarshaller.ConvertToManagedCalled);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void ErrorHandlingPrecedesOverlappingValueMarshalling(int location)
        {
            ObservedCustomErrorMarshaller.ConvertToManagedCalled = false;

            CustomErrorException exception = location switch
            {
                0 => Assert.Throws<CustomErrorException>(
                    () => NativeExportsNE.ErrorHandling.ReturnError(-2)),
                1 => Assert.Throws<CustomErrorException>(
                    () => NativeExportsNE.ErrorHandling.ErrorInOutParameter(-2, out _)),
                _ => throw new ArgumentOutOfRangeException(nameof(location)),
            };

            Assert.Equal(-2, exception.Error);
            Assert.False(ObservedCustomErrorMarshaller.ConvertToManagedCalled);
        }

        [Fact]
        public void HiddenLastParameterIsInjected()
        {
            NativeExportsNE.ErrorHandling.InjectedErrorParameter();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void ErrorHandlingPrecedesOtherOutMarshalling(int location)
        {
            TrackedOutputMarshaller.ConvertToManagedCalled = false;

            CustomErrorException exception = location switch
            {
                0 => Assert.Throws<CustomErrorException>(
                    () => NativeExportsNE.ErrorHandling.ReturnErrorBeforeOutput(-3, out _)),
                1 => Assert.Throws<CustomErrorException>(
                    () => NativeExportsNE.ErrorHandling.HiddenLastParameterErrorBeforeOutput(-3, out _)),
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
        public void ErrorUnmarshalStagesRunBeforeOtherOutMarshalling()
        {
            StatefulErrorMarshaller.Marshaller.FromUnmanagedCalled = false;
            StatefulErrorMarshaller.Marshaller.ToManagedCalled = false;
            StatefulErrorMarshaller.Marshaller.FreeCalled = false;
            StatefulResultMarshaller.Marshaller.FromUnmanagedCalled = false;
            StatefulResultMarshaller.Marshaller.ToManagedCalled = false;
            StatefulResultMarshaller.Marshaller.FreeCalled = false;
            TrackedOutputMarshaller.ConvertToManagedCalled = false;

            CustomErrorException exception = Assert.Throws<CustomErrorException>(
                () => NativeExportsNE.ErrorHandling.StatefulReturnErrorBeforeOutput(-5, out _));

            Assert.Equal(-5, exception.Error);
            Assert.True(StatefulErrorMarshaller.Marshaller.FromUnmanagedCalled);
            Assert.True(StatefulErrorMarshaller.Marshaller.ToManagedCalled);
            Assert.True(StatefulErrorMarshaller.Marshaller.FreeCalled);
            Assert.False(StatefulResultMarshaller.Marshaller.FromUnmanagedCalled);
            Assert.False(StatefulResultMarshaller.Marshaller.ToManagedCalled);
            Assert.False(StatefulResultMarshaller.Marshaller.FreeCalled);
            Assert.False(TrackedOutputMarshaller.ConvertToManagedCalled);
        }

        [Fact]
        public void StatefulLastParameterErrorCleanupRunsWhenConversionThrows()
        {
            StatefulErrorMarshaller.Marshaller.FromUnmanagedCalled = false;
            StatefulErrorMarshaller.Marshaller.ToManagedCalled = false;
            StatefulErrorMarshaller.Marshaller.FreeCalled = false;
            StatefulResultMarshaller.Marshaller.FromUnmanagedCalled = false;
            StatefulResultMarshaller.Marshaller.ToManagedCalled = false;
            StatefulResultMarshaller.Marshaller.FreeCalled = false;

            CustomErrorException exception = Assert.Throws<CustomErrorException>(
                () => NativeExportsNE.ErrorHandling.StatefulLastParameterError(-6, out _));

            Assert.Equal(-6, exception.Error);
            Assert.True(StatefulErrorMarshaller.Marshaller.FromUnmanagedCalled);
            Assert.True(StatefulErrorMarshaller.Marshaller.ToManagedCalled);
            Assert.True(StatefulErrorMarshaller.Marshaller.FreeCalled);
            Assert.False(StatefulResultMarshaller.Marshaller.FromUnmanagedCalled);
            Assert.False(StatefulResultMarshaller.Marshaller.ToManagedCalled);
            Assert.False(StatefulResultMarshaller.Marshaller.FreeCalled);
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
