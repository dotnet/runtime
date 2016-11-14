// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETNATIVE
extern alias System_Runtime_Extensions;
extern alias System_Security_Principal;
#endif

using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using Internal.Runtime.Augments;

namespace System.Threading
{
#if !NETNATIVE
    using AppDomain = System_Runtime_Extensions::System.AppDomain;
    using IPrincipal = System_Security_Principal::System.Security.Principal.IPrincipal;
#endif

#if !NETNATIVE
    public sealed partial class Thread : CriticalFinalizerObject
#else
    public sealed partial class Thread
#endif
    {
        [ThreadStatic]
        private static Thread t_currentThread;

        private readonly RuntimeThread _runtimeThread;
        private Delegate _start;
#if !NETNATIVE
        private IPrincipal _principal;
#endif

        private Thread(RuntimeThread runtimeThread)
        {
            Debug.Assert(runtimeThread != null);
            _runtimeThread = runtimeThread;
        }

        public Thread(ThreadStart start)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            _runtimeThread = RuntimeThread.Create(ThreadMain_ThreadStart);
            Debug.Assert(_runtimeThread != null);
            _start = start;
        }

        public Thread(ThreadStart start, int maxStackSize)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            if (maxStackSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), SR.ArgumentOutOfRange_NeedNonnegativeNumber);
            }

            _runtimeThread = RuntimeThread.Create(ThreadMain_ThreadStart, maxStackSize);
            Debug.Assert(_runtimeThread != null);
            _start = start;
        }

        public Thread(ParameterizedThreadStart start)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            _runtimeThread = RuntimeThread.Create(ThreadMain_ParameterizedThreadStart);
            Debug.Assert(_runtimeThread != null);
            _start = start;
        }

        public Thread(ParameterizedThreadStart start, int maxStackSize)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            if (maxStackSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), SR.ArgumentOutOfRange_NeedNonnegativeNumber);
            }

            _runtimeThread = RuntimeThread.Create(ThreadMain_ParameterizedThreadStart, maxStackSize);
            Debug.Assert(_runtimeThread != null);
            _start = start;
        }

        private void ThreadMain_ThreadStart()
        {
            t_currentThread = this;

            Delegate start = _start;
            _start = null;
            Debug.Assert(start is ThreadStart);
            ((ThreadStart)start)();
        }

        private void ThreadMain_ParameterizedThreadStart(object parameter)
        {
            t_currentThread = this;

            Delegate start = _start;
            _start = null;
            Debug.Assert(start is ParameterizedThreadStart);
            ((ParameterizedThreadStart)start)(parameter);
        }

        public static Thread CurrentThread
        {
            get
            {
                Thread currentThread = t_currentThread;
                if (currentThread == null)
                {
                    t_currentThread = currentThread = new Thread(RuntimeThread.CurrentThread);
                }
                return currentThread;
            }
        }

        private void RequireCurrentThread()
        {
            if (this != CurrentThread)
            {
                throw new InvalidOperationException(SR.Thread_Operation_RequiresCurrentThread);
            }
        }

        public CultureInfo CurrentCulture
        {
            get
            {
                RequireCurrentThread();
                return CultureInfo.CurrentCulture;
            }
            set
            {
                RequireCurrentThread();
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
                RequireCurrentThread();
                CultureInfo.CurrentUICulture = value;
            }
        }

#if !NETNATIVE
        public static IPrincipal CurrentPrincipal
        {
            get
            {
                return CurrentThread._principal;
            }
            set
            {
                CurrentThread._principal = value;
            }
        }
#endif

        public ExecutionContext ExecutionContext => ExecutionContext.Capture();
        public bool IsAlive => _runtimeThread.IsAlive;
        public bool IsBackground { get { return _runtimeThread.IsBackground; } set { _runtimeThread.IsBackground = value; } }
        public bool IsThreadPoolThread => _runtimeThread.IsThreadPoolThread;
        public int ManagedThreadId => _runtimeThread.ManagedThreadId;
        public string Name { get { return _runtimeThread.Name; } set { _runtimeThread.Name = value; } }
        public ThreadPriority Priority { get { return _runtimeThread.Priority; } set { _runtimeThread.Priority = value; } }
        public ThreadState ThreadState => _runtimeThread.ThreadState;

        public void Abort()
        {
            throw new PlatformNotSupportedException();
        }

        public void Abort(object stateInfo)
        {
            throw new PlatformNotSupportedException();
        }

        public static void ResetAbort()
        {
            throw new PlatformNotSupportedException();
        }

        public void Suspend()
        {
            throw new PlatformNotSupportedException();
        }

        public void Resume()
        {
            throw new PlatformNotSupportedException();
        }

        // Currently, no special handling is done for critical regions, and no special handling is necessary to ensure thread
        // affinity. If that changes, the relevant functions would instead need to delegate to RuntimeThread.
        public static void BeginCriticalRegion() { }
        public static void EndCriticalRegion() { }
        public static void BeginThreadAffinity() { }
        public static void EndThreadAffinity() { }

