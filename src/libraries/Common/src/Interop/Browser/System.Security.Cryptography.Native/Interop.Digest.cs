// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Browser;
using System.Threading;
using System.Threading.Tasks;
internal static partial class Interop
{

    internal static partial class SubtleCrypto
    {
        [DllImport(Interop.Libraries.SubtleCryptoNative, EntryPoint = "SubtleCryptoNative_DigestFree")]
        internal static extern void DigestFree(IntPtr handle);

        [DllImport(Interop.Libraries.SubtleCryptoNative, EntryPoint = "SubtleCryptoNative_DigestCreate")]
        internal static extern SafeDigestCtxHandle DigestCreate(PAL_HashAlgorithm algorithm, out int cbDigest);

        internal static int DigestUpdate(SafeDigestCtxHandle ctx, ReadOnlySpan<byte> data) =>
            DigestUpdate(ctx, ref MemoryMarshal.GetReference(data), data.Length);
        internal static int DigestUpdate(SafeDigestCtxHandle ctx, byte[] data, int ibStart, int cbSize) =>
            DigestUpdate(ctx, ref data[ibStart], cbSize);

        [DllImport(Interop.Libraries.SubtleCryptoNative, EntryPoint = "SubtleCryptoNative_DigestUpdate")]
        private static extern int DigestUpdate(SafeDigestCtxHandle ctx, ref byte pbData, int cbData);

        internal static int DigestFinal(SafeDigestCtxHandle ctx, Span<byte> output) =>
            DigestFinal(ctx, ref MemoryMarshal.GetReference(output), output.Length, -1);

        internal static int DigestFinal(SafeDigestCtxHandle ctx, Span<byte> output, TaskCompletionSource<int>? comptcs)
        {
            return DigestFinal(ctx, ref MemoryMarshal.GetReference(output), output.Length, WrapTaskCompletionSource(comptcs));
        }

        internal static int DigestFinal(SafeDigestCtxHandle ctx, byte[] output, TaskCompletionSource<int>? comptcs)
        {
            return DigestFinal(ctx, ref MemoryMarshal.GetArrayDataReference(output), output.Length, WrapTaskCompletionSource(comptcs));
        }

        [DllImport(Interop.Libraries.SubtleCryptoNative, EntryPoint = "SubtleCryptoNative_DigestFinal")]
        private static extern int DigestFinal(SafeDigestCtxHandle ctx, ref byte pbOutput, int cbOutput, int gc_handle);

        internal static int DigestCurrent(SafeDigestCtxHandle ctx, Span<byte> output) =>
            DigestCurrent(ctx, ref MemoryMarshal.GetReference(output), output.Length);

        [DllImport(Interop.Libraries.SubtleCryptoNative, EntryPoint = "SubtleCryptoNative_DigestCurrent")]
        private static extern int DigestCurrent(SafeDigestCtxHandle ctx, ref byte pbOutput, int cbOutput);

        internal static unsafe int DigestOneShot(PAL_HashAlgorithm algorithm, byte* pbData, int cbData, byte* pbOutput, int cbOutput, out int cbDigest, TaskCompletionSource<int>? comptcs)
        {
            return DigestOneShot(algorithm, pbData, cbData, pbOutput, cbOutput, out cbDigest, WrapTaskCompletionSource(comptcs));
        }

        [DllImport(Interop.Libraries.SubtleCryptoNative, EntryPoint = "SubtleCryptoNative_DigestOneShot")]
        internal static unsafe extern int DigestOneShot(PAL_HashAlgorithm algorithm, byte* pbData, int cbData, byte* pbOutput, int cbOutput, out int cbDigest, int gc_handle);

        public static void HashFinalCallback (int gcHandle, object? result = null)
        {
            //System.Diagnostics.Debug.WriteLine($"We are called back with a handle of {gcHandle}");
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            if (h.Target is TaskCompletionSource<int> tcs)
            {
                tcs?.SetResult((int)result);
                h.Free();
                //System.Diagnostics.Debug.WriteLine($"We are called back with a handle of {gcHandle} and object {tcs} setting result {result}");
            }
        }

        private static int WrapTaskCompletionSource(TaskCompletionSource<int>? comptcs)
        {
            var handle = false;
            if (handle)
                HashFinalCallback(-1);

            return (int)(IntPtr)GCHandle.Alloc (comptcs);
        }
    }
}

namespace System.Security.Cryptography.Browser
{
    internal sealed class SafeDigestCtxHandle : SafeHandle
    {
        internal SafeDigestCtxHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.SubtleCrypto.DigestFree(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
