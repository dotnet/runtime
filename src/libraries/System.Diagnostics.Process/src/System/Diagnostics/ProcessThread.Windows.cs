// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    /// <summary>Represents an operating system process thread.</summary>
    /// <remarks>Use <see cref="System.Diagnostics.ProcessThread" /> to obtain information about a thread that is currently running on the system. Doing so allows you, for example, to monitor the thread's performance characteristics.
    /// <format type="text/markdown"><![CDATA[
    /// > [!IMPORTANT]
    /// >  This type implements the <xref:System.IDisposable> interface. When you have finished using the type, you should dispose of it either directly or indirectly. To dispose of the type directly, call its <xref:System.IDisposable.Dispose%2A> method in a `try`/`catch` block. To dispose of it indirectly, use a language construct such as `using` (in C#) or `Using` (in Visual Basic). For more information, see the "Using an Object that Implements IDisposable" section in the <xref:System.IDisposable> interface topic.
    /// ]]></format>
    /// A thread is a path of execution through a program. It is the smallest unit of execution that Win32 schedules. It consists of a stack, the state of the CPU registers, and an entry in the execution list of the system scheduler.
    /// A process consists of one or more threads and the code, data, and other resources of a program in memory. Typical program resources are open files, semaphores, and dynamically allocated memory. Each resource of a process is shared by all that process's threads.
    /// A program executes when the system scheduler gives execution control to one of the program's threads. The scheduler determines which threads should run and when. A lower-priority thread might be forced to wait while higher-priority threads complete their tasks. On multiprocessor computers, the scheduler can move individual threads to different processors, thus balancing the CPU load.
    /// Each process starts with a single thread, which is known as the primary thread. Any thread can create additional threads. All the threads within a process share the address space of that process.
    /// The primary thread is not necessarily located at the first index in the collection.
    /// <format type="text/markdown"><![CDATA[
    /// > [!NOTE]
    /// >  Starting with the .NET Framework version 2.0, the ability to reference performance counter data on other computers has been eliminated for many of the .NET Framework methods and properties. This change was made to improve performance and to enable non-administrators to use the <xref:System.Diagnostics.ProcessThread> class. As a result, some applications that did not get exceptions in earlier versions of the .NET Framework may now get a <xref:System.NotSupportedException>. The methods and properties affected are too numerous to list here, but the exception information has been added to the affected member topics.
    /// ]]></format>
    /// The threads of a process execute individually and are unaware of each other unless you make them visible to each other. Threads that share common resources, however, must coordinate their work by using semaphores or another method of interprocess communication.
    /// To get a collection of all the <see cref="System.Diagnostics.ProcessThread" /> objects associated with the current process, get the <see cref="System.Diagnostics.Process.Threads" /> property of the <see cref="System.Diagnostics.Process" /> instance.</remarks>
    /// <altmember cref="System.Diagnostics.Process"/>
    /// <altmember cref="System.Diagnostics.Process.Threads"/>
    public partial class ProcessThread
    {
        /// <summary>Sets the preferred processor for this thread to run on.</summary>
        /// <value>The preferred processor for the thread, used when the system schedules threads, to determine which processor to run the thread on.</value>
        /// <remarks>The <see cref="System.Diagnostics.ProcessThread.IdealProcessor" /> value is zero-based.  In other words, to set the thread affinity for the first processor, set the property to zero.
        /// The system schedules threads on their preferred processors whenever possible.
        /// A process thread can migrate from processor to processor, with each migration reloading the processor cache. Specifying a processor for a thread can improve performance under heavy system loads by reducing the number of times the processor cache is reloaded.</remarks>
        /// <example>The following example demonstrates how to set the <see cref="System.Diagnostics.ProcessThread.IdealProcessor" /> property for an instance of Notepad to the first processor.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[ProcessThreadIdealProcessor#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessThreadIdealProcessor/CS/program.cs#1)]
        /// [!code-vb[ProcessThreadIdealProcessor#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessThreadIdealProcessor/VB/program.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.ComponentModel.Win32Exception">The system could not set the thread to start on the specified processor.</exception>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        public int IdealProcessor
        {
            set
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_SET_INFORMATION))
                {
                    if (Interop.Kernel32.SetThreadIdealProcessor(threadHandle, value) < 0)
                    {
                        throw new Win32Exception();
                    }
                }
            }
        }

        /// <summary>Resets the ideal processor for this thread to indicate that there is no single ideal processor. In other words, so that any processor is ideal.</summary>
        /// <exception cref="System.ComponentModel.Win32Exception">The ideal processor could not be reset.</exception>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        /// <altmember cref="System.Diagnostics.ProcessThread.IdealProcessor"/>
        public void ResetIdealProcessor()
        {
            // MAXIMUM_PROCESSORS == 32 on 32-bit or 64 on 64-bit, and means the thread has no preferred processor
            int MAXIMUM_PROCESSORS = IntPtr.Size == 4 ? 32 : 64;
            IdealProcessor = MAXIMUM_PROCESSORS;
        }

        /// <summary>
        /// Returns or sets whether this thread would like a priority boost if the user interacts
        /// with user interface associated with this thread.
        /// </summary>
        private bool PriorityBoostEnabledCore
        {
            get
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_QUERY_INFORMATION))
                {
                    bool disabled;
                    if (!Interop.Kernel32.GetThreadPriorityBoost(threadHandle, out disabled))
                    {
                        throw new Win32Exception();
                    }
                    return !disabled;
                }
            }
            set
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_SET_INFORMATION))
                {
                    if (!Interop.Kernel32.SetThreadPriorityBoost(threadHandle, !value))
                        throw new Win32Exception();
                }
            }
        }

        /// <summary>
        /// Returns or sets the priority level of the associated thread.  The priority level is
        /// not an absolute level, but instead contributes to the actual thread priority by
        /// considering the priority class of the process.
        /// </summary>
        private ThreadPriorityLevel PriorityLevelCore
        {
            get
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_QUERY_INFORMATION))
                {
                    int value = Interop.Kernel32.GetThreadPriority(threadHandle);
                    if (value == 0x7fffffff)
                    {
                        throw new Win32Exception();
                    }
                    return (ThreadPriorityLevel)value;
                }
            }
            set
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_SET_INFORMATION))
                {
                    if (!Interop.Kernel32.SetThreadPriority(threadHandle, (int)value))
                    {
                        throw new Win32Exception();
                    }
                }
            }
        }

        /// <summary>Sets the processors on which the associated thread can run.</summary>
        /// <value>An <see cref="System.IntPtr" /> that points to a set of bits, each of which represents a processor that the thread can run on.</value>
        /// <remarks>The processor affinity of a thread is the set of processors it has a relationship to. In other words, those it can be scheduled to run on.
        /// <see cref="System.Diagnostics.ProcessThread.ProcessorAffinity" /> represents each processor as a bit. Bit 0 represents processor one, bit 1 represents processor two, and so on. The following table shows a subset of the possible <see cref="System.Diagnostics.ProcessThread.ProcessorAffinity" /> for a four-processor system.
        /// |Property value (in hexadecimal)|Valid processors|
        /// |---------------------------------------|----------------------|
        /// |0x0001|1|
        /// |0x0002|2|
        /// |0x0003|1 or 2|
        /// |0x0004|3|
        /// |0x0005|1 or 3|
        /// |0x0007|1, 2, or 3|
        /// |0x000F|1, 2, 3, or 4|
        /// You can also specify the single, preferred processor for a thread by setting the <see cref="System.Diagnostics.ProcessThread.IdealProcessor" /> property. A process thread can migrate from processor to processor, with each migration reloading the processor cache. Specifying a processor for a thread can improve performance under heavy system loads by reducing the number of times the processor cache is reloaded.</remarks>
        /// <example>The following example shows how to set the <see cref="System.Diagnostics.ProcessThread.ProcessorAffinity" /> property for an instance of Notepad to the first processor.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[ProcessThreadIdealProcessor#1](~/samples/snippets/csharp/VS_Snippets_CLR/ProcessThreadIdealProcessor/CS/program.cs#1)]
        /// [!code-vb[ProcessThreadIdealProcessor#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/ProcessThreadIdealProcessor/VB/program.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.ComponentModel.Win32Exception">The processor affinity could not be set.</exception>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        /// <altmember cref="System.Diagnostics.ProcessThread.IdealProcessor"/>
        [SupportedOSPlatform("windows")]
        public IntPtr ProcessorAffinity
        {
            set
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_SET_INFORMATION | Interop.Kernel32.ThreadOptions.THREAD_QUERY_INFORMATION))
                {
                    if (Interop.Kernel32.SetThreadAffinityMask(threadHandle, value) == IntPtr.Zero)
                    {
                        throw new Win32Exception();
                    }
                }
            }
        }

        /// <summary>Gets the amount of time that the thread has spent running code inside the operating system core.</summary>
        /// <value>A <see cref="System.TimeSpan" /> indicating the amount of time that the thread has spent running code inside the operating system core.</value>
        /// <remarks>Windows uses several different protection mechanisms, and at the root of them all is the distinction between user mode and privileged mode. <see cref="System.Diagnostics.ProcessThread.PrivilegedProcessorTime" /> corresponds to the amount of time that the application has spent running in privileged mode, inside the operating system core. The <see cref="System.Diagnostics.ProcessThread.UserProcessorTime" /> property indicates the amount of time that the application has spent running code in user mode, outside the system core.
        /// User mode restricts the application in two important ways. First, the application cannot directly access the peripherals, but instead must call the operating system core to get or set peripheral data. The operating system can thus ensure that one application does not destroy peripheral data that is needed by another. Second, the application cannot read or change data that the operating system itself maintains. This restriction prevents applications from either inadvertently or intentionally corrupting the core. If the application needs the operating system to perform an operation, it calls one of the system's routines. Many of these transition into privileged mode, perform the operation, and smoothly return to user mode.</remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">The thread time could not be retrieved.</exception>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        /// <altmember cref="System.Diagnostics.ProcessThread.UserProcessorTime"/>
        /// <altmember cref="System.Diagnostics.ProcessThread.TotalProcessorTime"/>
        public TimeSpan PrivilegedProcessorTime
        {
            get { return GetThreadTimes().PrivilegedProcessorTime; }
        }

        /// <summary>Gets the time that the operating system started the thread.</summary>
        /// <value>A <see cref="System.DateTime" /> representing the time that was on the system when the operating system started the thread.</value>
        /// <exception cref="System.ComponentModel.Win32Exception">The thread time could not be retrieved.</exception>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        public DateTime StartTime
        {
            get { return GetThreadTimes().StartTime; }
        }

        /// <summary>Gets the total amount of time that this thread has spent using the processor.</summary>
        /// <value>A <see cref="System.TimeSpan" /> that indicates the amount of time that the thread has had control of the processor.</value>
        /// <remarks>The <see cref="System.Diagnostics.ProcessThread.TotalProcessorTime" /> property indicates the total amount of time that the system has taken the thread out of the wait state and given it priority on any processor. On a multiple processor system, this value would include time spent on each processor, if the thread used more than one processor.
        /// The <see cref="System.Diagnostics.ProcessThread.TotalProcessorTime" /> property is the sum of the <see cref="System.Diagnostics.ProcessThread.UserProcessorTime" /> and <see cref="System.Diagnostics.ProcessThread.PrivilegedProcessorTime" /> properties.</remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">The thread time could not be retrieved.</exception>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        /// <altmember cref="System.Diagnostics.ProcessThread.PrivilegedProcessorTime"/>
        /// <altmember cref="System.Diagnostics.ProcessThread.UserProcessorTime"/>
        public TimeSpan TotalProcessorTime
        {
            get { return GetThreadTimes().TotalProcessorTime; }
        }

        /// <summary>Gets the amount of time that the associated thread has spent running code inside the application.</summary>
        /// <value>A <see cref="System.TimeSpan" /> indicating the amount of time that the thread has spent running code inside the application, as opposed to inside the operating system core.</value>
        /// <remarks>Windows NT uses several different protection mechanisms, and at the root of them all is the distinction between user mode and privileged mode. <see cref="System.Diagnostics.ProcessThread.UserProcessorTime" /> corresponds to the amount of time that the application has spent running in user mode, outside the operating system core. The <see cref="System.Diagnostics.ProcessThread.PrivilegedProcessorTime" /> corresponds to the amount of time that the application has spent running code in privileged mode, inside the system core.
        /// User mode restricts the application in two important ways. First, the application cannot directly access the peripherals, but instead must call the operating system core to get or set peripheral data. The operating system can thus ensure that one application does not destroy peripheral data that is needed by another. Second, the application cannot read or change data that the operating system itself maintains. This restriction prevents applications from either inadvertently or intentionally corrupting the core. If the application needs the operating system to perform an operation, it calls one of the system's routines. Many of these transition into privileged mode, perform the operation, and smoothly return to user mode.</remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">The thread time could not be retrieved.</exception>
        /// <exception cref="System.NotSupportedException">The process is on a remote computer.</exception>
        /// <altmember cref="System.Diagnostics.ProcessThread.PrivilegedProcessorTime"/>
        /// <altmember cref="System.Diagnostics.ProcessThread.TotalProcessorTime"/>
        public TimeSpan UserProcessorTime
        {
            get { return GetThreadTimes().UserProcessorTime; }
        }

        /// <summary>Gets timing information for the thread.</summary>
        private ProcessThreadTimes GetThreadTimes()
        {
            using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_QUERY_INFORMATION))
            {
                var threadTimes = new ProcessThreadTimes();
                if (!Interop.Kernel32.GetThreadTimes(threadHandle,
                    out threadTimes._create, out threadTimes._exit,
                    out threadTimes._kernel, out threadTimes._user))
                {
                    throw new Win32Exception();
                }
                return threadTimes;
            }
        }

        /// <summary>Open a handle to the thread.</summary>
        private SafeThreadHandle OpenThreadHandle(int access)
        {
            EnsureState(State.IsLocal);
            return ProcessManager.OpenThread((int)_threadInfo._threadId, access);
        }
    }
}
