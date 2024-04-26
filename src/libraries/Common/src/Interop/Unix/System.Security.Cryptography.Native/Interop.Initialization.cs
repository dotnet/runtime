// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    // Initialization of libcrypto threading support is done in a static constructor.
    // This enables a project simply to include this file, and any usage of any of
    // the System.Security.Cryptography.Native functions will trigger
    // initialization of the threading support.

    internal static partial class Crypto
    {
        static Crypto()
        {
            CryptoInitializer.Initialize();
        }
    }

    internal static partial class OpenSsl
    {
        static OpenSsl()
        {
            CryptoInitializer.Initialize();
        }
    }

    internal static unsafe partial class CryptoInitializer
    {
        internal struct MemoryEntry
        {
            public char* File;
            public int Size;
            public int Line;
        }

        private static readonly bool DebugMemory = GetMemoryDebug("DOTNET_SYSTEM_NET_SECURITY_OPENSSL_MEMORY_DEBUG");
        private static readonly bool ValidateMemory = GetMemoryDebug("DOTNET_SYSTEM_NET_SECURITY_OPENSSL_MEMORY_VALIDATE");
        private static readonly IntPtr Offset = sizeof(MemoryEntry);
        private static HashSet<IntPtr>? _allocations;
        private static HashSet<IntPtr>? _allocationsDiff;
        private static bool _trackIncrementalAllocations;

        internal static long TotalAllocatedMemory;
        internal static long TotalAllocations;

#pragma warning disable CA1810
        static unsafe CryptoInitializer()
        {
            if (DebugMemory)
            {
                // we need to prepare everything as some allocations do happen during initialization itself.
                _allocations = new HashSet<IntPtr>();
                _allocationsDiff = new HashSet<IntPtr>();
            }

            if (EnsureOpenSslInitialized(DebugMemory ? &CryptoMalloc : null, DebugMemory ? &CryptoRealloc : null, DebugMemory ? &CryptoFree : null) != 0)
            {
                // Ideally this would be a CryptographicException, but we use
                // OpenSSL in libraries lower than System.Security.Cryptography.
                // It's not a big deal, though: this will already be wrapped in a
                // TypeLoadException, and this failing means something is very
                // wrong with the system's configuration and any code using
                // these libraries will be unable to operate correctly.
                throw new InvalidOperationException();
            }
        }
#pragma warning restore CA1810

        internal static void Initialize()
        {
            // No-op that exists to provide a hook for other static constructors.
        }

        private static bool GetMemoryDebug(string name)
        {
            string? value = Environment.GetEnvironmentVariable(name);
            if (int.TryParse(value, CultureInfo.InvariantCulture, out int enabled))
            {
                if (enabled == 2)
                {
                    EnableTracking();
                }
                else if (enabled == 3)
                {
                    GetIncrementalAllocations();
                }

                return enabled == 1;
            }

            return false;
        }

        internal static void EnableTracking()
        {
            _allocationsDiff!.Clear();
            _trackIncrementalAllocations = true;
        }

        internal static Tuple<IntPtr, int, string>[] GetIncrementalAllocations()
        {
            lock (_allocationsDiff!)
            {
                Tuple<IntPtr, int, string>[] allocations = new Tuple<IntPtr, int, string>[_allocationsDiff.Count];
                int index = 0;
                foreach (IntPtr ptr in _allocationsDiff)
                {
                    Span<MemoryEntry> entry = new Span<MemoryEntry>((void*)ptr, 1);
                    allocations[index] = new Tuple<IntPtr, int, string>(ptr+Offset, entry[0].Size, $"{Marshal.PtrToStringAnsi((IntPtr)entry[0].File)}:{entry[0].Line}");
                    index++;
                }

                return allocations;
            }
        }

        internal static void DisableTracking()
        {
            _trackIncrementalAllocations = false;
            _allocationsDiff!.Clear();
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EnsureOpenSslInitialized")]
        private static unsafe partial int EnsureOpenSslInitialized(delegate* unmanaged<UIntPtr, char*, int, void*> mallocFunction, delegate* unmanaged<void*, UIntPtr, char*, int, void*> reallocFunction, delegate* unmanaged<void*, void> freeFunction);

        [UnmanagedCallersOnly]
        internal static unsafe void* CryptoMalloc(UIntPtr size, char* file, int line)
        {
            void* ptr =  NativeMemory.Alloc(size + (UIntPtr)Offset);
            Debug.Assert(ptr != null);

            if (ptr == null)
            {
                return null;
            }

            Span<MemoryEntry> entry = new Span<MemoryEntry>(ptr, 1);
            entry[0].Line = line;
            entry[0].File = file;
            entry[0].Size = (int)size;

            if (ValidateMemory)
            {
                lock (_allocations!)
                {
                    Debug.Assert(_allocations!.Add((IntPtr)ptr));
                }
            }
            if (_trackIncrementalAllocations)
            {
                lock (_allocationsDiff!)
                {
                    Debug.Assert(_allocationsDiff!.Add((IntPtr)ptr));
                }
            }
            Interlocked.Add(ref TotalAllocatedMemory, (long)size);
            Interlocked.Increment(ref TotalAllocations);

            return (void*)((IntPtr)ptr + Offset);
        }

        [UnmanagedCallersOnly]
        internal static unsafe void* CryptoRealloc(void* oldPtr, UIntPtr size, char* file, int line)
        {
            void * ptr;
            Span<MemoryEntry> entry;

            if (oldPtr != null)
            {
                IntPtr entryPtr = (IntPtr)oldPtr - Offset;
                entry = new Span<MemoryEntry>((void*)entryPtr, 1);

                if (ValidateMemory)
                {
                    lock (_allocations!)
                    {
                        if (!_allocations!.Remove(entryPtr))
                        {
                            Environment.FailFast($"Failed to find OpenSSL memory 0x{(IntPtr)oldPtr:x}");
                        }
                    }
                }

                if (_trackIncrementalAllocations)
                {
                    lock (_allocationsDiff!)
                    {
                        // this may fail as we may start tracking after given chunk was allocated
                        _allocationsDiff!.Remove(entryPtr);
                    }
                }

                Interlocked.Add(ref TotalAllocatedMemory, -((long)entry[0].Size));
                ptr =  NativeMemory.Realloc((void*)entryPtr, size + (UIntPtr)Offset);
            }
            else
            {
                ptr =  NativeMemory.Alloc(size + (UIntPtr)Offset);
            }


            Debug.Assert(ptr != null);
            if (ptr == null)
            {
                return null;
            }

            if (ValidateMemory)
            {
                lock (_allocations!)
                {
                    Debug.Assert(_allocations!.Add((IntPtr)ptr));
                }
            }
            if (_trackIncrementalAllocations)
            {
                lock (_allocationsDiff!)
                {
                    Debug.Assert(_allocationsDiff!.Add((IntPtr)ptr));
                }
            }
            Interlocked.Add(ref TotalAllocatedMemory, (long)size);
            Interlocked.Increment(ref TotalAllocations);

            entry = new Span<MemoryEntry>((void*)ptr, 1);
            entry[0].Line = line;
            entry[0].File = file;
            entry[0].Size = (int)size;

            return (void*)((IntPtr)ptr + Offset);
        }

        [UnmanagedCallersOnly]
        internal static unsafe void CryptoFree(void* ptr)
        {
            if (ptr != null)
            {
                IntPtr entryPtr = (IntPtr)ptr - Offset;
                if (ValidateMemory)
                {
                    lock (_allocations!)
                    {
                        {
                            if (!_allocations!.Remove(entryPtr))
                            {
                                Environment.FailFast($"Failed to find OpenSSL memory 0x{(IntPtr)ptr:x}");
                            }
                        }
                    }
                }
                if (_trackIncrementalAllocations)
                {
                    lock (_allocationsDiff!)
                    {
                        // this may fail as we may start tracking after given chunk was allocated
                        _allocationsDiff!.Remove(entryPtr);
                    }
                }

                Span<MemoryEntry> entry = new Span<MemoryEntry>((void*)entryPtr, 1);
                Interlocked.Add(ref TotalAllocatedMemory, -((long)entry[0].Size));

                NativeMemory.Free((void*)entryPtr);
            }
        }
    }
}
