// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Provides helpers to decode strings from unmanaged memory to System.String while avoiding
    /// intermediate allocation.
    /// 
    /// This has three components:
    /// 
    ///   (1) Light-up Encoding.GetString(byte*, int) via reflection and resurface it as extension 
    ///       method. 
    ///
    ///       This is a new API that will provide API convergence across all platforms for 
    ///       this scenario. It is already on .NET 4.6+ and ASP.NET vNext, but not yet available 
    ///       on every platform we support. See below for how we fall back.
    ///
    ///   (2) Deal with WinRT prefixes. 
    ///
    ///      When reading managed winmds with projections enabled, the metadata   reader needs to prepend 
    ///      a WinRT prefix in some case . Doing this without allocation poses a problem
    ///      as we don't have the prefix and input in contiguous data that we can pass to the
    ///      Encoding.GetString. We handle this case using pooled managed scratch buffers where we copy
    ///      the prefix and input and decode using Encoding.GetString(byte[], int, int).
    ///
    ///   (3) Deal with platforms that don't yet have Encoding.GetString(byte*, int). 
    ///   
    ///      If we're running on a full framework earlier than 4.6, we will bind to the internal
    ///      String.CreateStringFromEncoding which is equivalent and Encoding.GetString is just a trivial 
    ///      wrapper around it in .NET 4.6. This means that we always have the fast path on every
    ///      full framework version we support.
    ///
    ///      If we can't bind to it via reflection, then we emulate it using what is effectively (2) and 
    ///      with an empty prefix. 
    ///
    /// For both (2) and (3), the pooled buffers have a fixed size deemed large enough for the
    /// vast majority of metadata strings. In the rare worst case (byteCount > threshold and
    /// (lightUpAttemptFailed || prefix != null), we give up and allocate a temporary array,
    /// copy to it, decode, and throw it away.
    /// </summary>
    internal static unsafe class EncodingHelper
    {
        // Size of pooled buffers. Input larger than that is prefixed or given to us on a
        // platform that doesn't have unsafe Encoding.GetString, will cause us to
        // allocate and throw away a temporary buffer. The vast majority of metadata strings
        // are quite small so we don't need to waste memory with large buffers.
        public const int PooledBufferSize = 200;

        // The pooled buffers for (2) and (3) above. Use AcquireBuffer(int) and ReleaseBuffer(byte[])
        // instead of the pool directly to implement the size check.
        private static readonly ObjectPool<byte[]> s_pool = new ObjectPool<byte[]>(() => new byte[PooledBufferSize]);

        public static string DecodeUtf8(byte* bytes, int byteCount, byte[] prefix, MetadataStringDecoder utf8Decoder)
        {
            Debug.Assert(utf8Decoder != null);

            if (prefix != null)
            {
                return DecodeUtf8Prefixed(bytes, byteCount, prefix, utf8Decoder);
            }

            if (byteCount == 0)
            {
                return String.Empty;
            }

            return utf8Decoder.GetString(bytes, byteCount);
        }

        private static string DecodeUtf8Prefixed(byte* bytes, int byteCount, byte[] prefix, MetadataStringDecoder utf8Decoder)
        {
            Debug.Assert(utf8Decoder != null);

            int prefixedByteCount = byteCount + prefix.Length;

            if (prefixedByteCount == 0)
            {
                return String.Empty;
            }

            byte[] buffer = AcquireBuffer(prefixedByteCount);

            prefix.CopyTo(buffer, 0);
            Marshal.Copy((IntPtr)bytes, buffer, prefix.Length, byteCount);

            string result;
            fixed (byte* prefixedBytes = &buffer[0])
            {
                result = utf8Decoder.GetString(prefixedBytes, prefixedByteCount);
            }

            ReleaseBuffer(buffer);
            return result;
        }

        private static byte[] AcquireBuffer(int byteCount)
        {
            if (byteCount > PooledBufferSize)
            {
                return new byte[byteCount];
            }

            return s_pool.Allocate();
        }

        private static void ReleaseBuffer(byte[] buffer)
        {
            if (buffer.Length == PooledBufferSize)
            {
                s_pool.Free(buffer);
            }
        }

        // TODO: Remove everything in this region when we can retarget and use the real 
        //       Encoding.GetString(byte*, int) without reflection.
        #region Light-Up 

        internal delegate string Encoding_GetString(Encoding encoding, byte* bytes, int byteCount); // only internal for test hook
        private delegate string String_CreateStringFromEncoding(byte* bytes, int byteCount, Encoding encoding);

        private static Encoding_GetString s_getStringPlatform = LoadGetStringPlatform(); // only non-readonly for test hook

        public static string GetString(this Encoding encoding, byte* bytes, int byteCount)
        {
            Debug.Assert(encoding != null);

            if (s_getStringPlatform == null)
            {
                return GetStringPortable(encoding, bytes, byteCount);
            }

            return s_getStringPlatform(encoding, bytes, byteCount);
        }

        private static unsafe string GetStringPortable(Encoding encoding, byte* bytes, int byteCount)
        {
            // This implementation can leak publicly (by design) to MetadataStringDecoder.GetString.
            // Therefore we implement the same validation.

            Debug.Assert(encoding != null); // validated by MetadataStringDecoder constructor.

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            byte[] buffer = AcquireBuffer(byteCount);
            Marshal.Copy((IntPtr)bytes, buffer, 0, byteCount);
            string result = encoding.GetString(buffer, 0, byteCount);
            ReleaseBuffer(buffer);
            return result;
        }

        private static Encoding_GetString LoadGetStringPlatform()
        {
            // .NET Framework 4.6+ and recent versions of other .NET platforms.
            //
            // Try to bind to Encoding.GetString(byte*, int);

            MethodInfo getStringInfo = LightUpHelper.GetMethod(typeof(Encoding), "GetString", typeof(byte*), typeof(int));

            if (getStringInfo != null && getStringInfo.ReturnType == typeof(String))
            {
                try
                {
                    return (Encoding_GetString)getStringInfo.CreateDelegate(typeof(Encoding_GetString), null);
                }
                catch (MemberAccessException)
                {
                }
                catch (InvalidOperationException)
                {
                    // thrown when accessing unapproved API in a Windows Store app
                }
            }

            // .NET Framework < 4.6
            //
            // Try to bind to String.CreateStringFromEncoding(byte*, int, Encoding)
            //
            // Note that this one is internal and GetRuntimeMethod only searches public methods, which accounts for the different
            // pattern from above. 
            //
            // NOTE: Another seeming equivalent is new string(sbyte*, int, Encoding), but  don't be fooled. First of all, we can't get 
            //       a delegate to a constructor. Worst than that, even if we could, it is actually about 4x slower than both of these
            //       and even the portable version (only ~20% slower than these) crushes it.
            //
            //       It spends an inordinate amount of time transitioning to managed code from the VM and then lands in String.CreateString
            //       (note no FromEncoding suffix), which defensively copies to a new byte array on every call -- defeating the entire purpose 
            //       of this class!
            //
            //       For this reason, desktop callers should not implement an interner that falls back to the unsafe string ctor but instead
            //       return null and let us find the best decoding approach for the current platform.
            //
            //       Yet another approach is to use new string('\0', GetCharCount) and use unsafe GetChars to fill it.
            //       However, on .NET < 4.6, there isn't no-op fast path for zero-initialization case so we'd slow down.
            //       Plus, mutating a System.String is no better than the reflection here.

            IEnumerable<MethodInfo> createStringInfos = typeof(String).GetTypeInfo().GetDeclaredMethods("CreateStringFromEncoding");
            foreach (var methodInfo in createStringInfos)
            {
                var parameters = methodInfo.GetParameters();
                if (parameters.Length == 3
                    && parameters[0].ParameterType == typeof(byte*)
                    && parameters[1].ParameterType == typeof(int)
                    && parameters[2].ParameterType == typeof(Encoding)
                    && methodInfo.ReturnType == typeof(String))
                {
                    try
                    {
                        var createStringFromEncoding = (String_CreateStringFromEncoding)methodInfo.CreateDelegate(typeof(String_CreateStringFromEncoding), null);
                        return (encoding, bytes, byteCount) => GetStringUsingCreateStringFromEncoding(createStringFromEncoding, bytes, byteCount, encoding);
                    }
                    catch (MemberAccessException)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                        // thrown when accessing unapproved API in a Windows Store app
                    }
                }
            }

            // Other platforms: Give up and fall back to GetStringPortable above.
            return null;
        }

        private static unsafe string GetStringUsingCreateStringFromEncoding(
            String_CreateStringFromEncoding createStringFromEncoding,
            byte* bytes,
            int byteCount,
            Encoding encoding)
        {
            // String.CreateStringFromEncoding is an internal method that does not validate
            // arguments, but this implementation can leak publicly (by design) via
            // MetadataStringDecoder.GetString. Therefore, we implement the same validation
            // that Encoding.GetString would do if it were available directly.

            Debug.Assert(encoding != null); // validated by MetadataStringDecoder constructor.

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            return createStringFromEncoding(bytes, byteCount, encoding);
        }

        // Test hook to force portable implementation and ensure light is functioning.
        internal static bool TestOnly_LightUpEnabled
        {
            get { return s_getStringPlatform != null; }
            set { s_getStringPlatform = value ? LoadGetStringPlatform() : null; }
        }
        #endregion
    }
}
