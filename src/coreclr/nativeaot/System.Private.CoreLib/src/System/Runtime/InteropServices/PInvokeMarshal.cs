// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;

using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;

using Debug = System.Diagnostics.Debug;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// This PInvokeMarshal class should provide full public Marshal
    /// implementation for all things related to P/Invoke marshalling
    /// </summary>
    internal static partial class PInvokeMarshal
    {
        [ThreadStatic]
        internal static int t_lastError;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static int GetHRForException(Exception e)
        {
            if (e == null)
            {
                return HResults.S_OK;
            }

            // @TODO: Setup IErrorInfo
            return e.HResult;
        }

        #region Delegate marshalling

        private static object s_thunkPoolHeap;

        /// <summary>
        /// Return the stub to the pinvoke marshalling stub
        /// </summary>
        /// <param name="del">The delegate</param>
        public static unsafe IntPtr GetFunctionPointerForDelegate(Delegate del)
        {
            if (del == null)
                return IntPtr.Zero;

#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            if (del.GetMethodTable()->IsGeneric)
                throw new ArgumentException(SR.Argument_NeedNonGenericType, "delegate");
#pragma warning restore CA2208

            NativeFunctionPointerWrapper? fpWrapper = del.TryGetNativeFunctionPointerWrapper();
            if (fpWrapper != null)
            {
                //
                // Marshalling a delegate created from native function pointer back into function pointer
                // This is easy - just return the 'wrapped' native function pointer
                //
                return fpWrapper.NativeFunctionPointer;
            }
            else
            {
                //
                // Marshalling a managed delegate created from managed code into a native function pointer
                //
                return GetPInvokeDelegates().GetValue(del, s_AllocateThunk ??= AllocateThunk).Thunk;
            }
        }

        /// <summary>
        /// Used to lookup whether a delegate already has thunk allocated for it
        /// </summary>
        private static ConditionalWeakTable<Delegate, PInvokeDelegateThunk> s_pInvokeDelegates;
        private static ConditionalWeakTable<Delegate, PInvokeDelegateThunk>.CreateValueCallback s_AllocateThunk;

        private static ConditionalWeakTable<Delegate, PInvokeDelegateThunk> GetPInvokeDelegates()
        {
            //
            // Create the dictionary on-demand
            //
            if (s_pInvokeDelegates == null)
            {
                Interlocked.CompareExchange(
                    ref s_pInvokeDelegates,
                    new ConditionalWeakTable<Delegate, PInvokeDelegateThunk>(),
                    null
                );
            }

            return s_pInvokeDelegates;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct ThunkContextData
        {
            public GCHandle Handle;        //  A weak GCHandle to the delegate
            public IntPtr FunctionPtr;     // Function pointer for open static delegates
        }

        internal sealed unsafe class PInvokeDelegateThunk
        {
            public readonly IntPtr Thunk;        //  Thunk pointer
            public readonly IntPtr ContextData;  //  ThunkContextData pointer which will be stored in the context slot of the thunk

            public PInvokeDelegateThunk(Delegate del)
            {
                Thunk = RuntimeAugments.AllocateThunk(s_thunkPoolHeap);
                if (Thunk == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }

                //
                //  For open static delegates set target to ReverseOpenStaticDelegateStub which calls the static function pointer directly
                //
                IntPtr openStaticFunctionPointer = del.TryGetOpenStaticFunctionPointer();

                //
                // Allocate unmanaged memory for GCHandle of delegate and function pointer of open static delegate
                // We will store this pointer on the context slot of thunk data
                //
                unsafe
                {
                    ContextData = (IntPtr)NativeMemory.Alloc((nuint)(2 * IntPtr.Size));

                    ThunkContextData* thunkData = (ThunkContextData*)ContextData;

                    // allocate a weak GChandle for the delegate
                    thunkData->Handle = GCHandle.Alloc(del, GCHandleType.Weak);
                    thunkData->FunctionPtr = openStaticFunctionPointer;
                }

                IntPtr pTarget = RuntimeInteropData.GetDelegateMarshallingStub(new RuntimeTypeHandle(del.GetMethodTable()), openStaticFunctionPointer != IntPtr.Zero);
                Debug.Assert(pTarget != IntPtr.Zero);

                RuntimeAugments.SetThunkData(s_thunkPoolHeap, Thunk, ContextData, pTarget);
            }

            ~PInvokeDelegateThunk()
            {
                // Free the thunk
                if (Thunk != IntPtr.Zero)
                {
                    RuntimeAugments.FreeThunk(s_thunkPoolHeap, Thunk);
                }

                if (ContextData != IntPtr.Zero)
                {
                    // free the GCHandle
                    GCHandle handle = ((ThunkContextData*)ContextData)->Handle;
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }

                    // Free the allocated context data memory
                    NativeMemory.Free((void*)ContextData);
                }
            }
        }

        private static unsafe PInvokeDelegateThunk AllocateThunk(Delegate del)
        {
            if (s_thunkPoolHeap == null)
            {
                // TODO: Free s_thunkPoolHeap if the thread lose the race
                Interlocked.CompareExchange(
                    ref s_thunkPoolHeap,
                    RuntimeAugments.CreateThunksHeap(RuntimeImports.GetInteropCommonStubAddress()),
                    null
                );
                Debug.Assert(s_thunkPoolHeap != null);
            }

            return new PInvokeDelegateThunk(del);
        }

        /// <summary>
        /// Retrieve the corresponding P/invoke instance from the stub
        /// </summary>
        public static unsafe Delegate? GetDelegateForFunctionPointer(IntPtr ptr, RuntimeTypeHandle delegateType)
        {
            if (ptr == IntPtr.Zero)
                return null;
            //
            // First try to see if this is one of the thunks we've allocated when we marshal a managed
            // delegate to native code
            // s_thunkPoolHeap will be null if there isn't any managed delegate to native
            //
            IntPtr pContext;
            IntPtr pTarget;
            if (s_thunkPoolHeap != null && RuntimeAugments.TryGetThunkData(s_thunkPoolHeap, ptr, out pContext, out pTarget))
            {
                GCHandle handle;
                unsafe
                {
                    // Pull out Handle from context
                    handle = ((ThunkContextData*)pContext)->Handle;
                }
                Delegate target = Unsafe.As<Delegate>(handle.Target);

                //
                // The delegate might already been garbage collected
                // User should use GC.KeepAlive or whatever ways necessary to keep the delegate alive
                // until they are done with the native function pointer
                //
                if (target == null)
                {
                    Environment.FailFast(SR.Delegate_GarbageCollected);
                }

                return target;
            }

            //
            // Otherwise, the stub must be a pure native function pointer
            // We need to create the delegate that points to the invoke method of a
            // NativeFunctionPointerWrapper derived class
            //
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            if (delegateType.ToMethodTable()->BaseType != MethodTable.Of<MulticastDelegate>())
                throw new ArgumentException(SR.Arg_MustBeDelegate, "t");
#pragma warning restore CA2208

            IntPtr pDelegateCreationStub = RuntimeInteropData.GetForwardDelegateCreationStub(delegateType);
            Debug.Assert(pDelegateCreationStub != IntPtr.Zero);

            return ((delegate*<IntPtr, Delegate>)pDelegateCreationStub)(ptr);
        }

        /// <summary>
        /// Retrieves the function pointer for the current open static delegate that is being called
        /// </summary>
        public static IntPtr GetCurrentCalleeOpenStaticDelegateFunctionPointer()
        {
            //
            // RH keeps track of the current thunk that is being called through a secret argument / thread
            // statics. No matter how that's implemented, we get the current thunk which we can use for
            // look up later
            //
            IntPtr pContext = RuntimeImports.GetCurrentInteropThunkContext();
            Debug.Assert(pContext != IntPtr.Zero);

            IntPtr fnPtr;
            unsafe
            {
                // Pull out function pointer for open static delegate
                fnPtr = ((ThunkContextData*)pContext)->FunctionPtr;
            }
            Debug.Assert(fnPtr != IntPtr.Zero);

            return fnPtr;
        }

        /// <summary>
        /// Retrieves the current delegate that is being called
        /// </summary>
        public static T GetCurrentCalleeDelegate<T>() where T : class // constraint can't be System.Delegate
        {
            //
            // RH keeps track of the current thunk that is being called through a secret argument / thread
            // statics. No matter how that's implemented, we get the current thunk which we can use for
            // look up later
            //
            IntPtr pContext = RuntimeImports.GetCurrentInteropThunkContext();

            Debug.Assert(pContext != IntPtr.Zero);

            GCHandle handle;
            unsafe
            {
                // Pull out Handle from context
                handle = ((ThunkContextData*)pContext)->Handle;

            }

            T target = Unsafe.As<T>(handle.Target);

            //
            // The delegate might already been garbage collected
            // User should use GC.KeepAlive or whatever ways necessary to keep the delegate alive
            // until they are done with the native function pointer
            //
            if (target == null)
            {
                Environment.FailFast(SR.Delegate_GarbageCollected);
            }
            return target;
        }
        #endregion

        #region String marshalling
        public static unsafe void StringBuilderToUnicodeString(System.Text.StringBuilder stringBuilder, ushort* destination)
        {
            int length = stringBuilder.Length;
            stringBuilder.CopyTo(0, new Span<char>((char*)destination, length), length);
            destination[length] = '\0';
        }

        public static unsafe void UnicodeStringToStringBuilder(ushort* newBuffer, System.Text.StringBuilder stringBuilder)
        {
            stringBuilder.ReplaceBuffer((char*)newBuffer);
        }

        public static unsafe void StringBuilderToAnsiString(System.Text.StringBuilder stringBuilder, byte* pNative,
            bool bestFit, bool throwOnUnmappableChar)
        {
            int len;

            // Convert StringBuilder to UNICODE string

            // Optimize for the most common case. If there is only a single char[] in the StringBuilder,
            // get it and convert it to ANSI
            char[] buffer = stringBuilder.GetBuffer(out len);

            if (buffer != null)
            {
                fixed (char* pManaged = buffer)
                {
                    StringToAnsiString(pManaged, len, pNative, /*terminateWithNull=*/true, bestFit, throwOnUnmappableChar);
                }
            }
            else // Otherwise, convert StringBuilder to string and then convert to ANSI
            {
                string str = stringBuilder.ToString();

                // Convert UNICODE string to ANSI string
                fixed (char* pManaged = str)
                {
                    StringToAnsiString(pManaged, str.Length, pNative, /*terminateWithNull=*/true, bestFit, throwOnUnmappableChar);
                }
            }
        }

        public static unsafe void AnsiStringToStringBuilder(byte* newBuffer, System.Text.StringBuilder stringBuilder)
        {
            if (newBuffer == null)
                throw new ArgumentNullException(nameof(newBuffer));

            int lenAnsi;
            int lenUnicode;
            CalculateStringLength(newBuffer, out lenAnsi, out lenUnicode);

            if (lenUnicode > 0)
            {
                char[] buffer = new char[lenUnicode];
                fixed (char* pTemp = &buffer[0])
                {
                    ConvertMultiByteToWideChar(newBuffer,
                                               lenAnsi,
                                               pTemp,
                                               lenUnicode);
                }
                stringBuilder.ReplaceBuffer(buffer);
            }
            else
            {
                stringBuilder.Clear();
            }
        }

        /// <summary>
        /// Convert ANSI string to unicode string, with option to free native memory.
        /// </summary>
        /// <remarks>Input assumed to be zero terminated. Generates String.Empty for zero length string.
        /// This version is more efficient than ConvertToUnicode in src\Interop\System\Runtime\InteropServices\Marshal.cs in that it can skip calling
        /// MultiByteToWideChar for ASCII string, and it does not need another char[] buffer</remarks>
        public static unsafe string? AnsiStringToString(byte* pchBuffer)
        {
            if (pchBuffer == null)
            {
                return null;
            }

            int lenAnsi;
            int lenUnicode;
            CalculateStringLength(pchBuffer, out lenAnsi, out lenUnicode);

            string result = string.Empty;

            if (lenUnicode > 0)
            {
                result = string.FastAllocateString(lenUnicode);

                fixed (char* pTemp = result)
                {
                    ConvertMultiByteToWideChar(pchBuffer,
                                               lenAnsi,
                                               pTemp,
                                               lenUnicode);
                }
            }

            return result;
        }

        /// <summary>
        /// Convert UNICODE string to ANSI string.
        /// </summary>
        /// <remarks>This version is more efficient than StringToHGlobalAnsi in Interop\System\Runtime\InteropServices\Marshal.cs in that
        /// it could allocate single byte per character, instead of SystemMaxDBCSCharSize per char, and it can skip calling WideCharToMultiByte for ASCII string</remarks>
        public static unsafe byte* StringToAnsiString(string str, bool bestFit, bool throwOnUnmappableChar)
        {
            if (str != null)
            {
                int lenUnicode = str.Length;

                fixed (char* pManaged = str)
                {
                    return StringToAnsiString(pManaged, lenUnicode, null, /*terminateWithNull=*/true, bestFit, throwOnUnmappableChar);
                }
            }

            return null;
        }

        public static unsafe void WideCharArrayToAnsiCharArray(char[] managedArray, byte* pNative, bool bestFit, bool throwOnUnmappableChar)
        {
            // Do nothing if array is NULL. This matches desktop CLR behavior
            if (managedArray == null)
                return;

            // Desktop CLR crash (AV at runtime) - we can do better in .NET Native
            if (pNative == null)
                throw new ArgumentNullException(nameof(pNative));

            int lenUnicode = managedArray.Length;
            fixed (char* pManaged = managedArray)
            {
                StringToAnsiString(pManaged, lenUnicode, pNative, /*terminateWithNull=*/false, bestFit, throwOnUnmappableChar);
            }
        }

        /// <summary>
        /// Convert ANSI ByVal byte array to UNICODE wide char array, best fit
        /// </summary>
        /// <remarks>
        /// * This version works with array instead to string, it means that the len must be provided and there will be NO NULL to
        /// terminate the array.
        /// * The buffer to the UNICODE wide char array must be allocated by the caller.
        /// </remarks>
        /// <param name="pNative">Pointer to the ANSI byte array. Could NOT be null.</param>
        /// <param name="managedArray">Wide char array that has already been allocated.</param>
        public static unsafe void AnsiCharArrayToWideCharArray(byte* pNative, char[] managedArray)
        {
            // Do nothing if native is NULL. This matches desktop CLR behavior
            if (pNative == null)
                return;

            // Desktop CLR crash (AV at runtime) - we can do better in .NET Native
            ArgumentNullException.ThrowIfNull(managedArray);

            // COMPAT: Use the managed array length as the maximum length of native buffer
            // This obviously doesn't make sense but desktop CLR does that
            int lenInBytes = managedArray.Length;
            fixed (char* pManaged = managedArray)
            {
                ConvertMultiByteToWideChar(pNative,
                                           lenInBytes,
                                           pManaged,
                                           lenInBytes);
            }
        }

        /// <summary>
        /// Convert a single UNICODE wide char to a single ANSI byte.
        /// </summary>
        /// <param name="managedValue">single UNICODE wide char value</param>
        /// <param name="bestFit">Enable best-fit mapping behavior</param>
        /// <param name="throwOnUnmappableChar">Throw an exception on an unmappable Unicode character</param>
        public static unsafe byte WideCharToAnsiChar(char managedValue, bool bestFit, bool throwOnUnmappableChar)
        {
            // @TODO - we really shouldn't allocate one-byte arrays and then destroy it
            byte* nativeArray = StringToAnsiString(&managedValue, 1, null, /*terminateWithNull=*/false, bestFit, throwOnUnmappableChar);
            byte native = (*nativeArray);
            Marshal.FreeCoTaskMem(new IntPtr(nativeArray));
            return native;
        }

        /// <summary>
        /// Convert a single ANSI byte value to a single UNICODE wide char value, best fit.
        /// </summary>
        /// <param name="nativeValue">Single ANSI byte value.</param>
        public static unsafe char AnsiCharToWideChar(byte nativeValue)
        {
            char ch;
            ConvertMultiByteToWideChar(&nativeValue, 1, &ch, 1);
            return ch;
        }

        // c# string (UTF-16) to UTF-8 encoded byte array
        internal static unsafe byte* StringToAnsiString(char* pManaged, int lenUnicode, byte* pNative, bool terminateWithNull,
            bool bestFit, bool throwOnUnmappableChar)
        {
            bool allAscii = Ascii.IsValid(new ReadOnlySpan<char>(pManaged, lenUnicode));
            int length;

            if (allAscii) // If all ASCII, map one UNICODE character to one ANSI char
            {
                length = lenUnicode;
            }
            else // otherwise, let OS count number of ANSI chars
            {
                length = GetByteCount(pManaged, lenUnicode);
            }

            if (pNative == null)
            {
                pNative = (byte*)Marshal.AllocCoTaskMem(checked(length + 1));
            }
            if (allAscii) // ASCII conversion
            {
                OperationStatus conversionStatus = Ascii.FromUtf16(new ReadOnlySpan<char>(pManaged, length), new Span<byte>(pNative, length), out _);
                Debug.Assert(conversionStatus == OperationStatus.Done);
            }
            else // Let OS convert
            {
                ConvertWideCharToMultiByte(pManaged,
                                           lenUnicode,
                                           pNative,
                                           length,
                                           bestFit,
                                           throwOnUnmappableChar);
            }

            // Zero terminate
            if (terminateWithNull)
                *(pNative + length) = 0;

            return pNative;
        }

        /// <summary>
        /// This is a auxiliary function that counts the length of the ansi buffer and
        ///  estimate the length of the buffer in Unicode. It returns true if all bytes
        ///  in the buffer are ANSII.
        /// </summary>
        private static unsafe bool CalculateStringLength(byte* pchBuffer, out int ansiBufferLen, out int unicodeBufferLen)
        {
            ReadOnlySpan<byte> span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pchBuffer);
            ansiBufferLen = span.Length;
            bool allAscii = Ascii.IsValid(span);

            if (allAscii)
            {
                unicodeBufferLen = ansiBufferLen;
            }
            else // If non ASCII, let OS calculate number of characters
            {
                unicodeBufferLen = GetCharCount(pchBuffer, ansiBufferLen);
            }
            return allAscii;
        }

        #endregion
    }
}
