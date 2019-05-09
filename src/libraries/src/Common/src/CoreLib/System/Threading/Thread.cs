// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Security.Principal;

namespace System.Threading
{
#if PROJECTN
    [Internal.Runtime.CompilerServices.RelocatedType("System.Threading.Thread")]
#endif
    public sealed partial class Thread : CriticalFinalizerObject
    {
        private static AsyncLocal<IPrincipal?>? s_asyncLocalPrincipal;

        [ThreadStatic]
        private static Thread? t_currentThread;

        public Thread(ThreadStart start)
            : this()
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            Create(start);
        }

        public Thread(ThreadStart start, int maxStackSize)
            : this()
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            if (maxStackSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            Create(start, maxStackSize);
        }

        public Thread(ParameterizedThreadStart start)
            : this()
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            Create(start);
        }

        public Thread(ParameterizedThreadStart start, int maxStackSize)
            : this()
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            if (maxStackSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            Create(start, maxStackSize);
        }

        private void RequireCurrentThread()
        {
            if (this != CurrentThread)
            {
                throw new InvalidOperationException(SR.Thread_Operation_RequiresCurrentThread);
            }
        }

        private void SetCultureOnUnstartedThread(CultureInfo value, bool uiCulture)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if ((ThreadState & ThreadState.Unstarted) == 0)
            {
                throw new InvalidOperationException(SR.Thread_Operation_RequiresCurrentThread);
            }
            SetCultureOnUnstartedThreadNoCheck(value, uiCulture);
        }

        partial void ThreadNameChanged(string? value);

        public CultureInfo CurrentCulture
        {
            get
            {
                RequireCurrentThread();
                return CultureInfo.CurrentCulture;
            }
            set
            {
                if (this != CurrentThread)
                {
                    SetCultureOnUnstartedThread(value, uiCulture: false);
                    return;
                }
                CultureInfo.CurrentCulture = value;
            }
        }

        public CultureInfo CurrentUICulture
        {
            get
            {
                RequireCurrentThread();
                return CultureInfo.CurrentUICulture;
            }
            set
            {
                if (this != CurrentThread)
                {
                    SetCultureOnUnstartedThread(value, uiCulture: true);
                    return;
                }
                CultureInfo.CurrentUICulture = value;
            }
        }

        public static IPrincipal? CurrentPrincipal
        {
            get
            {
                if (s_asyncLocalPrincipal is null)
                {
                    CurrentPrincipal = AppDomain.CurrentDomain.GetThreadPrincipal();
                }
                return s_asyncLocalPrincipal?.Value;
            }
            set
            {
                if (s_asyncLocalPrincipal is null)
                {
                    if (value is null)
                    {
                        return;
                    }
                    Interlocked.CompareExchange(ref s_asyncLocalPrincipal, new AsyncLocal<IPrincipal?>(), null);
                }
                s_asyncLocalPrincipal!.Value = value; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
            }
        }

        public static Thread CurrentThread => t_currentThread ?? InitializeCurrentThread();

        public ExecutionContext? ExecutionContext => ExecutionContext.Capture();

        public string? Name
        {
            get => _name;
            set
            {
                lock (this)
                {
                    if (_name != null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_WriteOnce);
                    }

                    _name = value;

                    ThreadNameChanged(value);
                }
            }
        }

        public void Abort()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadAbort);
        }

        public void Abort(object? stateInfo)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadAbort);
        }

        public static void ResetAbort()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadAbort);
        }

        [ObsoleteAttribute("Thread.Suspend has been deprecated.  Please use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.  https://go.microsoft.com/fwlink/?linkid=14202", false)]
        public void Suspend()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadSuspend);
        }

        [ObsoleteAttribute("Thread.Resume has been deprecated.  Please use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.  https://go.microsoft.com/fwlink/?linkid=14202", false)]
        public void Resume()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadSuspend);
        }

        // Currently, no special handling is done for critical regions, and no special handling is necessary to ensure thread
        // affinity. If that changes, the relevant functions would instead need to delegate to RuntimeThread.
        public static void BeginCriticalRegion() { }
        public static void EndCriticalRegion() { }
        public static void BeginThreadAffinity() { }
        public static void EndThreadAffinity() { }

        public static LocalDataStoreSlot AllocateDataSlot() => LocalDataStore.AllocateSlot();
        public static LocalDataStoreSlot AllocateNamedDataSlot(string name) => LocalDataStore.AllocateNamedSlot(name);
        public static LocalDataStoreSlot GetNamedDataSlot(string name) => LocalDataStore.GetNamedSlot(name);
        public static void FreeNamedDataSlot(string name) => LocalDataStore.FreeNamedSlot(name);
        public static object? GetData(LocalDataStoreSlot slot) => LocalDataStore.GetData(slot);
        public static void SetData(LocalDataStoreSlot slot, object? data) => LocalDataStore.SetData(slot, data);

        [Obsolete("The ApartmentState property has been deprecated.  Use GetApartmentState, SetApartmentState or TrySetApartmentState instead.", false)]
        public ApartmentState ApartmentState
        {
            get
            {
                return GetApartmentState();
            }
            set
            {
                TrySetApartmentState(value);
            }
        }

        public void SetApartmentState(ApartmentState state)
        {
            if (!TrySetApartmentState(state))
            {
                throw GetApartmentStateChangeFailedException();
            }
        }

        public bool TrySetApartmentState(ApartmentState state)
        {
            switch (state)
            {
                case ApartmentState.STA:
                case ApartmentState.MTA:
                case ApartmentState.Unknown:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_Enum, nameof(state));
            }

            return TrySetApartmentStateUnchecked(state);
        }

        [Obsolete("Thread.GetCompressedStack is no longer supported. Please use the System.Threading.CompressedStack class")]
        public CompressedStack GetCompressedStack()
        {
            throw new InvalidOperationException(SR.Thread_GetSetCompressedStack_NotSupported);
        }

        [Obsolete("Thread.SetCompressedStack is no longer supported. Please use the System.Threading.CompressedStack class")]
        public void SetCompressedStack(CompressedStack stack)
        {
            throw new InvalidOperationException(SR.Thread_GetSetCompressedStack_NotSupported);
        }

        public static AppDomain GetDomain() => AppDomain.CurrentDomain;
        public static int GetDomainID() => 1;
        public override int GetHashCode() => ManagedThreadId;
        public void Join() => Join(-1);
        public bool Join(TimeSpan timeout) => Join(WaitHandle.ToTimeoutMilliseconds(timeout));
        public static void MemoryBarrier() => Interlocked.MemoryBarrier();
        public static void Sleep(TimeSpan timeout) => Sleep(WaitHandle.ToTimeoutMilliseconds(timeout));

        public static byte VolatileRead(ref byte address) => Volatile.Read(ref address);
        public static double VolatileRead(ref double address) => Volatile.Read(ref address);
        public static short VolatileRead(ref short address) => Volatile.Read(ref address);
        public static int VolatileRead(ref int address) => Volatile.Read(ref address);
        public static long VolatileRead(ref long address) => Volatile.Read(ref address);
        public static IntPtr VolatileRead(ref IntPtr address) => Volatile.Read(ref address);
        public static object? VolatileRead(ref object? address) => Volatile.Read(ref address); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
        [CLSCompliant(false)]
        public static sbyte VolatileRead(ref sbyte address) => Volatile.Read(ref address);
        public static float VolatileRead(ref float address) => Volatile.Read(ref address);
        [CLSCompliant(false)]
        public static ushort VolatileRead(ref ushort address) => Volatile.Read(ref address);
        [CLSCompliant(false)]
        public static uint VolatileRead(ref uint address) => Volatile.Read(ref address);
        [CLSCompliant(false)]
        public static ulong VolatileRead(ref ulong address) => Volatile.Read(ref address);
        [CLSCompliant(false)]
        public static UIntPtr VolatileRead(ref UIntPtr address) => Volatile.Read(ref address);
        public static void VolatileWrite(ref byte address, byte value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref double address, double value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref short address, short value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref int address, int value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref long address, long value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref IntPtr address, IntPtr value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref object? address, object? value) => Volatile.Write(ref address, value);
        [CLSCompliant(false)]
        public static void VolatileWrite(ref sbyte address, sbyte value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref float address, float value) => Volatile.Write(ref address, value);
        [CLSCompliant(false)]
        public static void VolatileWrite(ref ushort address, ushort value) => Volatile.Write(ref address, value);
        [CLSCompliant(false)]
        public static void VolatileWrite(ref uint address, uint value) => Volatile.Write(ref address, value);
        [CLSCompliant(false)]
        public static void VolatileWrite(ref ulong address, ulong value) => Volatile.Write(ref address, value);
        [CLSCompliant(false)]
        public static void VolatileWrite(ref UIntPtr address, UIntPtr value) => Volatile.Write(ref address, value);

        /// <summary>
        /// Manages functionality required to support members of <see cref="Thread"/> dealing with thread-local data
        /// </summary>
        private static class LocalDataStore
        {
            private static Dictionary<string, LocalDataStoreSlot>? s_nameToSlotMap;

            public static LocalDataStoreSlot AllocateSlot()
            {
                return new LocalDataStoreSlot(new ThreadLocal<object?>());
            }

            private static Dictionary<string, LocalDataStoreSlot> EnsureNameToSlotMap()
            {
                Dictionary<string, LocalDataStoreSlot>? nameToSlotMap = s_nameToSlotMap;
                if (nameToSlotMap != null)
                {
                    return nameToSlotMap;
                }

                nameToSlotMap = new Dictionary<string, LocalDataStoreSlot>();
                return Interlocked.CompareExchange(ref s_nameToSlotMap, nameToSlotMap, null) ?? nameToSlotMap;
            }

            public static LocalDataStoreSlot AllocateNamedSlot(string name)
            {
                LocalDataStoreSlot slot = AllocateSlot();
                Dictionary<string, LocalDataStoreSlot> nameToSlotMap = EnsureNameToSlotMap();
                lock (nameToSlotMap)
                {
                    nameToSlotMap.Add(name, slot);
                }
                return slot;
            }

            public static LocalDataStoreSlot GetNamedSlot(string name)
            {
                Dictionary<string, LocalDataStoreSlot> nameToSlotMap = EnsureNameToSlotMap();
                lock (nameToSlotMap)
                {
                    LocalDataStoreSlot slot;
                    if (!nameToSlotMap.TryGetValue(name, out slot))
                    {
                        slot = AllocateSlot();
                        nameToSlotMap[name] = slot;
                    }
                    return slot;
                }
            }

            public static void FreeNamedSlot(string name)
            {
                Dictionary<string, LocalDataStoreSlot> nameToSlotMap = EnsureNameToSlotMap();
                lock (nameToSlotMap)
                {
                    nameToSlotMap.Remove(name);
                }
            }

            private static ThreadLocal<object?> GetThreadLocal(LocalDataStoreSlot slot)
            {
                if (slot == null)
                {
                    throw new ArgumentNullException(nameof(slot));
                }

                Debug.Assert(slot.Data != null);
                return slot.Data;
            }

            public static object? GetData(LocalDataStoreSlot slot)
            {
                return GetThreadLocal(slot).Value;
            }

            public static void SetData(LocalDataStoreSlot slot, object? value)
            {
                GetThreadLocal(slot).Value = value;
            }
        }
    }
}
