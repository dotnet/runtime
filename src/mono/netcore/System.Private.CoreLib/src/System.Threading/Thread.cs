// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
	//
	// Under netcore, there is only one thread object per thread
	//
	[StructLayout (LayoutKind.Sequential)]
	partial class Thread
	{
#pragma warning disable 169, 414, 649
		#region Sync with metadata/object-internals.h and InternalThread in mcs/class/corlib/System.Threading/Thread.cs
		int lock_thread_id;
		// stores a thread handle
		IntPtr handle;
		IntPtr native_handle; // used only on Win32
		/* accessed only from unmanaged code */
		private IntPtr name;
		private IntPtr name_generation;
		private int name_free;
		private int name_length;
		private ThreadState state;
		private object abort_exc;
		private int abort_state_handle;
		/* thread_id is only accessed from unmanaged code */
		internal Int64 thread_id;
		private IntPtr debugger_thread; // FIXME switch to bool as soon as CI testing with corlib version bump works
		private UIntPtr static_data; /* GC-tracked */
		private IntPtr runtime_thread_info;
		/* current System.Runtime.Remoting.Contexts.Context instance
		   keep as an object to avoid triggering its class constructor when not needed */
		private object current_appcontext;
		private object root_domain_thread;
		internal byte[] _serialized_principal;
		internal int _serialized_principal_version;
		private IntPtr appdomain_refs;
		private int interruption_requested;
		private IntPtr longlived;
		internal bool threadpool_thread;
		private bool thread_interrupt_requested;
		/* These are used from managed code */
		internal int stack_size;
		internal byte apartment_state;
		internal volatile int critical_region_level;
		internal int managed_id;
		private int small_id;
		private IntPtr manage_callback;
		private IntPtr flags;
		private IntPtr thread_pinning_ref;
		private IntPtr abort_protected_block_count;
		private int priority;
		private IntPtr owned_mutex;
		private IntPtr suspended_event;
		private int self_suspended;
		private IntPtr thread_state;

		private Thread self;
		private object pending_exception;
		private object start_obj;

		/* This is used only to check that we are in sync between the representation
		 * of MonoInternalThread in native and InternalThread in managed
		 *
		 * DO NOT RENAME! DO NOT ADD FIELDS AFTER! */
		private IntPtr last;
		#endregion
#pragma warning restore 169, 414, 649

		string _name;
		Delegate m_start;
		object m_start_arg;
		CultureInfo culture, ui_culture;
		internal ExecutionContext _executionContext;
		internal SynchronizationContext _synchronizationContext;

		Thread ()
		{
			InitInternal (this);
		}

		~Thread ()
		{
			FreeInternal ();
		}

		internal static ulong CurrentOSThreadId {
			get {
				return GetCurrentOSThreadId ();
			}
		}

		public bool IsAlive {
			get {
				var state = GetState (this);
				return (state & (ThreadState.Unstarted | ThreadState.Stopped | ThreadState.Aborted)) == 0;
			}
		}

		public bool IsBackground {
			get {
				var state = ValidateThreadState ();
				return (state & ThreadState.Background) != 0;
			}
			set {
				ValidateThreadState ();
				if (value) {
					SetState (this, ThreadState.Background);
				} else {
					ClrState (this, ThreadState.Background);
				}
			}
		}

		public bool IsThreadPoolThread {
			get {
				ValidateThreadState ();
				return threadpool_thread;
			}
		}

		public int ManagedThreadId => managed_id;

		internal static int OptimalMaxSpinWaitsPerSpinIteration {
			get {
				// Default from coreclr (src/utilcode/yieldprocessornormalized.cpp)
				return 7;
			}
		}

		public ThreadPriority Priority {
			get {
				ValidateThreadState ();
				return (ThreadPriority) priority;
			 }
			set {
				// TODO: arguments check
				SetPriority (this, (int) value);
			}
		}

		internal SynchronizationContext SynchronizationContext { get; set; }

		public ThreadState ThreadState => GetState (this);

		void Create (ThreadStart start) => SetStartHelper ((Delegate) start, 0); // 0 will setup Thread with default stackSize

		void Create (ThreadStart start, int maxStackSize) => SetStartHelper ((Delegate) start, maxStackSize);

		void Create (ParameterizedThreadStart start) => SetStartHelper ((Delegate) start, 0);

		void Create (ParameterizedThreadStart start, int maxStackSize) => SetStartHelper ((Delegate) start, maxStackSize);

		public ApartmentState GetApartmentState () => ApartmentState.Unknown;

		public void DisableComObjectEagerCleanup ()
		{
			// no-op
		}

		public static int GetCurrentProcessorId ()
		{
			// TODO: Implement correctly
			return Environment.CurrentManagedThreadId;
		}

		public void Interrupt ()
		{
			InterruptInternal (this);
		}

		public bool Join (int millisecondsTimeout)
		{
			if (millisecondsTimeout < Timeout.Infinite)
				throw new ArgumentOutOfRangeException (nameof (millisecondsTimeout), millisecondsTimeout, SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
			return JoinInternal (this, millisecondsTimeout);
		}

		internal void ResetThreadPoolThread ()
		{
		}

		void SetCultureOnUnstartedThreadNoCheck (CultureInfo value, bool uiCulture)
		{
			if (uiCulture)
				ui_culture = value;
			else
				culture = value;
		}

		void SetStartHelper (Delegate start, int maxStackSize)
		{
			m_start = start;
			stack_size = maxStackSize;
		}

		public static void SpinWait (int iterations)
		{
			if (iterations < 0)
				return;

			while (iterations-- > 0)
				SpinWait_nop ();
		}

		public static void Sleep (int millisecondsTimeout)
		{
			if (millisecondsTimeout < Timeout.Infinite)
				throw new ArgumentOutOfRangeException (nameof (millisecondsTimeout), millisecondsTimeout, SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

			SleepInternal (millisecondsTimeout);
		}

		public void Start ()
		{
			StartInternal (this);
		}

		public void Start (object parameter)
		{
			if (m_start is ThreadStart)
				throw new InvalidOperationException (SR.InvalidOperation_ThreadWrongThreadStart);

			m_start_arg = parameter;
			StartInternal (this);
		}

		// Called from the runtime
		internal void StartCallback ()
		{
			if (culture != null)
				CurrentCulture = culture;
			if (ui_culture != null)
				CurrentUICulture = ui_culture;
			if (m_start is ThreadStart del) {
				m_start = null;
				del ();
			} else {
				var pdel = (ParameterizedThreadStart) m_start;
				var arg = m_start_arg;
				m_start = null;
				m_start_arg = null;
				pdel (arg);
			}
		}

		partial void ThreadNameChanged (string value)
		{
			// TODO: Should only raise the events
			SetName (this, value);
		}

		public static bool Yield ()
		{
			return YieldInternal ();
		}

		bool TrySetApartmentStateUnchecked (ApartmentState state) => state == ApartmentState.Unknown;

		ThreadState ValidateThreadState ()
		{
			var state = GetState (this);
			if ((state & ThreadState.Stopped) != 0)
				throw new ThreadStateException ("Thread is dead; state can not be accessed.");
			return state;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private extern static ulong GetCurrentOSThreadId ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void InitInternal (Thread thread);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static Thread InitializeCurrentThread ();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern void FreeInternal ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static ThreadState GetState (Thread thread);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void SetState (Thread thread, ThreadState set);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void ClrState (Thread thread, ThreadState clr);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static string GetName (Thread thread);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		private static unsafe extern void SetName_icall (Thread thread, char *name, int nameLength);

		static unsafe void SetName (Thread thread, String name)
		{
			fixed (char* fixed_name = name)
				SetName_icall (thread, fixed_name, name?.Length ?? 0);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static bool YieldInternal ();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static void SleepInternal (int millisecondsTimeout);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void SpinWait_nop ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static Thread CreateInternal ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void StartInternal (Thread runtime_thread);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static bool JoinInternal (Thread thread, int millisecondsTimeout);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static void InterruptInternal (Thread thread);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static void SetPriority (Thread thread, int priority);
	}
}
