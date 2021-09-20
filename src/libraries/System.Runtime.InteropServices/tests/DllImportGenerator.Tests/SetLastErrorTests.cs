using System;
using System.Runtime.InteropServices;

using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    [BlittableType]
    public struct SetLastErrorMarshaller
    {
        public int val;

        public SetLastErrorMarshaller(int i)
        {
            val = i;
        }

        public int ToManaged()
        {
            // Explicity set the last error to something else on unmarshalling
            Marshal.SetLastPInvokeError(val * 2);
            return val;
        }
    }

    partial class NativeExportsNE
    {
        public partial class SetLastError
        {
            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "set_error", SetLastError = true)]
            public static partial int SetError(int error, byte shouldSetError);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "set_error_return_string", SetLastError = true)]
            [return: MarshalUsing(typeof(SetLastErrorMarshaller))]
            public static partial int SetError_CustomMarshallingSetsError(int error, byte shouldSetError);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "set_error_return_string", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.LPWStr)]
            public static partial string SetError_NonBlittableSignature(int error, [MarshalAs(UnmanagedType.U1)] bool shouldSetError, [MarshalAs(UnmanagedType.LPWStr)] string errorString);
        }
    }

    public class SetLastErrorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(-5)]
        public void LastWin32Error_HasExpectedValue(int error)
        {
            string errorString = error.ToString();
            string ret = NativeExportsNE.SetLastError.SetError_NonBlittableSignature(error, shouldSetError: true, errorString);
            Assert.Equal(error, Marshal.GetLastWin32Error());
            Assert.Equal(errorString, ret);

            // Clear the last error
            Marshal.SetLastPInvokeError(0);

            NativeExportsNE.SetLastError.SetError(error, shouldSetError: 1);
            Assert.Equal(error, Marshal.GetLastWin32Error());

            Marshal.SetLastPInvokeError(0);

            // Custom marshalling sets the last error on unmarshalling.
            // Last error should reflect error from native call, not unmarshalling.
            NativeExportsNE.SetLastError.SetError_CustomMarshallingSetsError(error, shouldSetError: 1);
            Assert.Equal(error, Marshal.GetLastWin32Error());
        }

        [Fact]
        public void ClearPreviousError()
        {
            int error = 100;
            Marshal.SetLastPInvokeError(error);

            // Don't actually set the error in the native call. SetLastError=true should clear any existing error.
            string errorString = error.ToString();
            string ret = NativeExportsNE.SetLastError.SetError_NonBlittableSignature(error, shouldSetError: false, errorString);
            Assert.Equal(0, Marshal.GetLastWin32Error());
            Assert.Equal(errorString, ret);

            Marshal.SetLastPInvokeError(error);

            // Don't actually set the error in the native call. SetLastError=true should clear any existing error.
            NativeExportsNE.SetLastError.SetError(error, shouldSetError: 0);
            Assert.Equal(0, Marshal.GetLastWin32Error());

            // Don't actually set the error in the native call. Custom marshalling still sets the last error.
            // SetLastError=true should clear any existing error and ignore error set by custom marshalling.
            NativeExportsNE.SetLastError.SetError_CustomMarshallingSetsError(error, shouldSetError: 0);
            Assert.Equal(0, Marshal.GetLastWin32Error());
        }
    }
}
