// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ObjectiveC;
using System.Runtime.Loader;
using System.Text;
using System.Threading;

using Internal.Runtime;
using Internal.Runtime.Augments;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// These methods are used to throw exceptions from generated code.
    /// </summary>
    internal static class InteropHelpers
    {
        internal static unsafe byte* StringToAnsiString(string str, bool bestFit, bool throwOnUnmappableChar)
        {
            return PInvokeMarshal.StringToAnsiString(str, bestFit, throwOnUnmappableChar);
        }

        public static unsafe string AnsiStringToString(byte* buffer)
        {
            return PInvokeMarshal.AnsiStringToString(buffer);
        }

        internal static unsafe void StringToByValAnsiString(string str, byte* pNative, int charCount, bool bestFit, bool throwOnUnmappableChar)
        {
            if (str != null)
            {
                // Truncate the string if it is larger than specified by SizeConst
                int lenUnicode = str.Length;
                if (lenUnicode >= charCount)
                    lenUnicode = charCount - 1;

                fixed (char* pManaged = str)
                {
                    PInvokeMarshal.StringToAnsiString(pManaged, lenUnicode, pNative, /*terminateWithNull=*/true, bestFit, throwOnUnmappableChar);
                }
            }
            else
            {
                (*pNative) = (byte)'\0';
            }
        }

        public static unsafe string ByValAnsiStringToString(byte* buffer, int length)
        {
            int end = new ReadOnlySpan<byte>(buffer, length).IndexOf((byte)0);
            if (end >= 0)
            {
                length = end;
            }

            return new string((sbyte*)buffer, 0, length);
        }

        internal static unsafe void StringToUnicodeFixedArray(string str, ushort* buffer, int length)
        {
            ReadOnlySpan<char> managed = str;
            Span<char> native = new Span<char>((char*)buffer, length);

            int numChars = Math.Min(managed.Length, length - 1);

            managed.Slice(0, numChars).CopyTo(native);
            native[numChars] = '\0';
        }

        internal static unsafe string UnicodeToStringFixedArray(ushort* buffer, int length)
        {
            int end = new ReadOnlySpan<char>(buffer, length).IndexOf('\0');
            if (end >= 0)
            {
                length = end;
            }

            return new string((char*)buffer, 0, length);
        }

        internal static unsafe char* StringToUnicodeBuffer(string str)
        {
            return (char*)Marshal.StringToCoTaskMemUni(str);
        }

        public static unsafe byte* AllocMemoryForAnsiStringBuilder(StringBuilder sb)
        {
            if (sb == null)
            {
                return null;
            }
            return (byte *)CoTaskMemAllocAndZeroMemory(checked((sb.Capacity + 2) * Marshal.SystemMaxDBCSCharSize));
        }

        public static unsafe char* AllocMemoryForUnicodeStringBuilder(StringBuilder sb)
        {
            if (sb == null)
            {
                return null;
            }
            return (char *)CoTaskMemAllocAndZeroMemory(checked((sb.Capacity + 2) * 2));
        }

        public static unsafe byte* AllocMemoryForAnsiCharArray(char[] chArray)
        {
            if (chArray == null)
            {
                return null;
            }
            return (byte*)CoTaskMemAllocAndZeroMemory(checked((chArray.Length + 2) * Marshal.SystemMaxDBCSCharSize));
        }

        public static unsafe void AnsiStringToStringBuilder(byte* newBuffer, System.Text.StringBuilder stringBuilder)
        {
            if (stringBuilder == null)
                return;

            PInvokeMarshal.AnsiStringToStringBuilder(newBuffer, stringBuilder);
        }

        public static unsafe void UnicodeStringToStringBuilder(ushort* newBuffer, System.Text.StringBuilder stringBuilder)
        {
            if (stringBuilder == null)
                return;

            PInvokeMarshal.UnicodeStringToStringBuilder(newBuffer, stringBuilder);
        }

        public static unsafe void StringBuilderToAnsiString(System.Text.StringBuilder stringBuilder, byte* pNative,
            bool bestFit, bool throwOnUnmappableChar)
        {
            if (pNative == null)
                return;

            PInvokeMarshal.StringBuilderToAnsiString(stringBuilder, pNative, bestFit, throwOnUnmappableChar);
        }

        public static unsafe void StringBuilderToUnicodeString(System.Text.StringBuilder stringBuilder, ushort* destination)
        {
            if (destination == null)
                return;

            PInvokeMarshal.StringBuilderToUnicodeString(stringBuilder, destination);
        }

        public static unsafe void WideCharArrayToAnsiCharArray(char[] managedArray, byte* pNative, bool bestFit, bool throwOnUnmappableChar)
        {
            PInvokeMarshal.WideCharArrayToAnsiCharArray(managedArray, pNative, bestFit, throwOnUnmappableChar);
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
            PInvokeMarshal.AnsiCharArrayToWideCharArray(pNative, managedArray);
        }

        /// <summary>
        /// Convert a single UNICODE wide char to a single ANSI byte.
        /// </summary>
        /// <param name="managedValue">single UNICODE wide char value</param>
        /// <param name="bestFit">Enable best-fit mapping behavior</param>
        /// <param name="throwOnUnmappableChar">Throw an exception on an unmappable Unicode character</param>
        public static unsafe byte WideCharToAnsiChar(char managedValue, bool bestFit, bool throwOnUnmappableChar)
        {
            return PInvokeMarshal.WideCharToAnsiChar(managedValue, bestFit, throwOnUnmappableChar);
        }

        /// <summary>
        /// Convert a single ANSI byte value to a single UNICODE wide char value, best fit.
        /// </summary>
        /// <param name="nativeValue">Single ANSI byte value.</param>
        public static unsafe char AnsiCharToWideChar(byte nativeValue)
        {
            return PInvokeMarshal.AnsiCharToWideChar(nativeValue);
        }

        internal static double DateTimeToOleDateTime(DateTime value)
        {
            return value.ToOADate();
        }

        internal static DateTime OleDateTimeToDateTime(double value)
        {
            return DateTime.FromOADate(value);
        }

        internal static long DecimalToOleCurrency(decimal value)
        {
            return decimal.ToOACurrency(value);
        }

        internal static decimal OleCurrencyToDecimal(long value)
        {
            return decimal.FromOACurrency(value);
        }

        internal static unsafe string BstrBufferToString(char* buffer)
        {
            if (buffer == null)
                return null;

            return Marshal.PtrToStringBSTR((IntPtr)buffer);
        }

        internal static unsafe byte* StringToAnsiBstrBuffer(string s)
        {
            if (s is null)
            {
                return (byte*)IntPtr.Zero;
            }

            int stringLength = s.Length;
            fixed (char* pStr = s)
            {
                int nativeLength = PInvokeMarshal.GetByteCount(pStr, stringLength);
                byte* bstr = (byte*)Marshal.AllocBSTRByteLen((uint)nativeLength);
                PInvokeMarshal.ConvertWideCharToMultiByte(pStr, stringLength, bstr, nativeLength, bestFit: false, throwOnUnmappableChar: false);
                return bstr;
            }
        }

        internal static unsafe string AnsiBstrBufferToString(byte* buffer)
        {
            if (buffer == null)
                return null;

            return Marshal.PtrToStringAnsi((IntPtr)buffer, (int)Marshal.SysStringByteLen((IntPtr)buffer));
        }

        internal static unsafe IntPtr ResolvePInvoke(MethodFixupCell* pCell)
        {
            if (pCell->Target != IntPtr.Zero)
                return pCell->Target;

            return ResolvePInvokeSlow(pCell);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe IntPtr ResolvePInvokeSlow(MethodFixupCell* pCell)
        {
            int lastSystemError = Marshal.GetLastSystemError();

            ModuleFixupCell* pModuleCell = pCell->Module;
            IntPtr hModule = pModuleCell->Handle;
            if (hModule == IntPtr.Zero)
            {
                FixupModuleCell(pModuleCell);
                hModule = pModuleCell->Handle;
            }

            FixupMethodCell(hModule, pCell);

            Marshal.SetLastSystemError(lastSystemError);

            return pCell->Target;
        }

        internal static unsafe void FreeLibrary(IntPtr hModule)
        {
#if !TARGET_UNIX
            Interop.Kernel32.FreeLibrary(hModule);
#else
            Interop.Sys.FreeLibrary(hModule);
#endif
        }

        private static unsafe string GetModuleName(ModuleFixupCell* pCell)
        {
            byte* pModuleName = (byte*)pCell->ModuleName;
            return Encoding.UTF8.GetString(pModuleName, string.strlen(pModuleName));
        }

        internal static unsafe void FixupModuleCell(ModuleFixupCell* pCell)
        {
            string moduleName = GetModuleName(pCell);

            uint dllImportSearchPath = 0;
            bool hasDllImportSearchPath = (pCell->DllImportSearchPathAndCookie & InteropDataConstants.HasDllImportSearchPath) != 0;
            if (hasDllImportSearchPath)
            {
                dllImportSearchPath = pCell->DllImportSearchPathAndCookie & ~InteropDataConstants.HasDllImportSearchPath;
            }

            Assembly callingAssembly = RuntimeAugments.Callbacks.GetAssemblyForHandle(new RuntimeTypeHandle(pCell->CallingAssemblyType));

            // First check if there's a NativeLibrary callback and call it to attempt the resolution
            IntPtr hModule = NativeLibrary.LoadLibraryCallbackStub(moduleName, callingAssembly, hasDllImportSearchPath, dllImportSearchPath);
            if (hModule == IntPtr.Zero)
            {
                // NativeLibrary callback didn't resolve the library. Use built-in rules.
                NativeLibrary.LoadLibErrorTracker loadLibErrorTracker = default;

                hModule = NativeLibrary.LoadBySearch(
                    callingAssembly,
                    searchAssemblyDirectory: (dllImportSearchPath & (uint)DllImportSearchPath.AssemblyDirectory) != 0,
                    dllImportSearchPathFlags: (int)(dllImportSearchPath & ~(uint)DllImportSearchPath.AssemblyDirectory),
                    ref loadLibErrorTracker,
                    moduleName);

                if (hModule == IntPtr.Zero)
                {
                    // Built-in rules didn't resolve the library. Use AssemblyLoadContext as a last chance attempt.
                    AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(callingAssembly)!;
                    hModule = loadContext.GetResolvedUnmanagedDll(callingAssembly, moduleName);
                }

                if (hModule == IntPtr.Zero)
                {
                    // If the module is still unresolved, this is an error.
                    loadLibErrorTracker.Throw(moduleName);
                }
            }

            Debug.Assert(hModule != IntPtr.Zero);
            var oldValue = Interlocked.CompareExchange(ref pCell->Handle, hModule, IntPtr.Zero);
            if (oldValue != IntPtr.Zero)
            {
                // Some other thread won the race to fix it up.
                FreeLibrary(hModule);
            }
        }

        internal static unsafe void FixupMethodCell(IntPtr hModule, MethodFixupCell* pCell)
        {
            byte* methodName = (byte*)pCell->MethodName;
            IntPtr pTarget;

#if FEATURE_OBJCMARSHAL
#pragma warning disable CA1416
            if (pCell->IsObjectiveCMessageSend && ObjectiveCMarshal.TryGetGlobalMessageSendCallback(pCell->ObjectiveCMessageSendFunction, out pTarget))
            {
                Debug.Assert(pTarget != IntPtr.Zero);
                pCell->Target = pTarget;
                return;
            }
#pragma warning restore CA1416
#endif

#if TARGET_WINDOWS
            CharSet charSetMangling = pCell->CharSetMangling;
            if (charSetMangling == 0)
            {
                // Look for the user-provided entry point name only
                pTarget = Interop.Kernel32.GetProcAddress(hModule, methodName);
            }
            else
            if (charSetMangling == CharSet.Ansi)
            {
                // For ANSI, look for the user-provided entry point name first.
                // If that does not exist, try the charset suffix.
                pTarget = Interop.Kernel32.GetProcAddress(hModule, methodName);
                if (pTarget == IntPtr.Zero)
                    pTarget = GetProcAddressWithSuffix(hModule, methodName, (byte)'A');
            }
            else
            {
                // For Unicode, look for the entry point name with the charset suffix first.
                // The 'W' API takes precedence over the undecorated one.
                pTarget = GetProcAddressWithSuffix(hModule, methodName, (byte)'W');
                if (pTarget == IntPtr.Zero)
                    pTarget = Interop.Kernel32.GetProcAddress(hModule, methodName);
            }
#else
            pTarget = Interop.Sys.GetProcAddress(hModule, methodName);
#endif
            if (pTarget == IntPtr.Zero)
            {
                string entryPointName = Encoding.UTF8.GetString(methodName, string.strlen(methodName));
                throw new EntryPointNotFoundException(SR.Format(SR.Arg_EntryPointNotFoundExceptionParameterized, entryPointName, GetModuleName(pCell->Module)));
            }

            pCell->Target = pTarget;
        }

#if TARGET_WINDOWS
        private static unsafe IntPtr GetProcAddressWithSuffix(IntPtr hModule, byte* methodName, byte suffix)
        {
            int nameLength = string.strlen(methodName);

            // We need to add an extra byte for the suffix, and an extra byte for the null terminator
            byte* probedMethodName = stackalloc byte[nameLength + 2];

            for (int i = 0; i < nameLength; i++)
            {
                probedMethodName[i] = methodName[i];
            }

            probedMethodName[nameLength + 1] = 0;

            probedMethodName[nameLength] = suffix;

            return Interop.Kernel32.GetProcAddress(hModule, probedMethodName);
        }
#endif

        internal static unsafe void* CoTaskMemAllocAndZeroMemory(int size)
        {
            byte* ptr = (byte*)Marshal.AllocCoTaskMem(size);

            // Marshal.AllocCoTaskMem will throw OOMException if out of memory
            Debug.Assert(ptr != null);

            NativeMemory.Clear(ptr, (uint)size);
            return ptr;
        }

        /// <summary>
        /// Retrieves the function pointer for the current open static delegate that is being called
        /// </summary>
        public static IntPtr GetCurrentCalleeOpenStaticDelegateFunctionPointer()
        {
            return PInvokeMarshal.GetCurrentCalleeOpenStaticDelegateFunctionPointer();
        }

        /// <summary>
        /// Retrieves the current delegate that is being called
        /// </summary>
        public static T GetCurrentCalleeDelegate<T>() where T : class // constraint can't be System.Delegate
        {
            return PInvokeMarshal.GetCurrentCalleeDelegate<T>();
        }

        public static IntPtr ConvertManagedComInterfaceToNative(object pUnk, Guid interfaceGuid)
        {
            if (pUnk == null)
            {
                return IntPtr.Zero;
            }

#if TARGET_WINDOWS
#pragma warning disable CA1416
            return ComWrappers.ComInterfaceForObject(pUnk, interfaceGuid);
#pragma warning restore CA1416
#else
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
#endif
        }

        public static IntPtr ConvertManagedComInterfaceToIUnknown(object pUnk)
        {
            if (pUnk == null)
            {
                return IntPtr.Zero;
            }

#if TARGET_WINDOWS
#pragma warning disable CA1416
            return ComWrappers.ComInterfaceForObject(pUnk);
#pragma warning restore CA1416
#else
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
#endif
        }

        public static object ConvertNativeComInterfaceToManaged(IntPtr pUnk)
        {
            if (pUnk == IntPtr.Zero)
            {
                return null;
            }

#if TARGET_WINDOWS
#pragma warning disable CA1416
            return ComWrappers.ComObjectForInterface(pUnk);
#pragma warning restore CA1416
#else
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
#endif
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "This API will be called from compiler generated code only.")]
        internal static int AsAnyGetNativeSize(object o)
        {
            // Array, string and StringBuilder are not implemented.
            if (o.GetEETypePtr().IsArray ||
                o is string ||
                o is StringBuilder)
            {
                throw new PlatformNotSupportedException();
            }

            // Assume that this is a type with layout.
            return Marshal.SizeOf(o.GetType());
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "This API will be called from compiler generated code only.")]
        internal static void AsAnyMarshalManagedToNative(object o, IntPtr address)
        {
            // Array, string and StringBuilder are not implemented.
            if (o.GetEETypePtr().IsArray ||
                o is string ||
                o is StringBuilder)
            {
                throw new PlatformNotSupportedException();
            }

            Marshal.StructureToPtr(o, address, fDeleteOld: false);
        }

        internal static void AsAnyMarshalNativeToManaged(IntPtr address, object o)
        {
            // Array, string and StringBuilder are not implemented.
            if (o.GetEETypePtr().IsArray ||
                o is string ||
                o is StringBuilder)
            {
                throw new PlatformNotSupportedException();
            }

            Marshal.PtrToStructureImpl(address, o);
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "This API will be called from compiler generated code only.")]
        internal static void AsAnyCleanupNative(IntPtr address, object o)
        {
            // Array, string and StringBuilder are not implemented.
            if (o.GetEETypePtr().IsArray ||
                o is string ||
                o is StringBuilder)
            {
                throw new PlatformNotSupportedException();
            }

            Marshal.DestroyStructure(address, o.GetType());
        }

        internal static unsafe object? VariantToObject(IntPtr pSrcNativeVariant)
        {
            if (pSrcNativeVariant == IntPtr.Zero)
            {
                return null;
            }

#if TARGET_WINDOWS
#pragma warning disable CA1416
            return Marshal.GetObjectForNativeVariant(pSrcNativeVariant);
#pragma warning restore CA1416
#else
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
#endif
        }

        internal static unsafe void ConvertObjectToVariant(object? obj, IntPtr pDstNativeVariant)
        {
#if TARGET_WINDOWS
#pragma warning disable CA1416
            Marshal.GetNativeVariantForObject(obj, pDstNativeVariant);
#pragma warning restore CA1416
#else
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
#endif
        }

        internal static unsafe void CleanupVariant(IntPtr pDstNativeVariant)
        {
#if TARGET_WINDOWS
#pragma warning disable CA1416
            Variant* data = (Variant*)pDstNativeVariant;
            data->Clear();
#pragma warning restore CA1416
#else
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
#endif
        }

        public static unsafe object InitializeCustomMarshaller(RuntimeTypeHandle pParameterType, RuntimeTypeHandle pMarshallerType, string cookie, delegate*<string, object> getInstanceMethod)
        {
            if (getInstanceMethod == null)
            {
                throw new ApplicationException();
            }

            if (!RuntimeImports.AreTypesAssignable(pMarshallerType.ToEETypePtr(), EETypePtr.EETypePtrOf<ICustomMarshaler>()))
            {
                throw new ApplicationException();
            }

            var marshaller = CustomMarshallerTable.s_customMarshallersTable.GetOrAdd(new CustomMarshallerKey(pParameterType, pMarshallerType, cookie, getInstanceMethod));
            if (marshaller == null)
            {
                throw new ApplicationException();
            }

            if (!RuntimeImports.AreTypesAssignable(marshaller.GetEETypePtr(), EETypePtr.EETypePtrOf<ICustomMarshaler>()))
            {
                throw new ApplicationException();
            }

            return marshaller;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct ModuleFixupCell
        {
            public IntPtr Handle;
            public IntPtr ModuleName;
            public EETypePtr CallingAssemblyType;
            public uint DllImportSearchPathAndCookie;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct MethodFixupCell
        {
            public IntPtr Target;
            public IntPtr MethodName;
            public ModuleFixupCell* Module;
            private int Flags;

            public CharSet CharSetMangling => (CharSet)(Flags & MethodFixupCellFlagsConstants.CharSetMask);
            public bool IsObjectiveCMessageSend => (Flags & MethodFixupCellFlagsConstants.IsObjectiveCMessageSendMask) != 0;
            public int ObjectiveCMessageSendFunction => (Flags & MethodFixupCellFlagsConstants.ObjectiveCMessageSendFunctionMask) >> MethodFixupCellFlagsConstants.ObjectiveCMessageSendFunctionShift;
        }

        internal unsafe struct CustomMarshallerKey : IEquatable<CustomMarshallerKey>
        {
            public CustomMarshallerKey(RuntimeTypeHandle pParameterType, RuntimeTypeHandle pMarshallerType, string cookie, delegate*<string, object> getInstanceMethod)
            {
                ParameterType = pParameterType;
                MarshallerType = pMarshallerType;
                Cookie = cookie;
                GetInstanceMethod = getInstanceMethod;
            }

            public RuntimeTypeHandle ParameterType { get; }
            public RuntimeTypeHandle MarshallerType { get; }
            public string Cookie { get; }
            public delegate*<string, object> GetInstanceMethod { get; }

            public override bool Equals(object obj)
            {
                if (!(obj is CustomMarshallerKey other))
                    return false;
                return Equals(other);
            }

            public bool Equals(CustomMarshallerKey other)
            {
                return ParameterType.Equals(other.ParameterType)
                    && MarshallerType.Equals(other.MarshallerType)
                    && Cookie.Equals(other.Cookie);
            }

            public override int GetHashCode()
            {
                return ParameterType.GetHashCode()
                    ^ MarshallerType.GetHashCode()
                    ^ Cookie.GetHashCode();
            }
        }

        internal sealed class CustomMarshallerTable : ConcurrentUnifier<CustomMarshallerKey, object>
        {
            internal static CustomMarshallerTable s_customMarshallersTable = new CustomMarshallerTable();

            protected override unsafe object Factory(CustomMarshallerKey key)
            {
                return key.GetInstanceMethod(key.Cookie);
            }
        }
    }
}
