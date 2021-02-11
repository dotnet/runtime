// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    /// <summary>Represents an operating system process thread.</summary>
    /// <remarks>Use <see cref="System.Diagnostics.ProcessThread" /> to obtain information about a thread that is currently running on the system. Doing so allows you, for example, to monitor the thread's performance characteristics.
    /// > [!IMPORTANT]
    /// >  This type implements the <see cref="System.IDisposable" /> interface. When you have finished using the type, you should dispose of it either directly or indirectly. To dispose of the type directly, call its <see cref="System.IDisposable.Dispose" /> method in a `try`/`catch` block. To dispose of it indirectly, use a language construct such as `using` (in C#) or `Using` (in Visual Basic). For more information, see the "Using an Object that Implements IDisposable" section in the <see cref="System.IDisposable" /> interface topic.
    /// A thread is a path of execution through a program. It is the smallest unit of execution that Win32 schedules. It consists of a stack, the state of the CPU registers, and an entry in the execution list of the system scheduler.
    /// A process consists of one or more threads and the code, data, and other resources of a program in memory. Typical program resources are open files, semaphores, and dynamically allocated memory. Each resource of a process is shared by all that process's threads.
    /// A program executes when the system scheduler gives execution control to one of the program's threads. The scheduler determines which threads should run and when. A lower-priority thread might be forced to wait while higher-priority threads complete their tasks. On multiprocessor computers, the scheduler can move individual threads to different processors, thus balancing the CPU load.
    /// Each process starts with a single thread, which is known as the primary thread. Any thread can create additional threads. All the threads within a process share the address space of that process.
    /// The primary thread is not necessarily located at the first index in the collection.
    /// > [!NOTE]
    /// >  Starting with the .NET Framework version 2.0, the ability to reference performance counter data on other computers has been eliminated for many of the .NET Framework methods and properties. This change was made to improve performance and to enable non-administrators to use the <see cref="System.Diagnostics.ProcessThread" /> class. As a result, some applications that did not get exceptions in earlier versions of the .NET Framework may now get a <see cref="System.NotSupportedException" />. The methods and properties affected are too numerous to list here, but the exception information has been added to the affected member topics.
    /// The threads of a process execute individually and are unaware of each other unless you make them visible to each other. Threads that share common resources, however, must coordinate their work by using semaphores or another method of interprocess communication.
    /// To get a collection of all the <see cref="System.Diagnostics.ProcessThread" /> objects associated with the current process, get the <see cref="System.Diagnostics.Process.Threads" /> property of the <see cref="System.Diagnostics.Process" /> instance.</remarks>
    /// <altmember cref="System.Diagnostics.Process"/>
    /// <altmember cref="System.Diagnostics.Process.Threads"/>
    [Designer("System.Diagnostics.Design.ProcessThreadDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class ProcessThread : Component
    {
        private readonly bool _isRemoteMachine;
        private readonly int _processId;
        private readonly ThreadInfo _threadInfo;
        private bool? _priorityBoostEnabled;
        private ThreadPriorityLevel? _priorityLevel;

        /// <devdoc>
        ///     Internal constructor.
        /// </devdoc>
        /// <internalonly/>
        internal ProcessThread(bool isRemoteMachine, int processId, ThreadInfo threadInfo)
        {
            _isRemoteMachine = isRemoteMachine;
            _processId = processId;
            _threadInfo = threadInfo;
        }

        /// <summary>Gets the base priority of the thread.</summary>
        /// <value>The base priority of the thread, which the operating system computes by combining the process priority class with the priority level of the associated thread.</value>
        /// <remarks>The <see cref="System.Diagnostics.ProcessThread.BasePriority" /> is the starting priority for the process thread. You can view information about the base priority through the System Monitor's Priority Base counter.
        /// The operating system computes a thread's base priority by combining the thread's priority level range with the process's priority class. You can set the process's <see cref="System.Diagnostics.Process.PriorityClass" /> property to one of the values in the <see cref="System.Diagnostics.ProcessPriorityClass" /> enumeration, which are <see cref="System.Diagnostics.ProcessPriorityClass.Idle" />, <see cref="System.Diagnostics.ProcessPriorityClass.Normal" />, <see cref="System.Diagnostics.ProcessPriorityClass.High" />, <see cref="System.Diagnostics.ProcessPriorityClass.AboveNormal" />, <see cref="System.Diagnostics.ProcessPriorityClass.BelowNormal" />, or <see cref="System.Diagnostics.ProcessPriorityClass.RealTime" />. You can set the thread's <see cref="System.Diagnostics.ProcessThread.PriorityLevel" /> property to a range of values that bounds the thread's base priority. Win32 uses four priority classes with seven base priority levels per class.
        /// The thread's current priority might deviate from the base priority. For example, the operating system can change the <see cref="System.Diagnostics.ProcessThread.CurrentPriority" /> property based on the time elapsed or other boosts when a process must be put ahead of others for access to the processor. In addition, you can set the <see cref="System.Diagnostics.Process.PriorityBoostEnabled" /> property to cause the system to temporarily boost the priority of a thread whenever the process is taken out of the wait state. The priority is reset when the process returns to the wait state.</remarks>
        /// <altmember cref="System.Diagnostics.ProcessThread.PriorityBoostEnabled"/>
        /// <altmember cref="System.Diagnostics.ProcessPriorityClass"/>
        /// <altmember cref="System.Diagnostics.Process.PriorityClass"/>
        /// <altmember cref="System.Diagnostics.ProcessThread.CurrentPriority"/>
        public int BasePriority
        {
            get { return _threadInfo._basePriority; }
        }

        /// <summary>Gets the current priority of the thread.</summary>
        /// <value>The current priority of the thread, which may deviate from the base priority based on how the operating system is scheduling the thread. The priority may be temporarily boosted for an active thread.</value>
        /// <remarks>The thread's current priority might deviate from the base priority. For example, the operating system can change the <see cref="System.Diagnostics.ProcessThread.CurrentPriority" /> property based on the time elapsed, or other boosts, when a process must be put ahead of others for access to the processor. In addition, you can set the <see cref="System.Diagnostics.Process.PriorityBoostEnabled" /> property to cause the system to temporarily boost the priority of a thread whenever the process is taken out of the wait state. The priority is reset when the process returns to the wait state.</remarks>
        /// <altmember cref="System.Diagnostics.ProcessThread.BasePriority"/>
        /// <altmember cref="System.Diagnostics.ProcessThread.PriorityBoostEnabled"/>
        public int CurrentPriority
        {
            get { return _threadInfo._currentPriority; }
        }

        /// <summary>Gets the unique identifier of the thread.</summary>
        /// <value>The unique identifier associated with a specific thread.</value>
        /// <remarks>The operating system reuses thread identification numbers, which identify threads only during their lifetimes.</remarks>
        public int Id
        {
            get { return unchecked((int)_threadInfo._threadId); }
        }

        /// <summary>Gets or sets a value indicating whether the operating system should temporarily boost the priority of the associated thread whenever the main window of the thread's process receives the focus.</summary>
        /// <value><see langword="true" /> to boost the thread's priority when the user interacts with the process's interface; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        /// <remarks>When <see cref="System.Diagnostics.ProcessThread.PriorityBoostEnabled" /> is <see langword="true" />, the system temporarily boosts the thread's priority whenever its associated process is taken out of the wait state. This action prevents other processes from interrupting the processing of the current thread. The <see cref="System.Diagnostics.ProcessThread.PriorityBoostEnabled" /> setting affects all existing threads as well as any threads subsequently created by the process. To restore normal behavior, set the <see cref="System.Diagnostics.Process.PriorityBoostEnabled" /> property to <see langword="false" />.
        /// <see cref="System.Diagnostics.ProcessThread.PriorityBoostEnabled" /> has an effect only when the thread is running in a process that has a <see cref="System.Diagnostics.Process.PriorityClass" /> set to one of the dynamic priority enumeration values (<see cref="System.Diagnostics.ProcessPriorityClass.Normal" />, <see cref="System.Diagnostics.ProcessPriorityClass.High" />, or <see cref="System.Diagnostics.ProcessPriorityClass.RealTime" />).
        /// > [!NOTE]
        /// >  Boosting the priority too high can drain resources from essential operating system and network functions. This could cause problems with other operating system tasks.</remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">The priority boost information could not be retrieved.
        /// -or-
        /// The priority boost information could not be set.</exception>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        /// <altmember cref="System.Diagnostics.ProcessPriorityClass"/>
        public bool PriorityBoostEnabled
        {
            get
            {
                if (!_priorityBoostEnabled.HasValue)
                {
                    _priorityBoostEnabled = PriorityBoostEnabledCore;
                }
                return _priorityBoostEnabled.Value;
            }
            set
            {
                PriorityBoostEnabledCore = value;
                _priorityBoostEnabled = value;
            }
        }

        /// <summary>Gets or sets the priority level of the thread.</summary>
        /// <value>One of the <see cref="System.Diagnostics.ThreadPriorityLevel" /> values, specifying a range that bounds the thread's priority.</value>
        /// <remarks>The priority level is not a single value, but rather a range of values. The operating system computes the thread's base priority by using the process's <see cref="System.Diagnostics.Process.PriorityClass" /> to choose a value from the range specified in the <see cref="System.Diagnostics.ProcessThread.PriorityLevel" /> property.</remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">The thread priority level information could not be retrieved.
        /// -or-
        /// The thread priority level could not be set.</exception>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        /// <altmember cref="System.Diagnostics.ThreadPriorityLevel"/>
        /// <altmember cref="System.Diagnostics.Process.PriorityClass"/>
        /// <altmember cref="System.Diagnostics.ProcessThread.BasePriority"/>
        /// <altmember cref="System.Diagnostics.ProcessThread.CurrentPriority"/>
        public ThreadPriorityLevel PriorityLevel
        {
            get
            {
                if (!_priorityLevel.HasValue)
                {
                    _priorityLevel = PriorityLevelCore;
                }
                return _priorityLevel.Value;
            }
            [SupportedOSPlatform("windows")]
            set
            {
                PriorityLevelCore = value;
                _priorityLevel = value;
            }
        }

        /// <summary>Gets the memory address of the function that the operating system called that started this thread.</summary>
        /// <value>The thread's starting address, which points to the application-defined function that the thread executes.</value>
        /// <remarks>Each process starts with a single thread, which is known as the primary thread. Any thread can create additional threads.
        /// A process has a virtual address space, executable code, data, object handles, environment variables, a base priority, and minimum and maximum working set sizes. All the threads of a process share its virtual address space and system resources. In addition, each thread maintains exception handlers, a scheduling priority, and a set of structures in which the system saves the thread context while the thread is waiting to be scheduled. The thread context includes the thread's set of machine registers, the kernel stack, a thread environment block, and a user stack in the address space of the thread's process.
        /// Every Windows thread actually begins execution in a system-supplied function, not the application-supplied function. The starting address for the primary thread is, therefore, the same (as it represents the address of the system-supplied function) for every Windows process in the system. However, the <see cref="System.Diagnostics.ProcessThread.StartAddress" /> property allows you to get the starting function address that is specific to your application.</remarks>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        public IntPtr StartAddress
        {
            get { return _threadInfo._startAddress; }
        }

        /// <summary>Gets the current state of this thread.</summary>
        /// <value>A <see cref="System.Diagnostics.ThreadState" /> that indicates the thread's execution, for example, running, waiting, or terminated.</value>
        /// <remarks>The <see cref="System.Diagnostics.ProcessThread.WaitReason" /> property value is valid only when the <see cref="System.Diagnostics.ProcessThread.ThreadState" /> value is <see cref="System.Diagnostics.ThreadState.Wait" />. Therefore, check the <see cref="System.Diagnostics.ProcessThread.ThreadState" /> value before you get the <see cref="System.Diagnostics.ProcessThread.WaitReason" /> property.</remarks>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        public ThreadState ThreadState
        {
            get { return _threadInfo._threadState; }
        }

        /// <summary>Gets the reason that the thread is waiting.</summary>
        /// <value>A <see cref="System.Diagnostics.ThreadWaitReason" /> representing the reason that the thread is in the wait state.</value>
        /// <remarks>The <see cref="System.Diagnostics.ProcessThread.WaitReason" /> property is valid only when the <see cref="System.Diagnostics.ProcessThread.ThreadState" /> is <see cref="System.Diagnostics.ThreadState.Wait" />. Therefore, check the <see cref="System.Diagnostics.ProcessThread.ThreadState" /> value before you get the <see cref="System.Diagnostics.ProcessThread.WaitReason" /> property.</remarks>
        /// <exception cref="System.InvalidOperationException">The thread is not in the wait state.</exception>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        /// <altmember cref="System.Diagnostics.ThreadWaitReason"/>
        /// <altmember cref="System.Diagnostics.ProcessThread.ThreadState"/>
        public ThreadWaitReason WaitReason
        {
            get
            {
                if (_threadInfo._threadState != ThreadState.Wait)
                {
                    throw new InvalidOperationException(SR.WaitReasonUnavailable);
                }
                return _threadInfo._threadWaitReason;
            }
        }

        /// <devdoc>
        ///     Helper to check preconditions for property access.
        /// </devdoc>
        private void EnsureState(State state)
        {
            if (((state & State.IsLocal) != (State)0) && _isRemoteMachine)
            {
                throw new NotSupportedException(SR.NotSupportedRemoteThread);
            }
        }

        /// <summary>
        ///      Preconditions for accessing properties.
        /// </summary>
        /// <internalonly/>
        private enum State
        {
            IsLocal = 0x2
        }
    }
}