#if !NETNATIVE
        public static LocalDataStoreSlot AllocateDataSlot() => LocalDataStore.AllocateSlot();
        public static LocalDataStoreSlot AllocateNamedDataSlot(string name) => LocalDataStore.AllocateNamedSlot(name);
        public static LocalDataStoreSlot GetNamedDataSlot(string name) => LocalDataStore.GetNamedSlot(name);
        public static void FreeNamedDataSlot(string name) => LocalDataStore.FreeNamedSlot(name);
        public static object GetData(LocalDataStoreSlot slot) => LocalDataStore.GetData(slot);
        public static void SetData(LocalDataStoreSlot slot, object data) => LocalDataStore.SetData(slot, data);
#endif

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

        private static int ToTimeoutMilliseconds(TimeSpan timeout)
        {
            var timeoutMilliseconds = (long)timeout.TotalMilliseconds;
            if (timeoutMilliseconds < -1 || timeoutMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_TimeoutMilliseconds);
            }
            return (int)timeoutMilliseconds;
        }

#if !NETNATIVE
        public static AppDomain GetDomain() => AppDomain.CurrentDomain;
        public static int GetDomainID() => GetDomain().Id;
#endif
        public override int GetHashCode() => ManagedThreadId;
        public void Interrupt() => _runtimeThread.Interrupt();
        public void Join() => _runtimeThread.Join();
        public bool Join(int millisecondsTimeout) => _runtimeThread.Join(millisecondsTimeout);
        public bool Join(TimeSpan timeout) => Join(ToTimeoutMilliseconds(timeout));
        public static void MemoryBarrier() => Interlocked.MemoryBarrier();
        public static void Sleep(int millisecondsTimeout) => RuntimeThread.Sleep(millisecondsTimeout);
        public static void Sleep(TimeSpan timeout) => Sleep(ToTimeoutMilliseconds(timeout));
        public static void SpinWait(int iterations) => RuntimeThread.SpinWait(iterations);
        public static bool Yield() => RuntimeThread.Yield();
        public void Start() => _runtimeThread.Start();
        public void Start(object parameter) => _runtimeThread.Start(parameter);

        public static byte VolatileRead(ref byte address) => Volatile.Read(ref address);
        public static double VolatileRead(ref double address) => Volatile.Read(ref address);
        public static short VolatileRead(ref short address) => Volatile.Read(ref address);
        public static int VolatileRead(ref int address) => Volatile.Read(ref address);
        public static long VolatileRead(ref long address) => Volatile.Read(ref address);
        public static IntPtr VolatileRead(ref IntPtr address) => Volatile.Read(ref address);
        public static object VolatileRead(ref object address) => Volatile.Read(ref address);
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
        public static void VolatileWrite(ref object address, object value) => Volatile.Write(ref address, value);
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

#if !NETNATIVE
        /// <summary>
        /// Manages functionality required to support members of <see cref="Thread"/> dealing with thread-local data
        /// </summary>
        private static class LocalDataStore
        {
            private static Dictionary<string, LocalDataStoreSlot> s_nameToSlotMap;

            public static LocalDataStoreSlot AllocateSlot()
            {
                return new LocalDataStoreSlot(new ThreadLocal<object>());
            }

            public static Dictionary<string, LocalDataStoreSlot> EnsureNameToSlotMap()
            {
                Dictionary<string, LocalDataStoreSlot> nameToSlotMap = s_nameToSlotMap;
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

            private static ThreadLocal<object> GetThreadLocal(LocalDataStoreSlot slot)
            {
                if (slot == null)
                {
                    throw new ArgumentNullException(nameof(slot));
                }

                Debug.Assert(slot.Data != null);
                return slot.Data;
            }

            public static object GetData(LocalDataStoreSlot slot)
            {
                return GetThreadLocal(slot).Value;
            }

            public static void SetData(LocalDataStoreSlot slot, object value)
            {
                GetThreadLocal(slot).Value = value;
            }
        }
#endif
    }
}
