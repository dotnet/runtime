// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>Provides access to local and remote processes and enables you to start and stop local system processes.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// A <xref:System.Diagnostics.Process> component provides access to a process that is running on a computer. A process, in the simplest terms, is a running app. A thread is the basic unit to which the operating system allocates processor time. A thread can execute any part of the code of the process, including parts currently being executed by another thread.
    /// The <xref:System.Diagnostics.Process> component is a useful tool for starting, stopping, controlling, and monitoring apps. You can use the <xref:System.Diagnostics.Process> component, to obtain a list of the processes that are running, or you can start a new process. A <xref:System.Diagnostics.Process> component is used to access system processes. After a <xref:System.Diagnostics.Process> component has been initialized, it can be used to obtain information about the running process. Such information includes the set of threads, the loaded modules (.dll and .exe files), and performance information such as the amount of memory the process is using.
    /// This type implements the <xref:System.IDisposable> interface. When you have finished using the type, you should dispose of it either directly or indirectly. To dispose of the type directly, call its <xref:System.IDisposable.Dispose%2A> method in a `try`/`finally` block. To dispose of it indirectly, use a language construct such as `using` (in C#) or `Using` (in Visual Basic). For more information, see the "Using an Object that Implements IDisposable" section in the <xref:System.IDisposable> interface topic.
    /// > [!NOTE]
    /// >  32-bit processes cannot access the modules of a 64-bit process. If you try to get information about a 64-bit process from a 32-bit process, you will get a <xref:System.ComponentModel.Win32Exception> exception. A 64-bit process, on the other hand, can access the modules of a 32-bit process.
    /// The process component obtains information about a group of properties all at once. After the <xref:System.Diagnostics.Process> component has obtained information about one member of any group, it will cache the values for the other properties in that group and not obtain new information about the other members of the group until you call the <xref:System.Diagnostics.Process.Refresh%2A> method. Therefore, a property value is not guaranteed to be any newer than the last call to the <xref:System.Diagnostics.Process.Refresh%2A> method. The group breakdowns are operating-system dependent.
    /// If you have a path variable declared in your system using quotes, you must fully qualify that path when starting any process found in that location. Otherwise, the system will not find the path. For example, if `c:\mypath` is not in your path, and you add it using quotation marks: `path = %path%;"c:\mypath"`, you must fully qualify any process in `c:\mypath` when starting it.
    /// A system process is uniquely identified on the system by its process identifier. Like many Windows resources, a process is also identified by its handle, which might not be unique on the computer. A handle is the generic term for an identifier of a resource. The operating system persists the process handle, which is accessed through the <xref:System.Diagnostics.Process.Handle%2A> property of the <xref:System.Diagnostics.Process> component, even when the process has exited. Thus, you can get the process's administrative information, such as the <xref:System.Diagnostics.Process.ExitCode%2A> (usually either zero for success or a nonzero error code) and the <xref:System.Diagnostics.Process.ExitTime%2A>. Handles are an extremely valuable resource, so leaking handles is more virulent than leaking memory.
    /// > [!NOTE]
    /// >  This class contains a link demand and an inheritance demand at the class level that applies to all members. A <xref:System.Security.SecurityException> is thrown when either the immediate caller or the derived class does not have full-trust permission. For details about security demands, see [Link Demands](/dotnet/framework/misc/link-demands).
    /// <a name="Core"></a>
    /// ## [!INCLUDE[net_core](~/includes/net-core-md.md)] Notes
    /// In the .NET Framework, the <xref:System.Diagnostics.Process> class by default uses <xref:System.Console> encodings, which are typically code page encodings, for the input, output, and error streams. For example code, on systems whose culture is English (United States), code page 437 is the default encoding for the <xref:System.Console> class. However, [!INCLUDE[net_core](~/includes/net-core-md.md)] may make only a limited subset of these encodings available. If this is the case, it uses <xref:System.Text.Encoding.UTF8%2A?displayProperty=nameWithType> as the default encoding.
    /// If a <xref:System.Diagnostics.Process> object depends on specific code page encodings, you can still make them available by doing the following *before* you call any <xref:System.Diagnostics.Process> methods:
    /// 1.  Add a reference to the System.Text.Encoding.CodePages.dll assembly to your project.
    /// 2.  Retrieve the <xref:System.Text.EncodingProvider> object from the <xref:System.Text.CodePagesEncodingProvider.Instance%2A?displayProperty=nameWithType> property.
    /// 3.  Pass the <xref:System.Text.EncodingProvider> object to the <xref:System.Text.Encoding.RegisterProvider%2A?displayProperty=nameWithType> method to make the additional encodings supported by the encoding provider available.
    /// The <xref:System.Diagnostics.Process> class will then automatically use the default system encoding rather than UTF8, provided that you have registered the encoding provider before calling any <xref:System.Diagnostics.Process> methods.
    /// ## Examples
    /// The following example uses an instance of the <xref:System.Diagnostics.Process> class to start a process.
    /// [!code-cpp[Process.Start_instance#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_instance/CPP/processstart.cpp#1)]
    /// [!code-csharp[Process.Start_instance#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_instance/CS/processstart.cs#1)]
    /// [!code-vb[Process.Start_instance#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_instance/VB/processstart.vb#1)]
    /// The following example uses the <xref:System.Diagnostics.Process> class itself and a static <xref:System.Diagnostics.Process.Start%2A> method to start a process.
    /// [!code-cpp[Process.Start_static#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_static/CPP/processstartstatic.cpp)]
    /// [!code-csharp[Process.Start_static#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_static/CS/processstartstatic.cs)]
    /// [!code-vb[Process.Start_static#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_static/VB/processstartstatic.vb)]
    /// The following F# example defines a `runProc` function that starts a process, captures all output and error information, and records the number of milliseconds that the process has run.  The `runProc` function has three parameters: the name of application to launch, the arguments to supply to the application, and the starting directory.
    /// [!code-fsharp[System.Diagnostics.Process#1](~/samples/snippets/fsharp/VS_Snippets_CLR_System/system.diagnostics.process/fs/Start1.fs#1)]
    /// The code for the `runProc` function was written by [ImaginaryDevelopment](http://fssnip.net/authors/ImaginaryDevelopment) and is available under the [Microsoft Public License](https://opensource.org/licenses/ms-pl).
    /// ]]></format></remarks>
    /// <altmember cref="O:System.Diagnostics.Process.Start"/>
    /// <altmember cref="System.Diagnostics.ProcessStartInfo"/>
    /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
    /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
    /// <altmember cref="System.Diagnostics.ProcessThread"/>
    /// <related type="ExternalDocumentation" href="https://code.msdn.microsoft.com/windowsdesktop/Using-the-NET-Process-Class-d70597ef">Using the .NET Process Class</related>
    [Designer("System.Diagnostics.Design.ProcessDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class Process : Component
    {
        private bool _haveProcessId;
        private int _processId;
        private bool _haveProcessHandle;
        private SafeProcessHandle? _processHandle;
        private bool _isRemoteMachine;
        private string _machineName;
        private ProcessInfo? _processInfo;

        private ProcessThreadCollection? _threads;
        private ProcessModuleCollection? _modules;

        private bool _haveWorkingSetLimits;
        private IntPtr _minWorkingSet;
        private IntPtr _maxWorkingSet;

        private bool _haveProcessorAffinity;
        private IntPtr _processorAffinity;

        private bool _havePriorityClass;
        private ProcessPriorityClass _priorityClass;

        private ProcessStartInfo? _startInfo;

        private bool _watchForExit;
        private bool _watchingForExit;
        private EventHandler? _onExited;
        private bool _exited;
        private int _exitCode;

        private DateTime? _startTime;
        private DateTime _exitTime;
        private bool _haveExitTime;

        private bool _priorityBoostEnabled;
        private bool _havePriorityBoostEnabled;

        private bool _raisedOnExited;
        private RegisteredWaitHandle? _registeredWaitHandle;
        private WaitHandle? _waitHandle;
        private StreamReader? _standardOutput;
        private StreamWriter? _standardInput;
        private StreamReader? _standardError;
        private bool _disposed;

        private bool _standardInputAccessed;

        private StreamReadMode _outputStreamReadMode;
        private StreamReadMode _errorStreamReadMode;

        /// <summary>Occurs each time an application writes a line to its redirected <see cref="System.Diagnostics.Process.StandardOutput" /> stream.</summary>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Diagnostics.Process.OutputDataReceived> event indicates that the associated <xref:System.Diagnostics.Process> has written a line that's terminated with a newline (carriage return (CR), line feed (LF), or CR+LF) to its redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream.
        /// The event is enabled during asynchronous read operations on <xref:System.Diagnostics.Process.StandardOutput%2A>. To start asynchronous read operations, you must redirect the <xref:System.Diagnostics.Process.StandardOutput%2A> stream of a <xref:System.Diagnostics.Process>, add your event handler to the <xref:System.Diagnostics.Process.OutputDataReceived> event, and call <xref:System.Diagnostics.Process.BeginOutputReadLine%2A>. Thereafter, the <xref:System.Diagnostics.Process.OutputDataReceived> event signals each time the process writes a line to the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream, until the process exits or calls <xref:System.Diagnostics.Process.CancelOutputRead%2A>.
        /// > [!NOTE]
        /// >  The application that is processing the asynchronous output should call the <xref:System.Diagnostics.Process.WaitForExit%2A> method to ensure that the output buffer has been flushed.
        /// ## Examples
        /// The following example illustrates how to perform asynchronous read operations on the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream of the `ipconfig` command.
        /// The example creates an event delegate for the `OutputHandler` event handler and associates it with the <xref:System.Diagnostics.Process.OutputDataReceived> event. The event handler receives text lines from the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream, formats the text, and saves it in an output string that's later shown in the example's console window.
        /// [!code-cpp[Process_AsyncStreams#4](~/samples/snippets/cpp/VS_Snippets_CLR/process_asyncstreams/CPP/datareceivedevent.cpp#4)]
        /// [!code-csharp[Process_AsyncStreams#4](~/samples/snippets/csharp/VS_Snippets_CLR/process_asyncstreams/CS/datareceivedevent.cs#4)]
        /// [!code-vb[Process_AsyncStreams#4](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_asyncstreams/VB/datareceivedevent.vb#4)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput"/>
        /// <altmember cref="System.Diagnostics.Process.StandardOutput"/>
        /// <altmember cref="System.Diagnostics.Process.BeginOutputReadLine"/>
        /// <altmember cref="System.Diagnostics.Process.CancelOutputRead"/>
        /// <altmember cref="System.Diagnostics.DataReceivedEventHandler"/>
        public event DataReceivedEventHandler? OutputDataReceived;
        /// <summary>Occurs when an application writes to its redirected <see cref="System.Diagnostics.Process.StandardError" /> stream.</summary>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Diagnostics.Process.ErrorDataReceived> event indicates that the associated <xref:System.Diagnostics.Process> has written a line that's terminated with a newline (carriage return (CR), line feed (LF), or CR+LF) to its redirected <xref:System.Diagnostics.Process.StandardError%2A> stream.
        /// The event only occurs during asynchronous read operations on <xref:System.Diagnostics.Process.StandardError%2A>. To start asynchronous read operations, you must redirect the <xref:System.Diagnostics.Process.StandardError%2A> stream of a <xref:System.Diagnostics.Process>, add your event handler to the <xref:System.Diagnostics.Process.ErrorDataReceived> event, and call <xref:System.Diagnostics.Process.BeginErrorReadLine%2A>. Thereafter, the <xref:System.Diagnostics.Process.ErrorDataReceived> event signals each time the process writes a line to the redirected <xref:System.Diagnostics.Process.StandardError%2A> stream, until the process exits or calls <xref:System.Diagnostics.Process.CancelErrorRead%2A>.
        /// > [!NOTE]
        /// >  The application that is processing the asynchronous output should call the <xref:System.Diagnostics.Process.WaitForExit> method to ensure that the output buffer has been flushed. Note that specifying a timeout by using the <xref:System.Diagnostics.Process.WaitForExit(int)> overload does *not* ensure the output buffer has been flushed.
        /// ## Examples
        /// The following example uses the `net view` command to list the available network resources on a remote computer. The user supplies the target computer name as a command-line argument. The user can also supply a file name for error output. The example collects the output of the net command, waits for the process to finish, and then writes the output results to the console. If the user supplies the optional error file, the example writes errors to the file.
        /// [!code-cpp[Process_AsyncStreams#2](~/samples/snippets/cpp/VS_Snippets_CLR/process_asyncstreams/CPP/net_async.cpp#2)]
        /// [!code-csharp[Process_AsyncStreams#2](~/samples/snippets/csharp/VS_Snippets_CLR/process_asyncstreams/CS/net_async.cs#2)]
        /// [!code-vb[Process_AsyncStreams#2](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_asyncstreams/VB/net_async.vb#2)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError"/>
        /// <altmember cref="System.Diagnostics.Process.StandardError"/>
        /// <altmember cref="System.Diagnostics.Process.BeginErrorReadLine"/>
        /// <altmember cref="System.Diagnostics.Process.CancelErrorRead"/>
        /// <altmember cref="System.Diagnostics.DataReceivedEventHandler"/>
        public event DataReceivedEventHandler? ErrorDataReceived;

        // Abstract the stream details
        internal AsyncStreamReader? _output;
        internal AsyncStreamReader? _error;
        internal bool _pendingOutputRead;
        internal bool _pendingErrorRead;

        private static int s_cachedSerializationSwitch;

        /// <summary>Initializes a new instance of the <see cref="System.Diagnostics.Process" /> class.</summary>
        /// <remarks>If you do not specify the <see cref="System.Diagnostics.Process.MachineName" /> property, the default is the local computer, (".").
        /// You have two options for associating a new <see cref="System.Diagnostics.Process" /> component with a process on the computer. The first option is to use the constructor to create the <see cref="System.Diagnostics.Process" /> component, set the appropriate members of the <see cref="System.Diagnostics.Process.StartInfo" /> property and call <see cref="O:System.Diagnostics.Process.Start" /> to associate the <see cref="System.Diagnostics.Process" /> with a new system process. The second option is to associate the <see cref="System.Diagnostics.Process" /> with a running system process by using <see cref="O:System.Diagnostics.Process.GetProcessById" /> or one of the <see cref="O:System.Diagnostics.Process.GetProcesses" /> return values.
        /// If you use a <see langword="static" /> overload of the <see cref="O:System.Diagnostics.Process.Start" /> method to start a new system process, the method creates a new <see cref="System.Diagnostics.Process" /> component and associates it with the process.
        /// When the <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property is set to its default value, <see langword="true" />, you can start applications and documents in a way that is similar to using the `Run` dialog box of the Windows `Start` menu. When <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is <see langword="false" />, you can start only executables.
        /// Any executable file that you can call from the command line can be started in one of two ways: by setting the appropriate members of the <see cref="System.Diagnostics.Process.StartInfo" /> property and calling the <see cref="O:System.Diagnostics.Process.Start" /> method with no parameters, or by passing the appropriate parameter to the <see langword="static" /><see cref="O:System.Diagnostics.Process.Start" /> member.
        /// You can create a <see cref="System.Diagnostics.Process" /> component by using the constructor, one of the static <see cref="O:System.Diagnostics.Process.Start" /> overloads, or any of the <see cref="O:System.Diagnostics.Process.GetProcessById" />, <see cref="O:System.Diagnostics.Process.GetProcesses" />, or <see cref="O:System.Diagnostics.Process.GetProcessesByName" /> methods. After you have done so, you have a view into the associated process. This is not a dynamic view that updates itself automatically when the process properties have changed in memory. Instead, you must call <see cref="System.Diagnostics.Process.Refresh" /> for the component to update the <see cref="System.Diagnostics.Process" /> property information in your application.</remarks>
        public Process()
        {
            // This class once inherited a finalizer. For backward compatibility it has one so that
            // any derived class that depends on it will see the behaviour expected. Since it is
            // not used by this class itself, suppress it immediately if this is not an instance
            // of a derived class it doesn't suffer the GC burden of finalization.
            if (GetType() == typeof(Process))
            {
                GC.SuppressFinalize(this);
            }

            _machineName = ".";
            _outputStreamReadMode = StreamReadMode.Undefined;
            _errorStreamReadMode = StreamReadMode.Undefined;
        }

        private Process(string machineName, bool isRemoteMachine, int processId, ProcessInfo? processInfo)
        {
            GC.SuppressFinalize(this);
            _processInfo = processInfo;
            _machineName = machineName;
            _isRemoteMachine = isRemoteMachine;
            _processId = processId;
            _haveProcessId = true;
            _outputStreamReadMode = StreamReadMode.Undefined;
            _errorStreamReadMode = StreamReadMode.Undefined;
        }

        /// <summary>Gets the native handle to this process.</summary>
        /// <value>The native handle to this process.</value>
        /// <remarks>The handle is only available if the calling component started the process.</remarks>
        public SafeProcessHandle SafeHandle
        {
            get
            {
                EnsureState(State.Associated);
                return GetOrOpenProcessHandle();
            }
        }

        /// <summary>Gets the native handle of the associated process.</summary>
        /// <value>The handle that the operating system assigned to the associated process when the process was started. The system uses this handle to keep track of process attributes.</value>
        /// <remarks>An application can obtain a handle to a process that can be used as a parameter to many process-information and control functions. You can use this handle to initialize a <see cref="System.Threading.WaitHandle" /> or to call native methods with platform invoke.
        /// This process handle is private to an application--in other words, process handles cannot be shared. A process also has a process <see cref="System.Diagnostics.Process.Id" /> which, unlike the <see cref="System.Diagnostics.Process.Handle" />, is unique and, therefore, valid throughout the system.
        /// Only processes started through a call to <see cref="O:System.Diagnostics.Process.Start" /> set the <see cref="System.Diagnostics.Process.Handle" /> property of the corresponding <see cref="System.Diagnostics.Process" /> instances.</remarks>
        /// <exception cref="System.InvalidOperationException">The process has not been started or has exited. The <see cref="System.Diagnostics.Process.Handle" /> property cannot be read because there is no process associated with this <see cref="System.Diagnostics.Process" /> instance.
        /// -or-
        /// The <see cref="System.Diagnostics.Process" /> instance has been attached to a running process but you do not have the necessary permissions to get a handle with full access rights.</exception>
        /// <exception cref="System.NotSupportedException">You are trying to access the <see cref="System.Diagnostics.Process.Handle" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.Id"/>
        /// <altmember cref="System.Diagnostics.Process.ExitCode"/>
        /// <altmember cref="System.Diagnostics.Process.ExitTime"/>
        /// <altmember cref="System.Diagnostics.Process.HandleCount"/>
        /// <altmember cref="O:System.Diagnostics.Process.Start"/>
        /// <altmember cref="System.Diagnostics.Process.Refresh"/>
        public IntPtr Handle => SafeHandle.DangerousGetHandle();

        /// <devdoc>
        ///     Returns whether this process component is associated with a real process.
        /// </devdoc>
        /// <internalonly/>
        private bool Associated
        {
            get { return _haveProcessId || _haveProcessHandle; }
        }

        /// <summary>Gets the base priority of the associated process.</summary>
        /// <value>The base priority, which is computed from the <see cref="System.Diagnostics.Process.PriorityClass" /> of the associated process.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the most recently refreshed base priority of the process. To get the most up to date base priority, you need to call <xref:System.Diagnostics.Process.Refresh> method first.
        /// The <xref:System.Diagnostics.Process.BasePriority%2A> of the process is the starting priority for threads created within the associated process. You can view information about the base priority through the System Monitor's Priority Base counter.
        /// Based on the time elapsed or other boosts, the operating system can change the base priority when a process should be placed ahead of others.
        /// The <xref:System.Diagnostics.Process.BasePriority%2A> property lets you view the starting priority assigned to a process. However, because it is read-only, you cannot use the <xref:System.Diagnostics.Process.BasePriority%2A> to set the priority of the process. To change the priority, use the <xref:System.Diagnostics.Process.PriorityClass%2A> property. The <xref:System.Diagnostics.Process.BasePriority%2A> is viewable using the System Monitor, while the <xref:System.Diagnostics.Process.PriorityClass%2A> is not. Both the <xref:System.Diagnostics.Process.BasePriority%2A> and the <xref:System.Diagnostics.Process.PriorityClass%2A> can be viewed programmatically. The following table shows the relationship between <xref:System.Diagnostics.Process.BasePriority%2A> values and <xref:System.Diagnostics.Process.PriorityClass%2A> values.
        /// |BasePriority|PriorityClass|
        /// |------------------|-------------------|
        /// |4|<xref:System.Diagnostics.ProcessPriorityClass.Idle>|
        /// |8|<xref:System.Diagnostics.ProcessPriorityClass.Normal>|
        /// |13|<xref:System.Diagnostics.ProcessPriorityClass.High>|
        /// |24|<xref:System.Diagnostics.ProcessPriorityClass.RealTime>|
        /// ## Examples
        /// The following example starts an instance of Notepad. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays the process's exit code.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The process has exited.
        /// -or-
        /// The process has not started, so there is no process ID.</exception>
        /// <altmember cref="System.Diagnostics.Process.PriorityClass"/>
        /// <altmember cref="System.Diagnostics.ProcessPriorityClass"/>
        /// <altmember cref="System.Diagnostics.ThreadPriorityLevel"/>
        public int BasePriority
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.BasePriority;
            }
        }

        /// <summary>Gets the value that the associated process specified when it terminated.</summary>
        /// <value>The code that the associated process specified when it terminated.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use <xref:System.Diagnostics.Process.ExitCode%2A> to get the status that the system process returned when it exited. You can use the exit code much like an integer return value from a `main()` procedure.
        /// The <xref:System.Diagnostics.Process.ExitCode%2A> value for a process reflects the specific convention implemented by the application developer for that process. If you use the exit code value to make decisions in your code, be sure that you know the exit code convention used by the application process.
        /// Developers usually indicate a successful exit by an <xref:System.Diagnostics.Process.ExitCode%2A> value of zero, and designate errors by nonzero values that the calling method can use to identify the cause of an abnormal process termination. It is not necessary to follow these guidelines, but they are the convention.
        /// If you try to get the <xref:System.Diagnostics.Process.ExitCode%2A> before the process has exited, the attempt throws an exception. Examine the <xref:System.Diagnostics.Process.HasExited%2A> property first to verify whether the associated process has terminated.
        /// > [!NOTE]
        /// >  When standard output has been redirected to asynchronous event handlers, it is possible that output processing will not have completed when <xref:System.Diagnostics.Process.HasExited%2A> returns `true`. To ensure that asynchronous event handling has been completed, call the <xref:System.Diagnostics.Process.WaitForExit> overload that takes no parameter before checking <xref:System.Diagnostics.Process.HasExited%2A>.
        /// You can use the <xref:System.Diagnostics.Process.CloseMainWindow%2A> or the <xref:System.Diagnostics.Process.Kill%2A> method to cause an associated process to exit.
        /// There are two ways of being notified when the associated process exits: synchronously and asynchronously. Synchronous notification relies on calling the <xref:System.Diagnostics.Process.WaitForExit%2A> method to pause the processing of your application until the associated component exits. Asynchronous notification relies on the <xref:System.Diagnostics.Process.Exited> event. When using asynchronous notification, <xref:System.Diagnostics.Process.EnableRaisingEvents%2A> must be set to `true` for the <xref:System.Diagnostics.Process> component to receive notification that the process has exited.
        /// ## Examples
        /// The following example starts an instance of Notepad. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays the process's exit code.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The process has not exited.
        /// -or-
        /// The process <see cref="System.Diagnostics.Process.Handle" /> is not valid.</exception>
        /// <exception cref="System.NotSupportedException">You are trying to access the <see cref="System.Diagnostics.Process.ExitCode" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.HasExited"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
        /// <altmember cref="System.Diagnostics.Process.WaitForExit(int)"/>
        /// <altmember cref="System.Diagnostics.Process.EnableRaisingEvents"/>
        public int ExitCode
        {
            get
            {
                EnsureState(State.Exited);
                return _exitCode;
            }
        }

        /// <summary>Gets a value indicating whether the associated process has been terminated.</summary>
        /// <value><see langword="true" /> if the operating system process referenced by the <see cref="System.Diagnostics.Process" /> component has terminated; otherwise, <see langword="false" />.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// A value of `true` for <xref:System.Diagnostics.Process.HasExited%2A> indicates that the associated process has terminated, either normally or abnormally. You can request or force the associated process to exit by calling <xref:System.Diagnostics.Process.CloseMainWindow%2A> or <xref:System.Diagnostics.Process.Kill%2A>. If a handle is open to the process, the operating system releases the process memory when the process has exited, but retains administrative information about the process, such as the handle, exit code, and exit time. To get this information, you can use the <xref:System.Diagnostics.Process.ExitCode%2A> and <xref:System.Diagnostics.Process.ExitTime%2A> properties. These properties are populated automatically for processes that were started by this component. The administrative information is released when all the <xref:System.Diagnostics.Process> components that are associated with the system process are destroyed and hold no more handles to the exited process.
        /// A process can terminate independently of your code. If you started the process using this component, the system updates the value of <xref:System.Diagnostics.Process.HasExited%2A> automatically, even if the associated process exits independently.
        /// > [!NOTE]
        /// >  When standard output has been redirected to asynchronous event handlers, it is possible that output processing will not have completed when this property returns `true`. To ensure that asynchronous event handling has been completed, call the <xref:System.Diagnostics.Process.WaitForExit> overload that takes no parameter before checking <xref:System.Diagnostics.Process.HasExited%2A>.
        /// ## Examples
        /// The following example starts an instance of Notepad. It then retrieves the physical memory usage of the associated process at 2 second intervals for a maximum of 10 seconds. The example detects whether the process exits before 10 seconds have elapsed. The example closes the process if it is still running after 10 seconds.
        /// [!code-cpp[process_refresh#1](~/samples/snippets/cpp/VS_Snippets_CLR/process_refresh/CPP/process_refresh.cpp#1)]
        /// [!code-csharp[process_refresh#1](~/samples/snippets/csharp/VS_Snippets_CLR/process_refresh/CS/process_refresh.cs#1)]
        /// [!code-vb[process_refresh#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_refresh/VB/process_refresh.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">There is no process associated with the object.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">The exit code for the process could not be retrieved.</exception>
        /// <exception cref="System.NotSupportedException">You are trying to access the <see cref="System.Diagnostics.Process.HasExited" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.ExitCode"/>
        /// <altmember cref="System.Diagnostics.Process.ExitTime"/>
        /// <altmember cref="System.Diagnostics.Process.WaitForExit(int)"/>
        /// <altmember cref="System.Diagnostics.Process.EnableRaisingEvents"/>
        /// <altmember cref="System.Diagnostics.Process.OnExited"/>
        public bool HasExited
        {
            get
            {
                if (!_exited)
                {
                    EnsureState(State.Associated);
                    UpdateHasExited();
                    if (_exited)
                    {
                        RaiseOnExited();
                    }
                }
                return _exited;
            }
        }

        /// <summary>Gets the time that the associated process was started.</summary>
        /// <value>An object  that indicates when the process started. An exception is thrown if the process is not running.</value>
        /// <exception cref="System.NotSupportedException">You are attempting to access the <see cref="System.Diagnostics.Process.StartTime" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <exception cref="System.InvalidOperationException">The process has exited.
        /// -or-
        /// The process has not been started.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">An error occurred in the call to the Windows function.</exception>
        public DateTime StartTime
        {
            get
            {
                if (!_startTime.HasValue)
                {
                    _startTime = StartTimeCore;
                }
                return _startTime.Value;
            }
        }

        /// <summary>Gets the time that the associated process exited.</summary>
        /// <value>A <see cref="System.DateTime" /> that indicates when the associated process was terminated.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// If the process has not terminated, attempting to retrieve the <xref:System.Diagnostics.Process.ExitTime%2A> property throws an exception. Use <xref:System.Diagnostics.Process.HasExited%2A> before getting the <xref:System.Diagnostics.Process.ExitTime%2A> property to determine whether the associated process has terminated.
        /// ## Examples
        /// The following code example creates a process that prints a file. The process raises the <xref:System.Diagnostics.Process.Exited> event when it exits, and the event handler displays the <xref:System.Diagnostics.Process.ExitTime%2A> property and other process information.
        /// [!code-csharp[System.Diagnostics.Process.EnableExited#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Diagnostics.Process.EnableExited/CS/processexitedevent.cs#1)]
        /// [!code-vb[System.Diagnostics.Process.EnableExited#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Diagnostics.Process.EnableExited/VB/processexitedevent.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.NotSupportedException">You are trying to access the <see cref="System.Diagnostics.Process.ExitTime" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.Handle"/>
        /// <altmember cref="System.Diagnostics.Process.ExitCode"/>
        public DateTime ExitTime
        {
            get
            {
                if (!_haveExitTime)
                {
                    EnsureState(State.Exited);
                    _exitTime = ExitTimeCore;
                    _haveExitTime = true;
                }
                return _exitTime;
            }
        }

        /// <summary>Gets the unique identifier for the associated process.</summary>
        /// <value>The system-generated unique identifier of the process that is referenced by this <see cref="System.Diagnostics.Process" /> instance.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The process <xref:System.Diagnostics.Process.Id%2A> is not valid if the associated process is not running. Therefore, you should ensure that the process is running before attempting to retrieve the <xref:System.Diagnostics.Process.Id%2A> property. Until the process terminates, the process identifier uniquely identifies the process throughout the system.
        /// You can connect a process that is running on a local or remote computer to a new <xref:System.Diagnostics.Process> instance by passing the process identifier to the <xref:System.Diagnostics.Process.GetProcessById%2A> method. <xref:System.Diagnostics.Process.GetProcessById%2A> is a `static` method that creates a new component and sets the <xref:System.Diagnostics.Process.Id%2A> property for the new <xref:System.Diagnostics.Process> instance automatically.
        /// Process identifiers can be reused by the system. The <xref:System.Diagnostics.Process.Id%2A> property value is unique only while the associated process is running. After the process has terminated, the system can reuse the <xref:System.Diagnostics.Process.Id%2A> property value for an unrelated process.
        /// Because the identifier is unique on the system, you can pass it to other threads as an alternative to passing a <xref:System.Diagnostics.Process> instance. This action can save system resources yet guarantee that the process is correctly identified.
        /// ## Examples
        /// The following example demonstrates how to obtain the <xref:System.Diagnostics.Process.Id%2A> for all running instances of an application. The code creates a new instance of Notepad, lists all the instances of Notepad, and then allows the user to enter the <xref:System.Diagnostics.Process.Id%2A> number to remove a specific instance.
        /// [!code-csharp[System.Diagnostics.Process.Id#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Diagnostics.Process.Id/CS/program.cs#1)]
        /// [!code-vb[System.Diagnostics.Process.Id#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Diagnostics.Process.Id/VB/program.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The process's <see cref="System.Diagnostics.Process.Id" /> property has not been set.
        /// -or-
        /// There is no process associated with this <see cref="System.Diagnostics.Process" /> object.</exception>
        /// <altmember cref="System.Diagnostics.Process.Handle"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessById(int,string)"/>
        public int Id
        {
            get
            {
                EnsureState(State.HaveId);
                return _processId;
            }
        }

        /// <summary>Gets the name of the computer the associated process is running on.</summary>
        /// <value>The name of the computer that the associated process is running on.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// You can view statistical data and process information for processes running on remote computers but you cannot call <xref:System.Diagnostics.Process.Start%2A>, <xref:System.Diagnostics.Process.CloseMainWindow%2A>, or <xref:System.Diagnostics.Process.Kill%2A> on remote computers.
        /// > [!NOTE]
        /// >  When the associated process is executing on the local machine, this property returns a period (".") for the machine name. You should use the <xref:System.Environment.MachineName%2A?displayProperty=nameWithType> property to get the correct machine name.
        /// ## Examples
        /// To use the following example you must first start at least one instance of Notepad on a remote computer. The example requests the name of the remote computer on which Notepad is running, and then displays the respective <xref:System.Diagnostics.Process.ProcessName%2A>, <xref:System.Diagnostics.Process.Id%2A>, and <xref:System.Diagnostics.Process.MachineName%2A> properties for each instance.
        /// [!code-csharp[process_GetProcessesByName2_2#2](~/samples/snippets/csharp/VS_Snippets_CLR/Process_GetProcessesByName2_2/CS/process_getprocessesbyname2_2.cs#2)]
        /// [!code-vb[process_GetProcessesByName2_2#2](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process_GetProcessesByName2_2/VB/process_getprocessesbyname2_2.vb#2)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">There is no process associated with this <see cref="System.Diagnostics.Process" /> object.</exception>
        /// <altmember cref="O:System.Diagnostics.Process.GetProcesses"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessById(int,string)"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessesByName(string)"/>
        public string MachineName
        {
            get
            {
                EnsureState(State.Associated);
                return _machineName;
            }
        }

        /// <summary>Gets or sets the maximum allowable working set size, in bytes, for the associated process.</summary>
        /// <value>The maximum working set size that is allowed in memory for the process, in bytes.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The working set of a process is the set of memory pages currently visible to the process in physical RAM memory. These pages are resident and available for an application to use without triggering a page fault.
        /// The working set includes both shared and private data. The shared data includes the pages that contain all the instructions that your application executes, including the pages in your .dll files and the system.dll files. As the working set size increases, memory demand increases.
        /// A process has minimum and maximum working set sizes. Each time a process resource is created, the system reserves an amount of memory equal to the minimum working set size for the process. The virtual memory manager attempts to keep at least the minimum amount of memory resident when the process is active, but it never keeps more than the maximum size.
        /// The system sets the default working set sizes. You can modify these sizes using the <xref:System.Diagnostics.Process.MaxWorkingSet%2A> and <xref:System.Diagnostics.Process.MinWorkingSet%2A> members. However, setting these values does not guarantee that the memory will be reserved or resident.
        /// > [!NOTE]
        /// >  When you increase the working set size of a process, you take physical memory away from the rest of the system. Ensure that you do not request a minimum or maximum working set size that is too large, because doing so can degrade system performance.
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException">The maximum working set size is invalid. It must be greater than or equal to the minimum working set size.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">Working set information cannot be retrieved from the associated process resource.
        /// -or-
        /// The process identifier or process handle is zero because the process has not been started.</exception>
        /// <exception cref="System.NotSupportedException">You are trying to access the <see cref="System.Diagnostics.Process.MaxWorkingSet" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <exception cref="System.InvalidOperationException">The process <see cref="System.Diagnostics.Process.Id" /> is not available.
        /// -or-
        /// The process has exited.</exception>
        /// <altmember cref="System.Diagnostics.Process.MinWorkingSet"/>
        /// <altmember cref="System.Diagnostics.Process.WorkingSet64"/>
        /// <altmember cref="System.Diagnostics.Process.PeakWorkingSet64"/>
        public IntPtr MaxWorkingSet
        {
            get
            {
                EnsureWorkingSetLimits();
                return _maxWorkingSet;
            }
            [SupportedOSPlatform("windows")]
            [SupportedOSPlatform("macos")]
            [SupportedOSPlatform("freebsd")]
            set
            {
                SetWorkingSetLimits(null, value);
            }
        }

        /// <summary>Gets or sets the minimum allowable working set size, in bytes, for the associated process.</summary>
        /// <value>The minimum working set size that is required in memory for the process, in bytes.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The working set of a process is the set of memory pages currently visible to the process in physical RAM memory. These pages are resident and available for an application to use without triggering a page fault.
        /// The working set includes both shared and private data. The shared data includes the pages that contain all the instructions that your application executes, including the pages in your .dll files and the system.dll files. As the working set size increases, memory demand increases.
        /// A process has minimum and maximum working set sizes. Each time a process resource is created, the system reserves an amount of memory equal to the minimum working set size for the process. The virtual memory manager attempts to keep at least the minimum amount of memory resident when the process is active, but it never keeps more than the maximum size.
        /// The system sets the default working set sizes. You can modify these sizes using the <xref:System.Diagnostics.Process.MaxWorkingSet%2A> and <xref:System.Diagnostics.Process.MinWorkingSet%2A> members. However, setting these values does not guarantee that the memory will be reserved or resident.
        /// > [!NOTE]
        /// >  When you increase the working set size of a process, you take physical memory away from the rest of the system. Ensure that you do not request a minimum or maximum working set size that is too large, because doing so can degrade system performance.
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException">The minimum working set size is invalid. It must be less than or equal to the maximum working set size.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">Working set information cannot be retrieved from the associated process resource.
        /// -or-
        /// The process identifier or process handle is zero because the process has not been started.</exception>
        /// <exception cref="System.NotSupportedException">You are trying to access the <see cref="System.Diagnostics.Process.MinWorkingSet" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <exception cref="System.InvalidOperationException">The process <see cref="System.Diagnostics.Process.Id" /> is not available.
        /// -or-
        /// The process has exited.</exception>
        /// <altmember cref="System.Diagnostics.Process.MaxWorkingSet"/>
        /// <altmember cref="System.Diagnostics.Process.WorkingSet64"/>
        /// <altmember cref="System.Diagnostics.Process.PeakWorkingSet64"/>
        public IntPtr MinWorkingSet
        {
            get
            {
                EnsureWorkingSetLimits();
                return _minWorkingSet;
            }
            [SupportedOSPlatform("windows")]
            [SupportedOSPlatform("macos")]
            [SupportedOSPlatform("freebsd")]
            set
            {
                SetWorkingSetLimits(value, null);
            }
        }

        /// <summary>Gets the modules that have been loaded by the associated process.</summary>
        /// <value>An array of type <see cref="System.Diagnostics.ProcessModule" /> that represents the modules that have been loaded by the associated process.</value>
        /// <remarks>The value returned by this property represents the most recently refreshed modules. To get the most up to date information, you need to call <see cref="System.Diagnostics.Process.Refresh" /> method first.
        /// A process module represents a.dll or .exe file that is loaded into a particular process. A <see cref="System.Diagnostics.ProcessModule" /> instance lets you view information about a module, including the module name, file name, and module memory details.
        /// A process can load multiple modules into memory. For example,.exe files that load additional .dll files have multiple modules.
        /// After starting the process, this collection is empty until the system has loaded the process. If the process has a main window, you can call <see cref="O:System.Diagnostics.Process.WaitForInputIdle" /> before retrieving this property to ensure that the collection is nonempty when you get the list.</remarks>
        /// <exception cref="System.NotSupportedException">You are attempting to access the <see cref="System.Diagnostics.Process.Modules" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <exception cref="System.InvalidOperationException">The process <see cref="System.Diagnostics.Process.Id" /> is not available.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">You are attempting to access the <see cref="System.Diagnostics.Process.Modules" /> property for either the system process or the idle process. These processes do not have modules.</exception>
        /// <altmember cref="System.Diagnostics.ProcessModule"/>
        public ProcessModuleCollection Modules
        {
            get
            {
                if (_modules == null)
                {
                    EnsureState(State.HaveNonExitedId | State.IsLocal);
                    _modules = ProcessManager.GetModules(_processId);
                }
                return _modules;
            }
        }

        /// <summary>Gets the amount of nonpaged system memory, in bytes, allocated for the associated process.</summary>
        /// <value>The amount of system memory, in bytes, allocated for the associated process that cannot be written to the virtual memory paging file.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the most recently refreshed size of nonpaged system memory used by the process, in bytes. To get the most up to date size, you need to call <xref:System.Diagnostics.Process.Refresh> method first.
        /// System memory is the physical memory used by the operating system, and is divided into paged and nonpaged pools. Nonpaged memory allocations remain in system memory and are not paged out to the virtual memory paging file.
        /// This property can be used to monitor memory usage on computers with 32-bit processors or 64-bit processors. The property value is equivalent to the **Pool Nonpaged Bytes** performance counter for the process.
        /// ## Examples
        /// The following code example starts an instance of the Notepad application. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays its exit code and peak memory statistics.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.PagedSystemMemorySize64"/>
        public long NonpagedSystemMemorySize64
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.PoolNonPagedBytes;
            }
        }

        /// <summary>Gets the amount of nonpaged system memory, in bytes, allocated for the associated process.</summary>
        /// <value>The amount of memory, in bytes, the system has allocated for the associated process that cannot be written to the virtual memory paging file.</value>
        /// <altmember cref="System.Diagnostics.Process.NonpagedSystemMemorySize64"/>
        [ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.NonpagedSystemMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int NonpagedSystemMemorySize
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return unchecked((int)_processInfo!.PoolNonPagedBytes);
            }
        }

        /// <summary>Gets the amount of paged memory, in bytes, allocated for the associated process.</summary>
        /// <value>The amount of memory, in bytes, allocated in the virtual memory paging file for the associated process.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the most recently refreshed size of memory in the virtual memory paging file used by the process, in bytes. To get the most up to date size, you need to call <xref:System.Diagnostics.Process.Refresh> method first.
        /// The operating system uses the virtual memory paging file in conjunction with physical memory to manage the virtual address space for each process. When pageable memory is not in use, it can be transferred to the virtual memory paging file on disk. To obtain the size of memory used by the operating system for the process, use the <xref:System.Diagnostics.Process.PagedSystemMemorySize64%2A> property.
        /// This property can be used to monitor memory usage on computers with 32-bit processors or 64-bit processors. The property value is equivalent to the **Page File Bytes** performance counter for the process.
        /// ## Examples
        /// The following code example starts an instance of the Notepad application, and then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays its exit code and peak memory statistics.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.PeakPagedMemorySize64"/>
        /// <altmember cref="System.Diagnostics.Process.PagedSystemMemorySize64"/>
        public long PagedMemorySize64
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.PageFileBytes;
            }
        }

        /// <summary>Gets the amount of paged memory, in bytes, allocated for the associated process.</summary>
        /// <value>The amount of memory, in bytes, allocated by the associated process that can be written to the virtual memory paging file.</value>
        /// <altmember cref="System.Diagnostics.Process.PagedMemorySize64"/>
        [ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PagedMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PagedMemorySize
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return unchecked((int)_processInfo!.PageFileBytes);
            }
        }

        /// <summary>Gets the amount of pageable system memory, in bytes, allocated for the associated process.</summary>
        /// <value>The amount of system memory, in bytes, allocated for the associated process that can be written to the virtual memory paging file.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property value represents the current size of pageable system memory used by the process, in bytes. System memory is the physical memory used by the operating system, and is divided into paged and nonpaged pools. When pageable memory is not in use, it can be transferred to the virtual memory paging file on disk. To obtain the size of the application memory used by the process, use the <xref:System.Diagnostics.Process.PagedMemorySize64%2A> property.
        /// This property can be used to monitor memory usage on computers with 32-bit processors or 64-bit processors. The property value is equivalent to the **Pool Paged Bytes** performance counter for the process.
        /// ## Examples
        /// The following code example starts an instance of the Notepad application. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays its exit code and peak memory statistics.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.PagedSystemMemorySize64"/>
        /// <altmember cref="System.Diagnostics.Process.NonpagedSystemMemorySize64"/>
        public long PagedSystemMemorySize64
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.PoolPagedBytes;
            }
        }

        /// <summary>Gets the amount of pageable system memory, in bytes, allocated for the associated process.</summary>
        /// <value>The amount of memory, in bytes, the system has allocated for the associated process that can be written to the virtual memory paging file.</value>
        /// <altmember cref="System.Diagnostics.Process.PagedSystemMemorySize64"/>
        [ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PagedSystemMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PagedSystemMemorySize
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return unchecked((int)_processInfo!.PoolPagedBytes);
            }
        }

        /// <summary>Gets the maximum amount of memory in the virtual memory paging file, in bytes, used by the associated process.</summary>
        /// <value>The maximum amount of memory, in bytes, allocated in the virtual memory paging file for the associated process since it was started.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property value represents the maximum size of memory in the virtual memory paging file used by the process since it started, in bytes. The operating system uses the virtual memory paging file in conjunction with physical memory to manage the virtual address space for each process. When pageable memory is not in use, it can be transferred to the virtual memory paging file on disk.
        /// This property can be used to monitor memory usage on computers with 32-bit processors or 64-bit processors. The property value is equivalent to the **Page File Bytes Peak** performance counter for the process.
        /// ## Examples
        /// The following code example starts an instance of the Notepad application. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays its exit code and peak memory statistics.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.PagedMemorySize64"/>
        public long PeakPagedMemorySize64
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.PageFileBytesPeak;
            }
        }

        /// <summary>Gets the maximum amount of memory in the virtual memory paging file, in bytes, used by the associated process.</summary>
        /// <value>The maximum amount of memory, in bytes, allocated by the associated process that could be written to the virtual memory paging file.</value>
        /// <altmember cref="System.Diagnostics.Process.PeakPagedMemorySize64"/>
        [ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PeakPagedMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PeakPagedMemorySize
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return unchecked((int)_processInfo!.PageFileBytesPeak);
            }
        }

        /// <summary>Gets the maximum amount of physical memory, in bytes, used by the associated process.</summary>
        /// <value>The maximum amount of physical memory, in bytes, allocated for the associated process since it was started.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the maximum size of working set memory used by the process since it started, in bytes. The working set of a process is the set of memory pages currently visible to the process in physical RAM memory. These pages are resident and available for an application to use without triggering a page fault.
        /// The working set includes both shared and private data. The shared data includes the pages that contain all the instructions that the process executes, including instructions from the process modules and the system libraries.
        /// This property can be used to monitor memory usage on computers with 32-bit processors or 64-bit processors. The property value is equivalent to the **Working Set Peak** performance counter for the process.
        /// ## Examples
        /// The following code example starts an instance of the Notepad application. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays its exit code and peak memory statistics.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.WorkingSet64"/>
        /// <altmember cref="System.Diagnostics.Process.MinWorkingSet"/>
        /// <altmember cref="System.Diagnostics.Process.MaxWorkingSet"/>
        public long PeakWorkingSet64
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.WorkingSetPeak;
            }
        }

        /// <summary>Gets the peak working set size for the associated process, in bytes.</summary>
        /// <value>The maximum amount of physical memory that the associated process has required all at once, in bytes.</value>
        /// <remarks>The working set of a process is the set of memory pages currently visible to the process in physical RAM memory. These pages are resident and available for an application to use without triggering a page fault.
        /// The working set includes both shared and private data. The shared data includes the pages that contain all the instructions that the process executes, including process modules and the system libraries.</remarks>
        /// <altmember cref="System.Diagnostics.Process.WorkingSet64"/>
        /// <altmember cref="System.Diagnostics.Process.MinWorkingSet"/>
        /// <altmember cref="System.Diagnostics.Process.MaxWorkingSet"/>
        /// <altmember cref="System.Diagnostics.Process.PeakWorkingSet64"/>
        [ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PeakWorkingSet64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PeakWorkingSet
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return unchecked((int)_processInfo!.WorkingSetPeak);
            }
        }

        /// <summary>Gets the maximum amount of virtual memory, in bytes, used by the associated process.</summary>
        /// <value>The maximum amount of virtual memory, in bytes, allocated for the associated process since it was started.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the maximum size of virtual memory used by the process since it started, in bytes. The operating system maps the virtual address space for each process either to pages loaded in physical memory, or to pages stored in the virtual memory paging file on disk.
        /// This property can be used to monitor memory usage on computers with 32-bit processors or 64-bit processors. The property value is equivalent to the **Virtual Bytes Peak** performance counter for the process.
        /// ## Examples
        /// The following code example starts an instance of the Notepad application. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays its exit code and peak memory statistics.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.VirtualMemorySize64"/>
        public long PeakVirtualMemorySize64
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.VirtualBytesPeak;
            }
        }

        /// <summary>Gets the maximum amount of virtual memory, in bytes, used by the associated process.</summary>
        /// <value>The maximum amount of virtual memory, in bytes, that the associated process has requested.</value>
        /// <altmember cref="System.Diagnostics.Process.PeakVirtualMemorySize64"/>
        [ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PeakVirtualMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PeakVirtualMemorySize
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return unchecked((int)_processInfo!.VirtualBytesPeak);
            }
        }

        /// <summary>Gets or sets a value indicating whether the associated process priority should temporarily be boosted by the operating system when the main window has the focus.</summary>
        /// <value><see langword="true" /> if dynamic boosting of the process priority should take place for a process when it is taken out of the wait state; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the most recently refreshed temporary priority boost. To get the most up to date value, you need to call <xref:System.Diagnostics.Process.Refresh> method first.
        /// When a thread runs in a process for which the priority class has one of the dynamic priority enumeration values (<xref:System.Diagnostics.ProcessPriorityClass.Normal>, <xref:System.Diagnostics.ProcessPriorityClass.High>, or <xref:System.Diagnostics.ProcessPriorityClass.RealTime>), the system temporarily boosts the thread's priority when it is taken out of a wait state. This action prevents other processes from interrupting the processing of the current thread. The <xref:System.Diagnostics.Process.PriorityBoostEnabled%2A> setting affects all the existing threads and any threads subsequently created by the process. To restore normal behavior, set the <xref:System.Diagnostics.Process.PriorityBoostEnabled%2A> property to `false`.
        /// > [!NOTE]
        /// >  Boosting the priority too high can drain resources from essential operating system and network functions, causing problems with other operating system tasks.
        /// ]]></format></remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">Priority boost information could not be retrieved from the associated process resource.</exception>
        /// <exception cref="System.PlatformNotSupportedException">The process identifier or process handle is zero. (The process has not been started.)</exception>
        /// <exception cref="System.NotSupportedException">You are attempting to access the <see cref="System.Diagnostics.Process.PriorityBoostEnabled" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <exception cref="System.InvalidOperationException">The process <see cref="System.Diagnostics.Process.Id" /> is not available.</exception>
        /// <altmember cref="System.Diagnostics.Process.PriorityClass"/>
        /// <altmember cref="System.Diagnostics.Process.BasePriority"/>
        public bool PriorityBoostEnabled
        {
            get
            {
                if (!_havePriorityBoostEnabled)
                {
                    _priorityBoostEnabled = PriorityBoostEnabledCore;
                    _havePriorityBoostEnabled = true;
                }
                return _priorityBoostEnabled;
            }
            set
            {
                PriorityBoostEnabledCore = value;
                _priorityBoostEnabled = value;
                _havePriorityBoostEnabled = true;
            }
        }

        /// <summary>Gets or sets the overall priority category for the associated process.</summary>
        /// <value>The priority category for the associated process, from which the <see cref="System.Diagnostics.Process.BasePriority" /> of the process is calculated.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the most recently refreshed priority of the process. To get the most up to date priority, you need to call <xref:System.Diagnostics.Process.Refresh> method first.
        /// A process priority class encompasses a range of thread priority levels. Threads with different priorities that are running in the process run relative to the priority class of the process. Win32 uses four priority classes with seven base priority levels per class. These process priority classes are captured in the <xref:System.Diagnostics.ProcessPriorityClass> enumeration, which lets you set the process priority to <xref:System.Diagnostics.ProcessPriorityClass.Idle>, <xref:System.Diagnostics.ProcessPriorityClass.Normal>, <xref:System.Diagnostics.ProcessPriorityClass.High>, <xref:System.Diagnostics.ProcessPriorityClass.AboveNormal>, <xref:System.Diagnostics.ProcessPriorityClass.BelowNormal>, or <xref:System.Diagnostics.ProcessPriorityClass.RealTime>. Based on the time elapsed or other boosts, the base priority level can be changed by the operating system when a process needs to be put ahead of others for access to the processor. In addition, you can set the <xref:System.Diagnostics.Process.PriorityBoostEnabled%2A> to temporarily boost the priority level of threads that have been taken out of the wait state. The priority is reset when the process returns to the wait state.
        /// The <xref:System.Diagnostics.Process.BasePriority%2A> property lets you view the starting priority that is assigned to a process. However, because it is read-only, you cannot use the <xref:System.Diagnostics.Process.BasePriority%2A> property to set the priority of a process. To change the priority, use the <xref:System.Diagnostics.Process.PriorityClass%2A> property, which gets or sets the overall priority category for the process.
        /// The priority class cannot be viewed using System Monitor. The following table shows the relationship between the <xref:System.Diagnostics.Process.BasePriority%2A> and <xref:System.Diagnostics.Process.PriorityClass%2A> values.
        /// |BasePriority|PriorityClass|
        /// |------------------|-------------------|
        /// |4|<xref:System.Diagnostics.ProcessPriorityClass.Idle>|
        /// |8|<xref:System.Diagnostics.ProcessPriorityClass.Normal>|
        /// |13|<xref:System.Diagnostics.ProcessPriorityClass.High>|
        /// |24|<xref:System.Diagnostics.ProcessPriorityClass.RealTime>|
        /// ## Examples
        /// The following example starts an instance of Notepad. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays the process's exit code.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">Process priority information could not be set or retrieved from the associated process resource.
        /// -or-
        /// The process identifier or process handle is zero. (The process has not been started.)</exception>
        /// <exception cref="System.NotSupportedException">You are attempting to access the <see cref="System.Diagnostics.Process.PriorityClass" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <exception cref="System.InvalidOperationException">The process <see cref="System.Diagnostics.Process.Id" /> is not available.</exception>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException">Priority class cannot be set because it does not use a valid value, as defined in the <see cref="System.Diagnostics.ProcessPriorityClass" /> enumeration.</exception>
        /// <altmember cref="System.Diagnostics.Process.BasePriority"/>
        /// <altmember cref="System.Diagnostics.Process.PriorityBoostEnabled"/>
        public ProcessPriorityClass PriorityClass
        {
            get
            {
                if (!_havePriorityClass)
                {
                    _priorityClass = PriorityClassCore;
                    _havePriorityClass = true;
                }
                return _priorityClass;
            }
            set
            {
                if (!Enum.IsDefined(typeof(ProcessPriorityClass), value))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ProcessPriorityClass));
                }

                PriorityClassCore = value;
                _priorityClass = value;
                _havePriorityClass = true;
            }
        }

        /// <summary>Gets the amount of private memory, in bytes, allocated for the associated process.</summary>
        /// <value>The amount of memory, in bytes, allocated for the associated process that cannot be shared with other processes.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the most recently refreshed size of memory used by the process, in bytes, that cannot be shared with other processes. To get the most up to date size, you need to call <xref:System.Diagnostics.Process.Refresh> method first.
        /// This property can be used to monitor memory usage on computers with 32-bit processors or 64-bit processors. The property value is equivalent to the **Private Bytes** performance counter for the process.
        /// ## Examples
        /// The following code example starts an instance of the Notepad application. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays its exit code and peak memory statistics.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        public long PrivateMemorySize64
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.PrivateBytes;
            }
        }

        /// <summary>Gets the amount of private memory, in bytes, allocated for the associated process.</summary>
        /// <value>The number of bytes allocated by the associated process that cannot be shared with other processes.</value>
        /// <altmember cref="System.Diagnostics.Process.PrivateMemorySize64"/>
        [ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PrivateMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PrivateMemorySize
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return unchecked((int)_processInfo!.PrivateBytes);
            }
        }

        /// <summary>Gets the name of the process.</summary>
        /// <value>The name that the system uses to identify the process to the user.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Diagnostics.Process.ProcessName%2A> property holds an executable file name, such as Outlook, that does not include the .exe extension or the path. It is helpful for getting and manipulating all the processes that are associated with the same executable file.
        /// > [!NOTE]
        /// >  On [!INCLUDE[Win2kFamily](~/includes/win2kfamily-md.md)] operating systems, the <xref:System.Diagnostics.Process.ProcessName%2A> property may be truncated to 15 characters if the process module information cannot be obtained.
        /// You can call <xref:System.Diagnostics.Process.GetProcessesByName%2A>, passing it an executable file name, to retrieve an array that contains every running instance on the specified computer. You can use this array, for example, to shut down all the running instances of the executable file.
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The process does not have an identifier, or no process is associated with the <see cref="System.Diagnostics.Process" />.
        /// -or-
        /// The associated process has exited.</exception>
        /// <exception cref="System.NotSupportedException">The process is not on this computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.GetProcessesByName(string)"/>
        public string ProcessName
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.ProcessName;
            }
        }

        /// <summary>Gets or sets the processors on which the threads in this process can be scheduled to run.</summary>
        /// <value>A bitmask representing the processors that the threads in the associated process can run on. The default depends on the number of processors on the computer. The default value is 2 <sup>n</sup> -1, where n is the number of processors.</value>
        /// <remarks>The value returned by this property represents the most recently refreshed affinity of the process. To get the most up to date affinity, you need to call <see cref="System.Diagnostics.Process.Refresh" /> method first.
        /// In Windows 2000 and later, a thread in a process can migrate from processor to processor, with each migration reloading the processor cache. Under heavy system loads, specifying which processor should run a specific thread can improve performance by reducing the number of times the processor cache is reloaded. The association between a processor and a thread is called the processor affinity.
        /// Each processor is represented as a bit. Bit 0 is processor one, bit 1 is processor two, and so forth. If you set a bit to the value 1, the corresponding processor is selected for thread assignment. When you set the <see cref="System.Diagnostics.Process.ProcessorAffinity" /> value to zero, the operating system's scheduling algorithms set the thread's affinity. When the <see cref="System.Diagnostics.Process.ProcessorAffinity" /> value is set to any nonzero value, the value is interpreted as a bitmask that specifies those processors eligible for selection.
        /// The following table shows a selection of <see cref="System.Diagnostics.Process.ProcessorAffinity" /> values for an eight-processor system.
        /// |Bitmask|Binary value|Eligible processors|
        /// |-------------|------------------|-------------------------|
        /// |0x0001|00000000 00000001|1|
        /// |0x0003|00000000 00000011|1 and 2|
        /// |0x0007|00000000 00000111|1, 2 and 3|
        /// |0x0009|00000000 00001001|1 and 4|
        /// |0x007F|00000000 01111111|1, 2, 3, 4, 5, 6 and 7|</remarks>
        /// <exception cref="System.ComponentModel.Win32Exception"><see cref="System.Diagnostics.Process.ProcessorAffinity" /> information could not be set or retrieved from the associated process resource.
        /// -or-
        /// The process identifier or process handle is zero. (The process has not been started.)</exception>
        /// <exception cref="System.NotSupportedException">You are attempting to access the <see cref="System.Diagnostics.Process.ProcessorAffinity" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <exception cref="System.InvalidOperationException">The process <see cref="System.Diagnostics.Process.Id" /> was not available.
        /// -or-
        /// The process has exited.</exception>
        public IntPtr ProcessorAffinity
        {
            get
            {
                if (!_haveProcessorAffinity)
                {
                    _processorAffinity = ProcessorAffinityCore;
                    _haveProcessorAffinity = true;
                }
                return _processorAffinity;
            }
            set
            {
                ProcessorAffinityCore = value;
                _processorAffinity = value;
                _haveProcessorAffinity = true;
            }
        }

        /// <summary>Gets the Terminal Services session identifier for the associated process.</summary>
        /// <value>The Terminal Services session identifier for the associated process.</value>
        /// <remarks>The <see cref="System.Diagnostics.Process.SessionId" /> property identifies the session in which the application is currently running.</remarks>
        /// <exception cref="System.NullReferenceException">There is no session associated with this process.</exception>
        /// <exception cref="System.InvalidOperationException">There is no process associated with this session identifier.
        /// -or-
        /// The associated process is not on this machine.</exception>
        public int SessionId
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.SessionId;
            }
        }

        /// <summary>Gets or sets the properties to pass to the <see cref="O:System.Diagnostics.Process.Start" /> method of the <see cref="System.Diagnostics.Process" />.</summary>
        /// <value>The <see cref="System.Diagnostics.ProcessStartInfo" /> that represents the data with which to start the process. These arguments include the name of the executable file or document used to start the process.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// <xref:System.Diagnostics.Process.StartInfo%2A> represents the set of parameters to use to start a process. When <xref:System.Diagnostics.Process.Start%2A> is called, the <xref:System.Diagnostics.Process.StartInfo%2A> is used to specify the process to start. The only necessary <xref:System.Diagnostics.Process.StartInfo%2A> member to set is the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property. Starting a process by specifying the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property is similar to typing the information in the **Run** dialog box of the Windows **Start** menu. Therefore, the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property does not need to represent an executable file. It can be of any file type for which the extension has been associated with an application installed on the system. For example the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> can have a .txt extension if you have associated text files with an editor, such as Notepad, or it can have a .doc if you have associated .doc files with a word processing tool, such as Microsoft Word. Similarly, in the same way that the **Run** dialog box can accept an executable file name with or without the .exe extension, the .exe extension is optional in the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> member. For example, you can set the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property to either "Notepad.exe" or "Notepad".
        /// You can start a ClickOnce application by setting the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property to the location (for example, a Web address) from which you originally installed the application. Do not start a ClickOnce application by specifying its installed location on your hard drive.
        /// If the file name involves a nonexecutable file, such as a .doc file, you can include a verb specifying what action to take on the file. For example, you could set the <xref:System.Diagnostics.ProcessStartInfo.Verb%2A> to "Print" for a file ending in the .doc extension. The file name specified in the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property does not need to have an extension if you manually enter a value for the <xref:System.Diagnostics.ProcessStartInfo.Verb%2A> property. However, if you use the <xref:System.Diagnostics.ProcessStartInfo.Verbs%2A> property to determine what verbs are available, you must include the extension.
        /// You can change the parameters specified in the <xref:System.Diagnostics.Process.StartInfo%2A> property up to the time that you call the <xref:System.Diagnostics.Process.Start%2A> method on the process. After you start the process, changing the <xref:System.Diagnostics.Process.StartInfo%2A> values does not affect or restart the associated process. If you call the <xref:System.Diagnostics.Process.Start%28System.Diagnostics.ProcessStartInfo%29> method with the <xref:System.Diagnostics.ProcessStartInfo.UserName%2A?displayProperty=nameWithType> and <xref:System.Diagnostics.ProcessStartInfo.Password%2A?displayProperty=nameWithType> properties set, the unmanaged `CreateProcessWithLogonW` function is called, which starts the process in a new window even if the <xref:System.Diagnostics.ProcessStartInfo.CreateNoWindow%2A> property value is `true` or the <xref:System.Diagnostics.ProcessStartInfo.WindowStyle%2A> property value is <xref:System.Diagnostics.ProcessWindowStyle.Hidden>.
        /// You should only access the <xref:System.Diagnostics.Process.StartInfo%2A> property on a <xref:System.Diagnostics.Process> object returned by the <xref:System.Diagnostics.Process.Start%2A> method. For example, you should not access the <xref:System.Diagnostics.Process.StartInfo%2A> property on a <xref:System.Diagnostics.Process> object returned by <xref:System.Diagnostics.Process.GetProcesses%2A>. Otherwise, on .NET Core the <xref:System.Diagnostics.Process.StartInfo%2A> property will throw an <xref:System.InvalidOperationException> and on .NET Framework it will return a dummy <xref:System.Diagnostics.ProcessStartInfo> object.
        /// When the process is started, the file name is the file that populates the (read-only) <xref:System.Diagnostics.Process.MainModule%2A> property. If you want to retrieve the executable file that is associated with the process after the process has started, use the <xref:System.Diagnostics.Process.MainModule%2A> property. If you want to set the executable file of a <xref:System.Diagnostics.Process> instance for which an associated process has not been started, use the <xref:System.Diagnostics.Process.StartInfo%2A> property's <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> member. Because the members of the <xref:System.Diagnostics.Process.StartInfo%2A> property are arguments that are passed to the <xref:System.Diagnostics.Process.Start%2A> method of a process, changing the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property after the associated process has started will not reset the <xref:System.Diagnostics.Process.MainModule%2A> property. These properties are used only to initialize the associated process.
        /// ## Examples
        /// The following example populates a <xref:System.Diagnostics.Process.StartInfo%2A> with the file to execute, the action performed on it and whether it should displays a user interface. For additional examples, refer to the reference pages for properties of the <xref:System.Diagnostics.ProcessStartInfo> class.
        /// [!code-cpp[Process.Start_instance#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_instance/CPP/processstart.cpp#1)]
        /// [!code-csharp[Process.Start_instance#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_instance/CS/processstart.cs#1)]
        /// [!code-vb[Process.Start_instance#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_instance/VB/processstart.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentNullException">The value that specifies the <see cref="System.Diagnostics.Process.StartInfo" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">.NET 5+ and .NET Core only: The <see cref="O:System.Diagnostics.Process.Start" /> method was not used to start the process.</exception>
        /// <altmember cref="O:System.Diagnostics.Process.Start"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.FileName"/>
        public ProcessStartInfo StartInfo
        {
            get
            {
                if (_startInfo == null)
                {
                    if (Associated)
                    {
                        throw new InvalidOperationException(SR.CantGetProcessStartInfo);
                    }

                    _startInfo = new ProcessStartInfo();
                }
                return _startInfo;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (Associated)
                {
                    throw new InvalidOperationException(SR.CantSetProcessStartInfo);
                }

                _startInfo = value;
            }
        }

        /// <summary>Gets the set of threads that are running in the associated process.</summary>
        /// <value>An array of type <see cref="System.Diagnostics.ProcessThread" /> representing the operating system threads currently running in the associated process.</value>
        /// <remarks>The value returned by this property represents the most recently refreshed threads. To get the most up to date information, you need to call <see cref="System.Diagnostics.Process.Refresh" /> method first.
        /// A thread executes code in a process. Each process is started with a single thread, its primary thread. Any thread can create additional threads. Threads within a process share the address space of the process.
        /// Use <see cref="System.Diagnostics.ProcessThread" /> to get all the threads associated with the current process. The primary thread is not necessarily at index zero in the array.</remarks>
        /// <exception cref="System.SystemException">The process does not have an <see cref="System.Diagnostics.Process.Id" />, or no process is associated with the <see cref="System.Diagnostics.Process" /> instance.
        /// -or-
        /// The associated process has exited.</exception>
        /// <altmember cref="System.Diagnostics.ProcessThread"/>
        /// <altmember cref="System.Diagnostics.Process.BasePriority"/>
        public ProcessThreadCollection Threads
        {
            get
            {
                if (_threads == null)
                {
                    EnsureState(State.HaveProcessInfo);
                    int count = _processInfo!._threadInfoList.Count;
                    ProcessThread[] newThreadsArray = new ProcessThread[count];
                    for (int i = 0; i < count; i++)
                    {
                        newThreadsArray[i] = new ProcessThread(_isRemoteMachine, _processId, (ThreadInfo)_processInfo._threadInfoList[i]);
                    }

                    ProcessThreadCollection newThreads = new ProcessThreadCollection(newThreadsArray);
                    _threads = newThreads;
                }
                return _threads;
            }
        }

        /// <summary>Gets the number of handles opened by the process.</summary>
        /// <value>The number of operating system handles the process has opened.</value>
        /// <remarks>The value returned by this property represents the most recently refreshed handle count. To get the most up to date handle count, you need to call <see cref="System.Diagnostics.Process.Refresh" /> method first.
        /// Handles provide a way for a process to refer to objects. A process can obtain handles to files, resources, message queues, and many other operating system objects. The operating system reclaims the memory associated with the process only when the handle count is zero.</remarks>
        /// <altmember cref="System.Diagnostics.Process.Handle"/>
        /// <altmember cref="O:System.Diagnostics.Process.Start"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
        public int HandleCount
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                EnsureHandleCountPopulated();
                return _processInfo!.HandleCount;
            }
        }

        partial void EnsureHandleCountPopulated();

        /// <summary>Gets the amount of the virtual memory, in bytes, allocated for the associated process.</summary>
        /// <value>The amount of virtual memory, in bytes, allocated for the associated process.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the most recently refreshed size of virtual memory used by the process, in bytes. To get the most up to date size, you need to call <xref:System.Diagnostics.Process.Refresh> method first.
        /// The operating system maps the virtual address space for each process either to pages loaded in physical memory, or to pages stored in the virtual memory paging file on disk.
        /// This property can be used to monitor memory usage on computers with 32-bit processors or 64-bit processors. The property value is equivalent to the **Virtual Bytes** performance counter for the process.
        /// ## Examples
        /// The following code example starts an instance of the Notepad application. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays its exit code and peak memory statistics.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.PeakVirtualMemorySize64"/>
        public long VirtualMemorySize64
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.VirtualBytes;
            }
        }

        /// <summary>Gets the size of the process's virtual memory, in bytes.</summary>
        /// <value>The amount of virtual memory, in bytes, that the associated process has requested.</value>
        /// <altmember cref="System.Diagnostics.Process.VirtualMemorySize64"/>
        /// <altmember cref="System.Diagnostics.Process.PeakVirtualMemorySize64"/>
        [ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.VirtualMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int VirtualMemorySize
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return unchecked((int)_processInfo!.VirtualBytes);
            }
        }

        /// <summary>Gets or sets whether the <see cref="System.Diagnostics.Process.Exited" /> event should be raised when the process terminates.</summary>
        /// <value><see langword="true" /> if the <see cref="System.Diagnostics.Process.Exited" /> event should be raised when the associated process is terminated (through either an exit or a call to <see cref="O:System.Diagnostics.Process.Kill" />); otherwise, <see langword="false" />. The default is <see langword="false" />. Note that the <see cref="System.Diagnostics.Process.Exited" /> event is raised even if the value of <see cref="System.Diagnostics.Process.EnableRaisingEvents" /> is <see langword="false" /> when the process exits during or before the user performs a <see cref="System.Diagnostics.Process.HasExited" /> check.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Diagnostics.Process.EnableRaisingEvents%2A> property suggests whether the component should be notified when the operating system has shut down a process. The <xref:System.Diagnostics.Process.EnableRaisingEvents%2A> property is used in asynchronous processing to notify your application that a process has exited. To force your application to synchronously wait for an exit event (which interrupts processing of the application until the exit event has occurred), use the <xref:System.Diagnostics.Process.WaitForExit%2A> method.
        /// > [!NOTE]
        /// > If you're using Visual Studio and double-click a <xref:System.Diagnostics.Process> component in your project, an <xref:System.Diagnostics.Process.Exited> event delegate and event handler are automatically generated. Additional code sets the <xref:System.Diagnostics.Process.EnableRaisingEvents%2A> property to `false`. You must change this property to `true` for your event handler to execute when the associated process exits.
        /// If the component's <xref:System.Diagnostics.Process.EnableRaisingEvents%2A> value is `true`, or when <xref:System.Diagnostics.Process.EnableRaisingEvents%2A> is `false` and a <xref:System.Diagnostics.Process.HasExited%2A> check is invoked by the component, the component can access the administrative information for the associated process, which remains stored by the operating system. Such information includes the <xref:System.Diagnostics.Process.ExitTime%2A> and the <xref:System.Diagnostics.Process.ExitCode%2A>.
        /// After the associated process exits, the <xref:System.Diagnostics.Process.Handle%2A> of the component no longer points to an existing process resource. Instead, it can only be used to access the operating system's information about the process resource. The operating system is aware that there are handles to exited processes that haven't been released by <xref:System.Diagnostics.Process> components, so it keeps the <xref:System.Diagnostics.Process.ExitTime%2A> and <xref:System.Diagnostics.Process.Handle%2A> information in memory.
        /// There's a cost associated with watching for a process to exit. If <xref:System.Diagnostics.Process.EnableRaisingEvents%2A> is `true`, the <xref:System.Diagnostics.Process.Exited> event is raised when the associated process terminates. Your procedures for the <xref:System.Diagnostics.Process.Exited> event run at that time.
        /// Sometimes, your application starts a process but doesn't require notification of its closure. For example, your application can start Notepad to allow the user to perform text editing but make no further use of the Notepad application. You can choose to avoid notification when the process exits because it's not relevant to the continued operation of your application. Setting <xref:System.Diagnostics.Process.EnableRaisingEvents%2A> to `false` can save system resources.
        /// ## Examples
        /// The following code example creates a process that prints a file. It sets the <xref:System.Diagnostics.Process.EnableRaisingEvents%2A> property to cause the process to raise the <xref:System.Diagnostics.Process.Exited> event when it exits. The <xref:System.Diagnostics.Process.Exited> event handler displays process information.
        /// [!code-csharp[System.Diagnostics.Process.EnableExited#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Diagnostics.Process.EnableExited/CS/processexitedevent.cs#1)]
        /// [!code-vb[System.Diagnostics.Process.EnableExited#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Diagnostics.Process.EnableExited/VB/processexitedevent.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.WaitForExit(int)"/>
        /// <altmember cref="System.Diagnostics.Process.Exited"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
        /// <altmember cref="System.Diagnostics.Process.Handle"/>
        /// <altmember cref="System.Diagnostics.Process.ExitTime"/>
        /// <altmember cref="System.Diagnostics.Process.HasExited"/>
        public bool EnableRaisingEvents
        {
            get
            {
                return _watchForExit;
            }
            set
            {
                if (value != _watchForExit)
                {
                    if (Associated)
                    {
                        if (value)
                        {
                            EnsureWatchingForExit();
                        }
                        else
                        {
                            StopWatchingForExit();
                        }
                    }
                    _watchForExit = value;
                }
            }
        }

        /// <summary>Gets a stream used to write the input of the application.</summary>
        /// <value>A <see cref="System.IO.StreamWriter" /> that can be used to write the standard input stream of the application.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// A <xref:System.Diagnostics.Process> can read input text from its standard input stream, typically the keyboard. By redirecting the <xref:System.Diagnostics.Process.StandardInput%2A> stream, you can programmatically specify the input. For example, instead of using keyboard input, you can provide text from the contents of a designated file or output from another application.
        /// > [!NOTE]
        /// >  To use <xref:System.Diagnostics.Process.StandardInput%2A>, you must set <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A?displayProperty=nameWithType> to `false`, and you must set <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardInput%2A?displayProperty=nameWithType> to `true`. Otherwise, writing to the <xref:System.Diagnostics.Process.StandardInput%2A> stream throws an exception.
        /// ## Examples
        /// The following example illustrates how to redirect the <xref:System.Diagnostics.Process.StandardInput%2A> stream of a process. The example starts the `sort` command with redirected input. It then prompts the user for text, and passes that to the `sort` process by means of the redirected <xref:System.Diagnostics.Process.StandardInput%2A> stream. The `sort` results are displayed to the user on the console.
        /// [!code-cpp[Process_StandardInput#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process_StandardInput/CPP/process_standardinput.cpp#1)]
        /// [!code-csharp[Process_StandardInput#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process_StandardInput/CS/process_standardinput.cs#1)]
        /// [!code-vb[Process_StandardInput#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process_StandardInput/VB/process_standardinput.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Diagnostics.Process.StandardInput" /> stream has not been defined because <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardInput" /> is set to <see langword="false" />.</exception>
        /// <altmember cref="System.Diagnostics.Process.StandardOutput"/>
        /// <altmember cref="System.Diagnostics.Process.StandardError"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardInput"/>
        public StreamWriter StandardInput
        {
            get
            {
                CheckDisposed();
                if (_standardInput == null)
                {
                    throw new InvalidOperationException(SR.CantGetStandardIn);
                }

                _standardInputAccessed = true;
                return _standardInput;
            }
        }

        /// <summary>Gets a stream used to read the textual output of the application.</summary>
        /// <value>A <see cref="System.IO.StreamReader" /> that can be used to read the standard output stream of the application.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// When a <xref:System.Diagnostics.Process> writes text to its standard stream, that text is normally displayed on the console. By redirecting the <xref:System.Diagnostics.Process.StandardOutput%2A> stream, you can manipulate or suppress the output of a process. For example, you can filter the text, format it differently, or write the output to both the console and a designated log file.
        /// > [!NOTE]
        /// >  To use <xref:System.Diagnostics.Process.StandardOutput%2A>, you must set <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A?displayProperty=nameWithType> to `false`, and you must set <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardOutput%2A?displayProperty=nameWithType> to `true`. Otherwise, reading from the <xref:System.Diagnostics.Process.StandardOutput%2A> stream throws an exception.
        /// The redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream can be read synchronously or asynchronously. Methods such as <xref:System.IO.StreamReader.Read%2A>, <xref:System.IO.StreamReader.ReadLine%2A>, and <xref:System.IO.StreamReader.ReadToEnd%2A> perform synchronous read operations on the output stream of the process. These synchronous read operations do not complete until the associated <xref:System.Diagnostics.Process> writes to its <xref:System.Diagnostics.Process.StandardOutput%2A> stream, or closes the stream.
        /// In contrast, <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> starts asynchronous read operations on the <xref:System.Diagnostics.Process.StandardOutput%2A> stream. This method enables a designated event handler for the stream output and immediately returns to the caller, which can perform other work while the stream output is directed to the event handler.
        /// Synchronous read operations introduce a dependency between the caller reading from the <xref:System.Diagnostics.Process.StandardOutput%2A> stream and the child process writing to that stream. These dependencies can result in deadlock conditions. When the caller reads from the redirected stream of a child process, it is dependent on the child. The caller waits on the read operation until the child writes to the stream or closes the stream. When the child process writes enough data to fill its redirected stream, it is dependent on the parent. The child process waits on the next write operation until the parent reads from the full stream or closes the stream. The deadlock condition results when the caller and child process wait on each other to complete an operation, and neither can proceed. You can avoid deadlocks by evaluating dependencies between the caller and child process.
        /// The last two examples in this section use the <xref:System.Diagnostics.Process.Start%2A> method to launch an executable named *Write500Lines.exe*. The following example contains its source code.
        /// [!code-csharp[Executable launched by Process.Start](~/samples/snippets/csharp/api/system.diagnostics/process/standardoutput/write500lines.cs)]
        /// [!code-vb[Executable launched by Process.Start](~/samples/snippets/visualbasic/api/system.diagnostics/process/standardoutput/write500lines.vb)]
        /// The following example shows how to read from a redirected stream and wait for the child process to exit. The example avoids a deadlock condition by calling `p.StandardOutput.ReadToEnd` before `p.WaitForExit`. A deadlock condition can result if the parent process calls `p.WaitForExit` before `p.StandardOutput.ReadToEnd` and the child process writes enough text to fill the redirected stream. The parent process would wait indefinitely for the child process to exit. The child process would wait indefinitely for the parent to read from the full <xref:System.Diagnostics.Process.StandardOutput%2A> stream.
        /// [!code-csharp[Reading synchronously from a redirected output stream](~/samples/snippets/csharp/api/system.diagnostics/process/standardoutput/stdoutput-sync.cs)]
        /// [!code-vb[Reading synchronously from a redirected output stream](~/samples/snippets/visualbasic/api/system.diagnostics/process/standardoutput/stdoutput-sync.vb)]
        /// There is a similar issue when you read all text from both the standard output and standard error streams. The following example performs a read operation on both streams. It avoids the deadlock condition by performing asynchronous read operations on the <xref:System.Diagnostics.Process.StandardError%2A> stream. A deadlock condition results if the parent process calls `p.StandardOutput.ReadToEnd` followed by `p.StandardError.ReadToEnd` and the child process writes enough text to fill its error stream. The parent process would wait indefinitely for the child process to close its <xref:System.Diagnostics.Process.StandardOutput%2A> stream. The child process would wait indefinitely for the parent to read from the full <xref:System.Diagnostics.Process.StandardError%2A> stream.
        /// [!code-csharp[Reading from a redirected output and error stream](~/samples/snippets/csharp/api/system.diagnostics/process/standardoutput/stdoutput-async.cs)]
        /// [!code-vb[Reading from a redirected output and error stream](~/samples/snippets/visualbasic/api/system.diagnostics/process/standardoutput/stdoutput-async.vb)]
        /// You can use asynchronous read operations to avoid these dependencies and their deadlock potential. Alternately, you can avoid the deadlock condition by creating two threads and reading the output of each stream on a separate thread.
        /// > [!NOTE]
        /// >  You cannot mix asynchronous and synchronous read operations on a redirected stream. Once the redirected stream of a <xref:System.Diagnostics.Process> is opened in either asynchronous or synchronous mode, all further read operations on that stream must be in the same mode. For example, do not follow <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> with a call to <xref:System.IO.StreamReader.ReadLine%2A> on the <xref:System.Diagnostics.Process.StandardOutput%2A> stream, or vice versa. However, you can read two different streams in different modes. For example, you can call <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> and then call <xref:System.IO.StreamReader.ReadLine%2A> for the <xref:System.Diagnostics.Process.StandardError%2A> stream.
        /// ## Examples
        /// The following example runs the ipconfig.exe command and redirects its standard output to the example's console window.
        /// [!code-cpp[Process_StandardOutput#2](~/samples/snippets/cpp/VS_Snippets_CLR/Process_StandardOutput/CPP/process_standardoutput.cpp)]
        /// [!code-csharp[Process_StandardOutput#2](~/samples/snippets/csharp/VS_Snippets_CLR/Process_StandardOutput/CS/process_standardoutput.cs)]
        /// [!code-vb[Process_StandardOutput#2](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process_StandardOutput/VB/process_standardoutput.vb)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Diagnostics.Process.StandardOutput" /> stream has not been defined for redirection; ensure <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput" /> is set to <see langword="true" /> and <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is set to <see langword="false" />.
        /// -or-
        /// The <see cref="System.Diagnostics.Process.StandardOutput" /> stream has been opened for asynchronous read operations with <see cref="System.Diagnostics.Process.BeginOutputReadLine" />.</exception>
        /// <altmember cref="System.Diagnostics.Process.StandardInput"/>
        /// <altmember cref="System.Diagnostics.Process.StandardError"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput"/>
        public StreamReader StandardOutput
        {
            get
            {
                CheckDisposed();
                if (_standardOutput == null)
                {
                    throw new InvalidOperationException(SR.CantGetStandardOut);
                }

                if (_outputStreamReadMode == StreamReadMode.Undefined)
                {
                    _outputStreamReadMode = StreamReadMode.SyncMode;
                }
                else if (_outputStreamReadMode != StreamReadMode.SyncMode)
                {
                    throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
                }

                return _standardOutput;
            }
        }

        /// <summary>Gets a stream used to read the error output of the application.</summary>
        /// <value>A <see cref="System.IO.StreamReader" /> that can be used to read the standard error stream of the application.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// When a <xref:System.Diagnostics.Process> writes text to its standard error stream, that text is normally displayed on the console. By redirecting the <xref:System.Diagnostics.Process.StandardError%2A> stream, you can manipulate or suppress the error output of a process. For example, you can filter the text, format it differently, or write the output to both the console and a designated log file.
        /// > [!NOTE]
        /// >  To use <xref:System.Diagnostics.Process.StandardError%2A>, you must set <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A?displayProperty=nameWithType> to `false`, and you must set <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardError%2A?displayProperty=nameWithType> to `true`. Otherwise, reading from the <xref:System.Diagnostics.Process.StandardError%2A> stream throws an exception.
        /// The redirected <xref:System.Diagnostics.Process.StandardError%2A> stream can be read synchronously or asynchronously. Methods such as <xref:System.IO.StreamReader.Read%2A>, <xref:System.IO.StreamReader.ReadLine%2A>, and <xref:System.IO.StreamReader.ReadToEnd%2A> perform synchronous read operations on the error output stream of the process. These synchronous read operations do not complete until the associated <xref:System.Diagnostics.Process> writes to its <xref:System.Diagnostics.Process.StandardError%2A> stream, or closes the stream.
        /// In contrast, <xref:System.Diagnostics.Process.BeginErrorReadLine%2A> starts asynchronous read operations on the <xref:System.Diagnostics.Process.StandardError%2A> stream. This method enables a designated event handler for the stream output and immediately returns to the caller, which can perform other work while the stream output is directed to the event handler.
        /// Synchronous read operations introduce a dependency between the caller reading from the <xref:System.Diagnostics.Process.StandardError%2A> stream and the child process writing to that stream. These dependencies can result in deadlock conditions. When the caller reads from the redirected stream of a child process, it is dependent on the child. The caller waits on the read operation until the child writes to the stream or closes the stream. When the child process writes enough data to fill its redirected stream, it is dependent on the parent. The child process waits on the next write operation until the parent reads from the full stream or closes the stream. The deadlock condition results when the caller and child process wait on each other to complete an operation, and neither can proceed. You can avoid deadlocks by evaluating dependencies between the caller and child process.
        /// The last two examples in this section use the <xref:System.Diagnostics.Process.Start%2A> method to launch an executable named *Write500Lines.exe*. The following example contains its source code.
        /// [!code-csharp[Executable launched by Process.Start](~/samples/snippets/csharp/api/system.diagnostics/process/standardoutput/write500lines.cs)]
        /// [!code-vb[Executable launched by Process.Start](~/samples/snippets/visualbasic/api/system.diagnostics/process/standardoutput/write500lines.vb)]
        /// The following example shows how to read from a redirected error stream and wait for the child process to exit. It avoids a deadlock condition by calling `p.StandardError.ReadToEnd` before `p.WaitForExit`. A deadlock condition can result if the parent process calls `p.WaitForExit` before `p.StandardError.ReadToEnd` and the child process writes enough text to fill the redirected stream. The parent process would wait indefinitely for the child process to exit. The child process would wait indefinitely for the parent to read from the full <xref:System.Diagnostics.Process.StandardError%2A> stream.
        /// [!code-csharp[Reading from the error stream](~/samples/snippets/csharp/api/system.diagnostics/process/standarderror/stderror-sync.cs)]
        /// [!code-vb[Reading from the error stream](~/samples/snippets/visualbasic/api/system.diagnostics/process/standarderror/stderror-sync.vb)]
        /// There is a similar issue when you read all text from both the standard output and standard error streams. The following example performs a read operation on both streams. It avoids the deadlock condition by performing asynchronous read operations on the <xref:System.Diagnostics.Process.StandardError%2A> stream. A deadlock condition results if the parent process calls `p.StandardOutput.ReadToEnd` followed by `p.StandardError.ReadToEnd` and the child process writes enough text to fill its error stream. The parent process would wait indefinitely for the child process to close its <xref:System.Diagnostics.Process.StandardOutput%2A> stream. The child process would wait indefinitely for the parent to read from the full <xref:System.Diagnostics.Process.StandardError%2A> stream.
        /// [!code-csharp[Reading from both streams](~/samples/snippets/csharp/api/system.diagnostics/process/standardoutput/stdoutput-async.cs)]
        /// [!code-vb[Reading from both streams](~/samples/snippets/visualbasic/api/system.diagnostics/process/standardoutput/stdoutput-async.vb)]
        /// You can use asynchronous read operations to avoid these dependencies and their deadlock potential. Alternately, you can avoid the deadlock condition by creating two threads and reading the output of each stream on a separate thread.
        /// > [!NOTE]
        /// >  You cannot mix asynchronous and synchronous read operations on a redirected stream. Once the redirected stream of a <xref:System.Diagnostics.Process> is opened in either asynchronous or synchronous mode, all further read operations on that stream must be in the same mode. For example, do not follow <xref:System.Diagnostics.Process.BeginErrorReadLine%2A> with a call to <xref:System.IO.StreamReader.ReadLine%2A> on the <xref:System.Diagnostics.Process.StandardError%2A> stream, or vice versa. However, you can read two different streams in different modes. For example, you can call <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> and then call <xref:System.IO.StreamReader.ReadLine%2A> for the <xref:System.Diagnostics.Process.StandardError%2A> stream.
        /// ## Examples
        /// The following example uses the `net use` command together with a user supplied argument to map a network resource. It then reads the standard error stream of the net command and writes it to console.
        /// [!code-cpp[Process_StandardError#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process_StandardError/CPP/source.cpp#1)]
        /// [!code-csharp[Process_StandardError#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process_StandardError/CS/source.cs#1)]
        /// [!code-vb[Process_StandardError#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process_StandardError/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Diagnostics.Process.StandardError" /> stream has not been defined for redirection; ensure <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError" /> is set to <see langword="true" /> and <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is set to <see langword="false" />.
        /// -or-
        /// The <see cref="System.Diagnostics.Process.StandardError" /> stream has been opened for asynchronous read operations with <see cref="System.Diagnostics.Process.BeginErrorReadLine" />.</exception>
        /// <altmember cref="System.Diagnostics.Process.StandardInput"/>
        /// <altmember cref="System.Diagnostics.Process.StandardOutput"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError"/>
        public StreamReader StandardError
        {
            get
            {
                CheckDisposed();
                if (_standardError == null)
                {
                    throw new InvalidOperationException(SR.CantGetStandardError);
                }

                if (_errorStreamReadMode == StreamReadMode.Undefined)
                {
                    _errorStreamReadMode = StreamReadMode.SyncMode;
                }
                else if (_errorStreamReadMode != StreamReadMode.SyncMode)
                {
                    throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
                }

                return _standardError;
            }
        }

        /// <summary>Gets the amount of physical memory, in bytes, allocated for the associated process.</summary>
        /// <value>The amount of physical memory, in bytes, allocated for the associated process.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the most recently refreshed size of working set memory used by the process, in bytes. To get the most up to date size, you need to call <xref:System.Diagnostics.Process.Refresh> method first.
        /// The working set of a process is the set of memory pages currently visible to the process in physical RAM memory. These pages are resident and available for an application to use without triggering a page fault.
        /// The working set includes both shared and private data. The shared data includes the pages that contain all the instructions that the process executes, including instructions in the process modules and the system libraries.
        /// This property can be used to monitor memory usage on computers with 32-bit processors or 64-bit processors. The property value is equivalent to the **Working Set** performance counter for the process.
        /// ## Examples
        /// The following code example starts an instance of the Notepad application. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays its exit code and peak memory statistics.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.MinWorkingSet"/>
        /// <altmember cref="System.Diagnostics.Process.MaxWorkingSet"/>
        /// <altmember cref="System.Diagnostics.Process.PeakWorkingSet64"/>
        public long WorkingSet64
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo!.WorkingSet;
            }
        }

        /// <summary>Gets the associated process's physical memory usage, in bytes.</summary>
        /// <value>The total amount of physical memory the associated process is using, in bytes.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The value returned by this property represents the most recently refreshed size of working set memory used by the process, in bytes. To get the most up to date size, you need to call <xref:System.Diagnostics.Process.Refresh> method first.
        /// The working set of a process is the set of memory pages currently visible to the process in physical RAM memory. These pages are resident and available for an application to use without triggering a page fault.
        /// The working set includes both shared and private data. The shared data includes the pages that contain all the instructions that the process executes, including the process modules and the system libraries.
        /// ## Examples
        /// The following example starts an instance of Notepad. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays the process' exit code.
        /// [!code-cpp[process_sample#1](~/samples/snippets/cpp/VS_Snippets_CLR/process_sample/CPP/process_sample.cpp#1)]
        /// [!code-csharp[process_sample#1](~/samples/snippets/csharp/VS_Snippets_CLR/process_sample/CS/process_sample.cs#1)]
        /// [!code-vb[process_sample#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_sample/VB/process_sample.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.MinWorkingSet"/>
        /// <altmember cref="System.Diagnostics.Process.MaxWorkingSet"/>
        /// <altmember cref="System.Diagnostics.Process.PeakWorkingSet"/>
        /// <altmember cref="System.Diagnostics.Process.PeakWorkingSet64"/>
        /// <altmember cref="System.Diagnostics.Process.WorkingSet64"/>
        [ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.WorkingSet64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int WorkingSet
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return unchecked((int)_processInfo!.WorkingSet);
            }
        }

        public event EventHandler Exited
        {
            add
            {
                _onExited += value;
            }
            remove
            {
                _onExited -= value;
            }
        }

        /// <devdoc>
        ///     This is called from the threadpool when a process exits.
        /// </devdoc>
        /// <internalonly/>
        private void CompletionCallback(object? waitHandleContext, bool wasSignaled)
        {
            Debug.Assert(waitHandleContext != null, "Process.CompletionCallback called with no waitHandleContext");
            lock (this)
            {
                // Check the exited event that we get from the threadpool
                // matches the event we are waiting for.
                if (waitHandleContext != _waitHandle)
                {
                    return;
                }
                StopWatchingForExit();
                RaiseOnExited();
            }
        }

        /// <summary>Release all resources used by this process.</summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //Dispose managed and unmanaged resources
                    Close();
                }
                _disposed = true;
            }
        }

        /// <summary>Closes a process that has a user interface by sending a close message to its main window.</summary>
        /// <returns><see langword="true" /> if the close message was successfully sent; <see langword="false" /> if the associated process does not have a main window or if the main window is disabled (for example if a modal dialog is being shown).</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// When a process is executing, its message loop is in a wait state. The message loop executes every time a Windows message is sent to the process by the operating system. Calling <xref:System.Diagnostics.Process.CloseMainWindow%2A> sends a request to close the main window, which, in a well-formed application, closes child windows and revokes all running message loops for the application. The request to exit the process by calling <xref:System.Diagnostics.Process.CloseMainWindow%2A> does not force the application to quit. The application can ask for user verification before quitting, or it can refuse to quit. To force the application to quit, use the <xref:System.Diagnostics.Process.Kill%2A> method. The behavior of <xref:System.Diagnostics.Process.CloseMainWindow%2A> is identical to that of a user closing an application's main window using the system menu. Therefore, the request to exit the process by closing the main window does not force the application to quit immediately.
        /// Data edited by the process or resources allocated to the process can be lost if you call <xref:System.Diagnostics.Process.Kill%2A>. <xref:System.Diagnostics.Process.Kill%2A> causes an abnormal process termination, and should be used only when necessary. <xref:System.Diagnostics.Process.CloseMainWindow%2A> enables an orderly termination of the process and closes all windows, so it is preferable for applications with an interface. If <xref:System.Diagnostics.Process.CloseMainWindow%2A> fails, you can use <xref:System.Diagnostics.Process.Kill%2A> to terminate the process. <xref:System.Diagnostics.Process.Kill%2A> is the only way to terminate processes that do not have graphical interfaces.
        /// You can call <xref:System.Diagnostics.Process.Kill%2A> and <xref:System.Diagnostics.Process.CloseMainWindow%2A> only for processes that are running on the local computer. You cannot cause processes on remote computers to exit. You can only view information for processes running on remote computers.
        /// ## Examples
        /// The following example starts an instance of Notepad. It then retrieves the physical memory usage of the associated process at 2 second intervals for a maximum of 10 seconds. The example detects whether the process exits before 10 seconds have elapsed. The example closes the process if it is still running after 10 seconds.
        /// [!code-cpp[process_refresh#1](~/samples/snippets/cpp/VS_Snippets_CLR/process_refresh/CPP/process_refresh.cpp#1)]
        /// [!code-csharp[process_refresh#1](~/samples/snippets/csharp/VS_Snippets_CLR/process_refresh/CS/process_refresh.cs#1)]
        /// [!code-vb[process_refresh#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_refresh/VB/process_refresh.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The process has already exited.
        /// -or-
        /// No process is associated with this <see cref="System.Diagnostics.Process" /> object.</exception>
        public bool CloseMainWindow()
        {
            return CloseMainWindowCore();
        }

        /// <summary>Causes the <see cref="System.Diagnostics.Process" /> component to wait indefinitely for the associated process to enter an idle state. This overload applies only to processes with a user interface and, therefore, a message loop.</summary>
        /// <returns><see langword="true" /> if the associated process has reached an idle state.</returns>
        /// <remarks>Use <see cref="O:System.Diagnostics.Process.WaitForInputIdle" /> to force the processing of your application to wait until the message loop has returned to the idle state. When a process with a user interface is executing, its message loop executes every time a Windows message is sent to the process by the operating system. The process then returns to the message loop. A process is said to be in an idle state when it is waiting for messages inside of a message loop. This state is useful, for example, when your application needs to wait for a starting process to finish creating its main window before the application communicates with that window.
        /// If a process does not have a message loop, <see cref="O:System.Diagnostics.Process.WaitForInputIdle" /> throws an <see cref="System.InvalidOperationException" />.
        /// The <see cref="O:System.Diagnostics.Process.WaitForInputIdle" /> overload instructs the <see cref="System.Diagnostics.Process" /> component to wait indefinitely for the process to become idle in the message loop. This instruction can cause an application to stop responding. For example, if the process is written to always exit its message loop immediately, as in the code fragment `while(true)`.</remarks>
        /// <exception cref="System.InvalidOperationException">The process does not have a graphical interface.
        /// -or-
        /// An unknown error occurred. The process failed to enter an idle state.
        /// -or-
        /// The process has already exited.
        /// -or-
        /// No process is associated with this <see cref="System.Diagnostics.Process" /> object.</exception>
        /// <altmember cref="O:System.Diagnostics.Process.Start"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        public bool WaitForInputIdle()
        {
            return WaitForInputIdle(int.MaxValue);
        }

        /// <summary>Causes the <see cref="System.Diagnostics.Process" /> component to wait the specified number of milliseconds for the associated process to enter an idle state. This overload applies only to processes with a user interface and, therefore, a message loop.</summary>
        /// <param name="milliseconds">A value of 1 to <see cref="int.MaxValue" /> that specifies the amount of time, in milliseconds, to wait for the associated process to become idle. A value of 0 specifies an immediate return, and a value of -1 specifies an infinite wait.</param>
        /// <returns><see langword="true" /> if the associated process has reached an idle state; otherwise, <see langword="false" />.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use <xref:System.Diagnostics.Process.WaitForInputIdle%28int%29> to force the processing of your application to wait until the message loop has returned to the idle state. When a process with a user interface is executing, its message loop executes every time a Windows message is sent to the process by the operating system. The process then returns to the message loop. A process is said to be in an idle state when it is waiting for messages inside of a message loop. This state is useful, for example, when your application needs to wait for a starting process to finish creating its main window before the application communicates with that window.
        /// If a process does not have a message loop, <xref:System.Diagnostics.Process.WaitForInputIdle%28int%29> throws an <xref:System.InvalidOperationException>.
        /// The <xref:System.Diagnostics.Process.WaitForInputIdle%28int%29> overload instructs the <xref:System.Diagnostics.Process> component to wait a finite amount of time for the process to become idle in the message loop. If the associated process has not become idle by the end of the interval because the loop is still processing messages, `false` is returned to the calling procedure.
        /// For more information about handling events, see [Handling and Raising Events](/dotnet/standard/events/).
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The process does not have a graphical interface.
        /// -or-
        /// An unknown error occurred. The process failed to enter an idle state.
        /// -or-
        /// The process has already exited.
        /// -or-
        /// No process is associated with this <see cref="System.Diagnostics.Process" /> object.</exception>
        /// <altmember cref="O:System.Diagnostics.Process.Start"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        public bool WaitForInputIdle(int milliseconds)
        {
            return WaitForInputIdleCore(milliseconds);
        }

        /// <summary>Gets or sets the object used to marshal the event handler calls that are issued as a result of a process exit event.</summary>
        /// <value>The <see cref="System.ComponentModel.ISynchronizeInvoke" /> used to marshal event handler calls that are issued as a result of an <see cref="System.Diagnostics.Process.Exited" /> event on the process.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// When <xref:System.Diagnostics.EventLog.SynchronizingObject%2A> is `null`, methods that handle the <xref:System.Diagnostics.Process.Exited> event are called on a thread from the system thread pool. For more information about system thread pools, see <xref:System.Threading.ThreadPool>.
        /// When the <xref:System.Diagnostics.Process.Exited> event is handled by a visual Windows Forms component, such as a <xref:System.Windows.Forms.Button>, accessing the component through the system thread pool might not work, or might result in an exception. Avoid this by setting <xref:System.Diagnostics.Process.SynchronizingObject%2A> to a Windows Forms component, which causes the methods handling the <xref:System.Diagnostics.Process.Exited> event to be called on the same thread on which the component was created.
        /// If the <xref:System.Diagnostics.Process> is used inside [!INCLUDE[vsprvslong](~/includes/vsprvslong-md.md)] in a Windows Forms designer, <xref:System.Diagnostics.Process.SynchronizingObject%2A> is automatically set to the control that contains the <xref:System.Diagnostics.Process>. For example, if you place a <xref:System.Diagnostics.Process> on a designer for `Form1` (which inherits from <xref:System.Windows.Forms.Form>) the <xref:System.Diagnostics.Process.SynchronizingObject%2A> property of <xref:System.Diagnostics.Process> is set to the instance of `Form1`:
        /// [!code-cpp[Process_SynchronizingObject#2](~/samples/snippets/cpp/VS_Snippets_CLR/Process_SynchronizingObject/CPP/remarks.cpp#2)]
        /// [!code-csharp[Process_SynchronizingObject#2](~/samples/snippets/csharp/VS_Snippets_CLR/Process_SynchronizingObject/CS/remarks.cs#2)]
        /// [!code-vb[Process_SynchronizingObject#2](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process_SynchronizingObject/VB/remarks.vb#2)]
        /// Typically, this property is set when the component is placed inside a control or form, because those components are bound to a specific thread.
        /// ## Examples
        /// [!code-cpp[Process_SynchronizingObject#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process_SynchronizingObject/CPP/process_synchronizingobject.cpp#1)]
        /// [!code-csharp[Process_SynchronizingObject#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process_SynchronizingObject/CS/process_synchronizingobject.cs#1)]
        /// [!code-vb[Process_SynchronizingObject#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process_SynchronizingObject/VB/process_synchronizingobject.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.SynchronizingObject"/>
        public ISynchronizeInvoke? SynchronizingObject { get; set; }

        /// <summary>Frees all the resources that are associated with this component.</summary>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Diagnostics.Process.Close%2A> method causes the process to stop waiting for exit if it was waiting, closes the process handle, and clears process-specific properties. <xref:System.Diagnostics.Process.Close%2A> does not close the standard output, input, and error readers and writers in case they are being referenced externally.
        /// > [!NOTE]
        /// >  The <xref:System.Diagnostics.Process.Dispose%2A> method calls <xref:System.Diagnostics.Process.Close%2A>. Placing the <xref:System.Diagnostics.Process> object in a `using` block disposes of resources without the need to call <xref:System.Diagnostics.Process.Close%2A>.
        /// ## Examples
        /// The following example starts an instance of Notepad. It then retrieves the physical memory usage of the associated process at 2-second intervals for a maximum of 10 seconds. The example detects whether the process exits before 10 seconds have elapsed. The example closes the process if it is still running after 10 seconds.
        /// [!code-cpp[process_refresh#1](~/samples/snippets/cpp/VS_Snippets_CLR/process_refresh/CPP/process_refresh.cpp#1)]
        /// [!code-csharp[process_refresh#1](~/samples/snippets/csharp/VS_Snippets_CLR/process_refresh/CS/process_refresh.cs#1)]
        /// [!code-vb[process_refresh#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_refresh/VB/process_refresh.vb#1)]
        /// ]]></format></remarks>
        public void Close()
        {
            if (Associated)
            {
                // We need to lock to ensure we don't run concurrently with CompletionCallback.
                // Without this lock we could reset _raisedOnExited which causes CompletionCallback to
                // raise the Exited event a second time for the same process.
                lock (this)
                {
                    // This sets _waitHandle to null which causes CompletionCallback to not emit events.
                    StopWatchingForExit();
                }

                if (_haveProcessHandle)
                {
                    _processHandle!.Dispose();
                    _processHandle = null;
                    _haveProcessHandle = false;
                }
                _haveProcessId = false;
                _isRemoteMachine = false;
                _machineName = ".";
                _raisedOnExited = false;

                // Only call close on the streams if the user cannot have a reference on them.
                // If they are referenced it is the user's responsibility to dispose of them.
                try
                {
                    if (_standardOutput != null && (_outputStreamReadMode == StreamReadMode.AsyncMode || _outputStreamReadMode == StreamReadMode.Undefined))
                    {
                        if (_outputStreamReadMode == StreamReadMode.AsyncMode)
                        {
                            _output?.CancelOperation();
                            _output?.Dispose();
                        }
                        _standardOutput.Close();
                    }

                    if (_standardError != null && (_errorStreamReadMode == StreamReadMode.AsyncMode || _errorStreamReadMode == StreamReadMode.Undefined))
                    {
                        if (_errorStreamReadMode == StreamReadMode.AsyncMode)
                        {
                            _error?.CancelOperation();
                            _error?.Dispose();
                        }
                        _standardError.Close();
                    }

                    if (_standardInput != null && !_standardInputAccessed)
                    {
                        _standardInput.Close();
                    }
                }
                finally
                {
                    _standardOutput = null;
                    _standardInput = null;
                    _standardError = null;

                    _output = null;
                    _error = null;

                    CloseCore();
                    Refresh();
                }
            }
        }

        // Checks if the process hasn't exited on Unix systems.
        // This is used to detect recycled child PIDs.
        partial void ThrowIfExited(bool refresh);

        /// <devdoc>
        ///     Helper method for checking preconditions when accessing properties.
        /// </devdoc>
        /// <internalonly/>
        private void EnsureState(State state)
        {
            if ((state & State.Associated) != (State)0)
                if (!Associated)
                    throw new InvalidOperationException(SR.NoAssociatedProcess);

            if ((state & State.HaveId) != (State)0)
            {
                if (!_haveProcessId)
                {
                    if (_haveProcessHandle)
                    {
                        SetProcessId(ProcessManager.GetProcessIdFromHandle(_processHandle!));
                    }
                    else
                    {
                        EnsureState(State.Associated);
                        throw new InvalidOperationException(SR.ProcessIdRequired);
                    }
                }
                if ((state & State.HaveNonExitedId) == State.HaveNonExitedId)
                {
                    ThrowIfExited(refresh: false);
                }
            }

            if ((state & State.IsLocal) != (State)0 && _isRemoteMachine)
            {
                throw new NotSupportedException(SR.NotSupportedRemote);
            }

            if ((state & State.HaveProcessInfo) != (State)0)
            {
                if (_processInfo == null)
                {
                    if ((state & State.HaveNonExitedId) != State.HaveNonExitedId)
                    {
                        EnsureState(State.HaveNonExitedId);
                    }
                    _processInfo = ProcessManager.GetProcessInfo(_processId, _machineName);
                    if (_processInfo == null)
                    {
                        throw new InvalidOperationException(SR.NoProcessInfo);
                    }
                }
            }

            if ((state & State.Exited) != (State)0)
            {
                if (!HasExited)
                {
                    throw new InvalidOperationException(SR.WaitTillExit);
                }

                if (!_haveProcessHandle)
                {
                    throw new InvalidOperationException(SR.NoProcessHandle);
                }
            }
        }

        /// <devdoc>
        ///     Make sure we have obtained the min and max working set limits.
        /// </devdoc>
        /// <internalonly/>
        private void EnsureWorkingSetLimits()
        {
            if (!_haveWorkingSetLimits)
            {
                GetWorkingSetLimits(out _minWorkingSet, out _maxWorkingSet);
                _haveWorkingSetLimits = true;
            }
        }

        /// <devdoc>
        ///     Helper to set minimum or maximum working set limits.
        /// </devdoc>
        /// <internalonly/>
        private void SetWorkingSetLimits(IntPtr? min, IntPtr? max)
        {
            SetWorkingSetLimitsCore(min, max, out _minWorkingSet, out _maxWorkingSet);
            _haveWorkingSetLimits = true;
        }

        /// <summary>Returns a new <see cref="System.Diagnostics.Process" /> component, given a process identifier and the name of a computer on the network.</summary>
        /// <param name="processId">The system-unique identifier of a process resource.</param>
        /// <param name="machineName">The name of a computer on the network.</param>
        /// <returns>A <see cref="System.Diagnostics.Process" /> component that is associated with a remote process resource identified by the <paramref name="processId" /> parameter.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use this method to create a new <xref:System.Diagnostics.Process> component and associate it with a process resource on a remote computer on the network. The process resource must already exist on the specified computer, because <xref:System.Diagnostics.Process.GetProcessById%28int%2Cstring%29> does not create a system resource, but rather associates a resource with an application-generated <xref:System.Diagnostics.Process> component. A process <xref:System.Diagnostics.Process.Id%2A> can be retrieved only for a process that is currently running on the computer. After the process terminates, <xref:System.Diagnostics.Process.GetProcessById%28int%2Cstring%29> throws an exception if you pass it an expired identifier.
        /// On any particular computer, the identifier of a process is unique. <xref:System.Diagnostics.Process.GetProcessById%28int%2Cstring%29> returns one process at most. If you want to get all the processes running a particular application, use <xref:System.Diagnostics.Process.GetProcessesByName%28string%29>. If multiple processes exist on the computer running the specified application, <xref:System.Diagnostics.Process.GetProcessesByName%28string%29> returns an array containing all the associated processes. You can query each of these processes in turn for its identifier. The process identifier can be viewed in the `Processes` panel of the Windows Task Manager. The `PID` column displays the process identifier that is assigned to a process.
        /// If you do not specify a `machineName`, the local computer is used. Alternatively, you can specify the local computer by setting `machineName` to the value "." or to an empty string ("").
        /// The `processId` parameter is an <xref:int> (a 32-bit signed integer), although the underlying Windows API uses a `DWORD` (an unsigned 32-bit integer) for similar APIs. This is for historical reasons.
        /// ## Examples
        /// The following example retrieves information of the current process, processes running on the local computer, all instances of Notepad running on the local computer, and a specific process on the local computer. It then retrieves information for the same processes on a remote computer.
        /// [!code-cpp[Process.GetProcesses_noexception#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.GetProcesses_noexception/CPP/processstaticget.cpp#1)]
        /// [!code-csharp[Process.GetProcesses_noexception#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.GetProcesses_noexception/CS/processstaticget.cs#1)]
        /// [!code-vb[Process.GetProcesses_noexception#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.GetProcesses_noexception/VB/processstaticget.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException">The process specified by the <paramref name="processId" /> parameter is not running. The identifier might be expired.
        /// -or-
        /// The <paramref name="machineName" /> parameter syntax is invalid. The name might have length zero (0).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="machineName" /> parameter is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The process was not started by this object.</exception>
        /// <altmember cref="System.Diagnostics.Process.Id"/>
        /// <altmember cref="System.Diagnostics.Process.MachineName"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessesByName(string)"/>
        /// <altmember cref="O:System.Diagnostics.Process.GetProcesses"/>
        /// <altmember cref="System.Diagnostics.Process.GetCurrentProcess"/>
        public static Process GetProcessById(int processId, string machineName)
        {
            if (!ProcessManager.IsProcessRunning(processId, machineName))
            {
                throw new ArgumentException(SR.Format(SR.MissingProccess, processId.ToString()));
            }

            return new Process(machineName, ProcessManager.IsRemoteMachine(machineName), processId, null);
        }

        /// <summary>Returns a new <see cref="System.Diagnostics.Process" /> component, given the identifier of a process on the local computer.</summary>
        /// <param name="processId">The system-unique identifier of a process resource.</param>
        /// <returns>A <see cref="System.Diagnostics.Process" /> component that is associated with the local process resource identified by the <paramref name="processId" /> parameter.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use this method to create a new <xref:System.Diagnostics.Process> component and associate it with a process resource on the local computer. The process resource must already exist on the computer, because <xref:System.Diagnostics.Process.GetProcessById%28int%29> does not create a system resource, but rather associates a resource with an application-generated <xref:System.Diagnostics.Process> component. A process <xref:System.Diagnostics.Process.Id%2A> can be retrieved only for a process that is currently running on the computer. After the process terminates, <xref:System.Diagnostics.Process.GetProcessById%28int%29> throws an exception if you pass it an expired identifier.
        /// On any particular computer, the identifier of a process is unique. <xref:System.Diagnostics.Process.GetProcessById%28int%29> returns one process at most. If you want to get all the processes running a particular application, use <xref:System.Diagnostics.Process.GetProcessesByName%28string%29>. If multiple processes exist on the computer running the specified application, <xref:System.Diagnostics.Process.GetProcessesByName%28string%29> returns an array containing all the associated processes. You can query each of these processes in turn for its identifier. The process identifier can be viewed in the `Processes` panel of the Windows Task Manager. The `PID` column displays the process identifier that is assigned to a process.
        /// The `processId` parameter is an <xref:int> (a 32-bit signed integer), although the underlying Windows API uses a `DWORD` (an unsigned 32-bit integer) for similar APIs. This is for historical reasons.
        /// ## Examples
        /// The following example retrieves information of the current process, processes running on the local computer, all instances of Notepad running on the local computer, and a specific process on the local computer. It then retrieves information for the same processes on a remote computer.
        /// [!code-cpp[Process.GetProcesses_noexception#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.GetProcesses_noexception/CPP/processstaticget.cpp#1)]
        /// [!code-csharp[Process.GetProcesses_noexception#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.GetProcesses_noexception/CS/processstaticget.cs#1)]
        /// [!code-vb[Process.GetProcesses_noexception#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.GetProcesses_noexception/VB/processstaticget.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException">The process specified by the <paramref name="processId" /> parameter is not running. The identifier might be expired.</exception>
        /// <exception cref="System.InvalidOperationException">The process was not started by this object.</exception>
        /// <altmember cref="System.Diagnostics.Process.Id"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessesByName(string)"/>
        /// <altmember cref="O:System.Diagnostics.Process.GetProcesses"/>
        /// <altmember cref="System.Diagnostics.Process.GetCurrentProcess"/>
        public static Process GetProcessById(int processId)
        {
            return GetProcessById(processId, ".");
        }

        /// <summary>Creates an array of new <see cref="System.Diagnostics.Process" /> components and associates them with all the process resources on the local computer that share the specified process name.</summary>
        /// <param name="processName">The friendly name of the process.</param>
        /// <returns>An array of type <see cref="System.Diagnostics.Process" /> that represents the process resources running the specified application or file.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use this method to create an array of new <xref:System.Diagnostics.Process> components and associate them with all the process resources that are running the same executable file on the local computer. The process resources must already exist on the computer, because <xref:System.Diagnostics.Process.GetProcessesByName%2A> does not create system resources but rather associates them with application-generated <xref:System.Diagnostics.Process> components. A `processName` can be specified for an executable file that is not currently running on the local computer, so the array the method returns can be empty.
        /// The process name is a friendly name for the process, such as Outlook, that does not include the .exe extension or the path. <xref:System.Diagnostics.Process.GetProcessesByName%2A> is helpful for getting and manipulating all the processes that are associated with the same executable file. For example, you can pass an executable file name as the `processName` parameter, in order to shut down all the running instances of that executable file.
        /// Although a process <xref:System.Diagnostics.Process.Id%2A> is unique to a single process resource on the system, multiple processes on the local computer can be running the application specified by the `processName` parameter. Therefore, <xref:System.Diagnostics.Process.GetProcessById%2A> returns one process at most, but <xref:System.Diagnostics.Process.GetProcessesByName%2A> returns an array containing all the associated processes. If you need to manipulate the process using standard API calls, you can query each of these processes in turn for its identifier. You cannot access process resources through the process name alone but, once you have retrieved an array of <xref:System.Diagnostics.Process> components that have been associated with the process resources, you can start, terminate, and otherwise manipulate the system resources.
        /// ## Examples
        /// The following example retrieves information of the current process, processes running on the local computer, all instances of Notepad running on the local computer, and a specific process on the local computer. It then retrieves information for the same processes on a remote computer.
        /// [!code-cpp[Process.GetProcesses_noexception#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.GetProcesses_noexception/CPP/processstaticget.cpp#1)]
        /// [!code-csharp[Process.GetProcesses_noexception#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.GetProcesses_noexception/CS/processstaticget.cs#1)]
        /// [!code-vb[Process.GetProcesses_noexception#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.GetProcesses_noexception/VB/processstaticget.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">There are problems accessing the performance counter APIs used to get process information. This exception is specific to Windows NT, Windows 2000, and Windows XP.</exception>
        /// <altmember cref="System.Diagnostics.Process.ProcessName"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessById(int,string)"/>
        /// <altmember cref="O:System.Diagnostics.Process.GetProcesses"/>
        /// <altmember cref="System.Diagnostics.Process.GetCurrentProcess"/>
        public static Process[] GetProcessesByName(string? processName)
        {
            return GetProcessesByName(processName, ".");
        }

        /// <summary>Creates a new <see cref="System.Diagnostics.Process" /> component for each process resource on the local computer.</summary>
        /// <returns>An array of type <see cref="System.Diagnostics.Process" /> that represents all the process resources running on the local computer.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use this method to create an array of new <xref:System.Diagnostics.Process> components and associate them with all the process resources on the local computer. The process resources must already exist on the local computer, because <xref:System.Diagnostics.Process.GetProcesses%2A> does not create system resources but rather associates resources with application-generated <xref:System.Diagnostics.Process> components. Because the operating system itself is running background processes, this array is never empty.
        /// If you do not want to retrieve all the processes running on the computer, you can restrict their number by using the <xref:System.Diagnostics.Process.GetProcessById%2A> or <xref:System.Diagnostics.Process.GetProcessesByName%2A> method. <xref:System.Diagnostics.Process.GetProcessById%2A> creates a <xref:System.Diagnostics.Process> component that is associated with the process identified on the system by the process identifier that you pass to the method. <xref:System.Diagnostics.Process.GetProcessesByName%2A> creates an array of <xref:System.Diagnostics.Process> components whose associated process resources share the executable file you pass to the method.
        /// > [!NOTE]
        /// >  Multiple Windows services can be loaded within the same instance of the Service Host process (svchost.exe). GetProcesses does not identify those individual services; for that, see <xref:System.ServiceProcess.ServiceController.GetServices%2A>.
        /// ## Examples
        /// The following example retrieves information of the current process, processes running on the local computer, all instances of Notepad running on the local computer, and a specific process on the local computer. It then retrieves information for the same processes on a remote computer.
        /// [!code-cpp[Process.GetProcesses_noexception#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.GetProcesses_noexception/CPP/processstaticget.cpp#1)]
        /// [!code-csharp[Process.GetProcesses_noexception#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.GetProcesses_noexception/CS/processstaticget.cs#1)]
        /// [!code-vb[Process.GetProcesses_noexception#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.GetProcesses_noexception/VB/processstaticget.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.MachineName"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessById(int,string)"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessesByName(string)"/>
        /// <altmember cref="System.Diagnostics.Process.GetCurrentProcess"/>
        public static Process[] GetProcesses()
        {
            return GetProcesses(".");
        }

        /// <summary>Creates a new <see cref="System.Diagnostics.Process" /> component for each process resource on the specified computer.</summary>
        /// <param name="machineName">The computer from which to read the list of processes.</param>
        /// <returns>An array of type <see cref="System.Diagnostics.Process" /> that represents all the process resources running on the specified computer.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use this method to create an array of new <xref:System.Diagnostics.Process> components and associate them with all the process resources on the specified (usually remote) computer. The process resources must already exist on the local computer, because <xref:System.Diagnostics.Process.GetProcesses%2A> does not create system resources but rather associates resources with application-generated <xref:System.Diagnostics.Process> components. Because the operating system itself is running background processes, this array is never empty.
        /// If you do not want to retrieve all the processes running on the computer, you can restrict their number by using the <xref:System.Diagnostics.Process.GetProcessById%2A> or <xref:System.Diagnostics.Process.GetProcessesByName%2A> method. <xref:System.Diagnostics.Process.GetProcessById%2A> creates a <xref:System.Diagnostics.Process> component that is associated with the process identified on the system by the process identifier that you pass to the method. <xref:System.Diagnostics.Process.GetProcessesByName%2A> creates an array of <xref:System.Diagnostics.Process> components whose associated process resources share the executable file you pass to the method.
        /// This overload of the <xref:System.Diagnostics.Process.GetProcesses%2A> method is generally used to retrieve the list of process resources running on a remote computer on the network, but you can specify the local computer by passing ".".
        /// > [!NOTE]
        /// >  Multiple Windows services can be loaded within the same instance of the Service Host process (svchost.exe). GetProcesses does not identify those individual services; for that, see <xref:System.ServiceProcess.ServiceController.GetServices%2A>.
        /// ## Examples
        /// The following example retrieves information of the current process, processes running on the local computer, all instances of Notepad running on the local computer, and a specific process on the local computer. It then retrieves information for the same processes on a remote computer.
        /// [!code-cpp[Process.GetProcesses_noexception#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.GetProcesses_noexception/CPP/processstaticget.cpp#1)]
        /// [!code-csharp[Process.GetProcesses_noexception#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.GetProcesses_noexception/CS/processstaticget.cs#1)]
        /// [!code-vb[Process.GetProcesses_noexception#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.GetProcesses_noexception/VB/processstaticget.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException">The <paramref name="machineName" /> parameter syntax is invalid. It might have length zero (0).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="machineName" /> parameter is <see langword="null" />.</exception>
        /// <exception cref="System.PlatformNotSupportedException">The operating system platform does not support this operation on remote computers.</exception>
        /// <exception cref="System.InvalidOperationException">There are problems accessing the performance counter APIs used to get process information. This exception is specific to Windows NT, Windows 2000, and Windows XP.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">A problem occurred accessing an underlying system API.</exception>
        /// <altmember cref="System.Diagnostics.Process.MachineName"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessById(int,string)"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessesByName(string)"/>
        /// <altmember cref="System.Diagnostics.Process.GetCurrentProcess"/>
        public static Process[] GetProcesses(string machineName)
        {
            bool isRemoteMachine = ProcessManager.IsRemoteMachine(machineName);
            ProcessInfo[] processInfos = ProcessManager.GetProcessInfos(machineName);
            Process[] processes = new Process[processInfos.Length];
            for (int i = 0; i < processInfos.Length; i++)
            {
                ProcessInfo processInfo = processInfos[i];
                processes[i] = new Process(machineName, isRemoteMachine, processInfo.ProcessId, processInfo);
            }
            return processes;
        }

        /// <summary>Gets a new <see cref="System.Diagnostics.Process" /> component and associates it with the currently active process.</summary>
        /// <returns>A new <see cref="System.Diagnostics.Process" /> component associated with the process resource that is running the calling application.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use this method to create a new <xref:System.Diagnostics.Process> instance and associate it with the process resource on the local computer.
        /// Like the similar <xref:System.Diagnostics.Process.GetProcessById%2A>, <xref:System.Diagnostics.Process.GetProcessesByName%2A>, and <xref:System.Diagnostics.Process.GetProcesses%2A> methods, <xref:System.Diagnostics.Process.GetCurrentProcess%2A> associates an existing resource with a new <xref:System.Diagnostics.Process> component.
        /// ## Examples
        /// The following example retrieves information of the current process, processes running on the local computer, all instances of Notepad running on the local computer, and a specific process on the local computer. It then retrieves information for the same processes on a remote computer.
        /// [!code-cpp[Process.GetProcesses_noexception#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.GetProcesses_noexception/CPP/processstaticget.cpp#1)]
        /// [!code-csharp[Process.GetProcesses_noexception#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.GetProcesses_noexception/CS/processstaticget.cs#1)]
        /// [!code-vb[Process.GetProcesses_noexception#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.GetProcesses_noexception/VB/processstaticget.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.GetProcessById(int,string)"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessesByName(string)"/>
        /// <altmember cref="O:System.Diagnostics.Process.GetProcesses"/>
        public static Process GetCurrentProcess()
        {
            return new Process(".", false, Environment.ProcessId, null);
        }

        /// <summary>Raises the <see cref="System.Diagnostics.Process.Exited" /> event.</summary>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// <xref:System.Diagnostics.Process.OnExited%2A> is the API method that raises the <xref:System.Diagnostics.Process.Exited> event. Calling <xref:System.Diagnostics.Process.OnExited%2A> causes the <xref:System.Diagnostics.Process.Exited> event to occur and is the only way to raise the event using the <xref:System.Diagnostics.Process> component. <xref:System.Diagnostics.Process.OnExited%2A> is primarily used when deriving classes from the component.
        /// As an alternative to <xref:System.Diagnostics.Process.OnExited%2A>, you can write your own event handler. You create your own event handler delegate and your own event-handling method.
        /// > [!NOTE]
        /// >  If you are using the Visual Studio environment, an event handler delegate (AddOnExited) and an event-handling method (Process1_Exited) are created for you when you drag a <xref:System.Diagnostics.Process> component onto a form and double-click the icon. The code you create to run when the <xref:System.Diagnostics.Process.Exited> event occurs is entered into the Process1_Exited procedure. You do not need to create the <xref:System.Diagnostics.Process.OnExited%2A> member, because it is implemented for you.
        /// Raising an event invokes the event handler through a delegate. For an overview, see [Handling and Raising Events](/dotnet/standard/events/).
        /// ## Examples
        /// The following example shows how to use the <xref:System.Diagnostics.Process.OnExited%2A> method in a derived class.
        /// [!code-csharp[OnExitSample#1](~/samples/snippets/csharp/VS_Snippets_CLR/onexitsample/cs/program.cs#1)]
        /// [!code-vb[OnExitSample#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/onexitsample/vb/program.vb#1)]
        /// ]]></format></remarks>
        /// <altmember cref="System.Diagnostics.Process.Exited"/>
        protected void OnExited()
        {
            EventHandler? exited = _onExited;
            if (exited != null)
            {
                if (SynchronizingObject is ISynchronizeInvoke syncObj && syncObj.InvokeRequired)
                {
                    syncObj.BeginInvoke(exited, new object[] { this, EventArgs.Empty });
                }
                else
                {
                    exited(this, EventArgs.Empty);
                }
            }
        }

        /// <devdoc>
        ///     Raise the Exited event, but make sure we don't do it more than once.
        /// </devdoc>
        /// <internalonly/>
        private void RaiseOnExited()
        {
            if (!_raisedOnExited)
            {
                lock (this)
                {
                    if (!_raisedOnExited)
                    {
                        _raisedOnExited = true;
                        OnExited();
                    }
                }
            }
        }

        /// <summary>Discards any information about the associated process that has been cached inside the process component.</summary>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// After <xref:System.Diagnostics.Process.Refresh%2A> is called, the first request for information about each property causes the process component to obtain a new value from the associated process.
        /// When a <xref:System.Diagnostics.Process> component is associated with a process resource, the property values of the <xref:System.Diagnostics.Process> are immediately populated according to the status of the associated process. If the information about the associated process subsequently changes, those changes are not reflected in the <xref:System.Diagnostics.Process> component's cached values. The <xref:System.Diagnostics.Process> component is a snapshot of the process resource at the time they are associated. To view the current values for the associated process, call the <xref:System.Diagnostics.Process.Refresh%2A> method.
        /// ## Examples
        /// The following example starts an instance of Notepad. It then retrieves the physical memory usage of the associated process at 2 second intervals for a maximum of 10 seconds. The example detects whether the process exits before 10 seconds have elapsed. The example closes the process if it is still running after 10 seconds.
        /// [!code-cpp[process_refresh#1](~/samples/snippets/cpp/VS_Snippets_CLR/process_refresh/CPP/process_refresh.cpp#1)]
        /// [!code-csharp[process_refresh#1](~/samples/snippets/csharp/VS_Snippets_CLR/process_refresh/CS/process_refresh.cs#1)]
        /// [!code-vb[process_refresh#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_refresh/VB/process_refresh.vb#1)]
        /// ]]></format></remarks>
        public void Refresh()
        {
            _processInfo = null;
            _threads?.Dispose();
            _threads = null;
            _modules?.Dispose();
            _modules = null;
            _exited = false;
            _haveWorkingSetLimits = false;
            _haveProcessorAffinity = false;
            _havePriorityClass = false;
            _haveExitTime = false;
            _havePriorityBoostEnabled = false;
            RefreshCore();
        }

        /// <summary>
        /// Opens a long-term handle to the process, with all access.  If a handle exists,
        /// then it is reused.  If the process has exited, it throws an exception.
        /// </summary>
        private SafeProcessHandle GetOrOpenProcessHandle()
        {
            if (!_haveProcessHandle)
            {
                //Cannot open a new process handle if the object has been disposed, since finalization has been suppressed.
                CheckDisposed();

                SetProcessHandle(GetProcessHandle());
            }
            return _processHandle!;
        }

        /// <devdoc>
        ///     Helper to associate a process handle with this component.
        /// </devdoc>
        /// <internalonly/>
        private void SetProcessHandle(SafeProcessHandle processHandle)
        {
            _processHandle = processHandle;
            _haveProcessHandle = true;
            if (_watchForExit)
            {
                EnsureWatchingForExit();
            }
        }

        /// <devdoc>
        ///     Helper to associate a process id with this component.
        /// </devdoc>
        /// <internalonly/>
        private void SetProcessId(int processId)
        {
            _processId = processId;
            _haveProcessId = true;
            ConfigureAfterProcessIdSet();
        }

        /// <summary>Additional optional configuration hook after a process ID is set.</summary>
        partial void ConfigureAfterProcessIdSet();

        /// <summary>Starts (or reuses) the process resource that is specified by the <see cref="System.Diagnostics.Process.StartInfo" /> property of this <see cref="System.Diagnostics.Process" /> component and associates it with the component.</summary>
        /// <returns><see langword="true" /> if a process resource is started; <see langword="false" /> if no new process resource is started (for example, if an existing process is reused).</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use this overload to start a process resource and associate it with the current <xref:System.Diagnostics.Process> component. The return value `true` indicates that a new process resource was started. If the process resource specified by the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> member of the <xref:System.Diagnostics.Process.StartInfo%2A> property is already running on the computer, no additional process resource is started. Instead, the running process resource is reused and `false` is returned.
        /// You can start a ClickOnce application by specifying the location (for example, a Web address) from which you originally installed the application. Do not start a ClickOnce application by specifying its installed location on your hard drive.
        /// > [!NOTE]
        /// >  If you are using Visual Studio, this overload of the <xref:System.Diagnostics.Process.Start%2A> method is the one that you insert into your code after you drag a <xref:System.Diagnostics.Process> component onto the designer. Use the `Properties` window to expand the `StartInfo` category and write the appropriate value into the `FileName` property. Your changes will appear in the form's `InitializeComponent` procedure.
        /// This overload of <xref:System.Diagnostics.Process.Start%2A> is not a `static` method. You must call it from an instance of the <xref:System.Diagnostics.Process> class. Before calling <xref:System.Diagnostics.Process.Start%2A>, you must first specify <xref:System.Diagnostics.Process.StartInfo%2A> property information for this <xref:System.Diagnostics.Process> instance, because that information is used to determine the process resource to start.
        /// The other overloads of the <xref:System.Diagnostics.Process.Start%2A> method are `static` members. You do not need to create an instance of the <xref:System.Diagnostics.Process> component before you call those overloads of the method. Instead, you can call <xref:System.Diagnostics.Process.Start%2A> for the <xref:System.Diagnostics.Process> class itself, and a new <xref:System.Diagnostics.Process> component is created if the process was started. Or, `null` is returned if a process was reused. The process resource is automatically associated with the new <xref:System.Diagnostics.Process> component that is returned by the <xref:System.Diagnostics.Process.Start%2A> method.
        /// The <xref:System.Diagnostics.Process.StartInfo%2A> members can be used to duplicate the functionality of the `Run` dialog box of the Windows `Start` menu. Anything that can be typed into a command line can be started by setting the appropriate values in the <xref:System.Diagnostics.Process.StartInfo%2A> property. The only <xref:System.Diagnostics.Process.StartInfo%2A> property that must be set is the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property. The <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property does not have to be an executable file. It can be of any file type for which the extension has been associated with an application that is installed on the system. For example, the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property can have a .txt extension if you have associated text files with an editor, such as Notepad, or it can have a .doc extension if you have associated .doc files with a word processing tool, such as Microsoft Word.
        /// In the command line, you can specify actions to take for certain types of files. For example, you can print documents or edit text files. Specify these actions using the <xref:System.Diagnostics.ProcessStartInfo.Verb%2A> member of the <xref:System.Diagnostics.Process.StartInfo%2A> property. For other types of files, you can specify command-line arguments when you start the file from the `Run` dialog box. For example, you can pass a URL as an argument if you specify your browser as the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A>. These arguments can be specified in the <xref:System.Diagnostics.Process.StartInfo%2A> property's <xref:System.Diagnostics.ProcessStartInfo.Arguments%2A> member.
        /// If you have a path variable declared in your system using quotes, you must fully qualify that path when starting any process found in that location. Otherwise, the system will not find the path. For example, if `c:\mypath` is not in your path, and you add it using quotation marks: `path = %path%;"c:\mypath"`, you must fully qualify any process in `c:\mypath` when starting it.
        /// > [!NOTE]
        /// >  ASP.NET Web page and server control code executes in the context of the ASP.NET worker process on the Web server.  If you use the <xref:System.Diagnostics.Process.Start%2A> method in an ASP.NET Web page or server control, the new process executes on the Web server with restricted permissions. The process does not start in the same context as the client browser, and does not have access to the user desktop.
        /// Whenever you use <xref:System.Diagnostics.Process.Start%2A> to start a process, you might need to close it or you risk losing system resources. Close processes using <xref:System.Diagnostics.Process.CloseMainWindow%2A> or <xref:System.Diagnostics.Process.Kill%2A>. You can check whether a process has already been closed by using its <xref:System.Diagnostics.Process.HasExited%2A> property.
        /// A note about apartment states in managed threads is necessary here. When <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> is `true` on the process component's <xref:System.Diagnostics.Process.StartInfo%2A> property, make sure you have set a threading model on your application by setting the attribute `[STAThread]` on the `main()` method. Otherwise, a managed thread can be in an `unknown` state or put in the `MTA` state, the latter of which conflicts with <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> being `true`. Some methods require that the apartment state not be `unknown`. If the state is not explicitly set, when the application encounters such a method, it defaults to `MTA`, and once set, the apartment state cannot be changed. However, `MTA` causes an exception to be thrown when the operating system shell is managing the thread.
        /// ## Examples
        /// The following example uses an instance of the <xref:System.Diagnostics.Process> class to start a process.
        /// [!code-cpp[Process.Start_instance#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_instance/CPP/processstart.cpp#1)]
        /// [!code-csharp[Process.Start_instance#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_instance/CS/processstart.cs#1)]
        /// [!code-vb[Process.Start_instance#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_instance/VB/processstart.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">No file name was specified in the <see cref="System.Diagnostics.Process" /> component's <see cref="System.Diagnostics.Process.StartInfo" />.
        /// -or-
        /// The <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> member of the <see cref="System.Diagnostics.Process.StartInfo" /> property is <see langword="true" /> while <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardInput" />, <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput" />, or <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError" /> is <see langword="true" />.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">There was an error in opening the associated file.</exception>
        /// <exception cref="System.ObjectDisposedException">The process object has already been disposed.</exception>
        /// <exception cref="System.PlatformNotSupportedException">Method not supported on operating systems without shell support such as Nano Server (.NET Core only).</exception>
        /// <altmember cref="System.Diagnostics.Process.StartInfo"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.FileName"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
        public bool Start()
        {
            Close();

            ProcessStartInfo startInfo = StartInfo;
            if (startInfo.FileName.Length == 0)
            {
                throw new InvalidOperationException(SR.FileNameMissing);
            }
            if (startInfo.StandardInputEncoding != null && !startInfo.RedirectStandardInput)
            {
                throw new InvalidOperationException(SR.StandardInputEncodingNotAllowed);
            }
            if (startInfo.StandardOutputEncoding != null && !startInfo.RedirectStandardOutput)
            {
                throw new InvalidOperationException(SR.StandardOutputEncodingNotAllowed);
            }
            if (startInfo.StandardErrorEncoding != null && !startInfo.RedirectStandardError)
            {
                throw new InvalidOperationException(SR.StandardErrorEncodingNotAllowed);
            }
            if (!string.IsNullOrEmpty(startInfo.Arguments) && startInfo.HasArgumentList)
            {
                throw new InvalidOperationException(SR.ArgumentAndArgumentListInitialized);
            }

            //Cannot start a new process and store its handle if the object has been disposed, since finalization has been suppressed.
            CheckDisposed();

            SerializationGuard.ThrowIfDeserializationInProgress("AllowProcessCreation", ref s_cachedSerializationSwitch);

            return StartCore(startInfo);
        }

        /// <summary>Starts a process resource by specifying the name of a document or application file and associates the resource with a new <see cref="System.Diagnostics.Process" /> component.</summary>
        /// <param name="fileName">The name of a document or application file to run in the process.</param>
        /// <returns>A new <see cref="System.Diagnostics.Process" /> that is associated with the process resource, or <see langword="null" /> if no process resource is started. Note that a new process that's started alongside already running instances of the same process will be independent from the others. In addition, Start may return a non-null Process with its <see cref="System.Diagnostics.Process.HasExited" /> property already set to <see langword="true" />. In this case, the started process may have activated an existing instance of itself and then exited.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use this overload to start a process resource by specifying its file name. The overload associates the resource with a new <xref:System.Diagnostics.Process> object.
        /// > [!NOTE]
        /// >  If the address of the executable file to start is a URL, the process is not started and `null` is returned.
        /// This overload lets you start a process without first creating a new <xref:System.Diagnostics.Process> instance. The overload is an alternative to the explicit steps of creating a new <xref:System.Diagnostics.Process> instance, setting the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> member of the <xref:System.Diagnostics.Process.StartInfo%2A> property, and calling <xref:System.Diagnostics.Process.Start%2A> for the <xref:System.Diagnostics.Process> instance.
        /// You can start a ClickOnce application by setting the `fileName` parameter to the location (for example, a Web address) from which you originally installed the application. Do not start a ClickOnce application by specifying its installed location on your hard drive.
        /// Starting a process by specifying its file name is similar to typing the information in the `Run` dialog box of the Windows `Start` menu. Therefore, the file name does not need to represent an executable file. It can be of any file type for which the extension has been associated with an application installed on the system. For example the file name can have a .txt extension if you have associated text files with an editor, such as Notepad, or it can have a .doc if you have associated .doc files with a word processing tool, such as Microsoft Word. Similarly, in the same way that the `Run` dialog box can accept an executable file name with or without the .exe extension, the .exe extension is optional in the `fileName` parameter. For example, you can set the `fileName` parameter to either "Notepad.exe" or "Notepad".
        /// This overload does not allow command-line arguments for the process. If you need to specify one or more command-line arguments for the process, use the <xref:System.Diagnostics.Process.Start%28System.Diagnostics.ProcessStartInfo%29?displayProperty=nameWithType> or <xref:System.Diagnostics.Process.Start%28string%2Cstring%29?displayProperty=nameWithType> overloads.
        /// Unlike the other overloads, the overload of <xref:System.Diagnostics.Process.Start%2A> that has no parameters is not a `static` member. Use that overload when you have already created a <xref:System.Diagnostics.Process> instance and specified start information (including the file name), and you want to start a process resource and associate it with the existing <xref:System.Diagnostics.Process> instance. Use one of the `static` overloads when you want to create a new <xref:System.Diagnostics.Process> component rather than start a process for an existing component. Both this overload and the overload that has no parameters allow you to specify the file name of the process resource to start.
        /// If you have a path variable declared in your system using quotes, you must fully qualify that path when starting any process found in that location. Otherwise, the system will not find the path. For example, if `c:\mypath` is not in your path, and you add it using quotation marks: `path = %path%;"c:\mypath"`, you must fully qualify any process in `c:\mypath` when starting it.
        /// > [!NOTE]
        /// >  ASP.NET Web page and server control code executes in the context of the ASP.NET worker process on the Web server.  If you use the <xref:System.Diagnostics.Process.Start%2A> method in an ASP.NET Web page or server control, the new process executes on the Web server with restricted permissions. The process does not start in the same context as the client browser, and does not have access to the user desktop.
        /// Whenever you use <xref:System.Diagnostics.Process.Start%2A> to start a process, you might need to close it or you risk losing system resources. Close processes using <xref:System.Diagnostics.Process.CloseMainWindow%2A> or <xref:System.Diagnostics.Process.Kill%2A>. You can check whether a process has already been closed by using its <xref:System.Diagnostics.Process.HasExited%2A> property.
        /// A note about apartment states in managed threads is necessary here. When <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> is `true` on the process component's <xref:System.Diagnostics.Process.StartInfo%2A> property, make sure you have set a threading model on your application by setting the attribute `[STAThread]` on the `main()` method. Otherwise, a managed thread can be in an `unknown` state or put in the `MTA` state, the latter of which conflicts with <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> being `true`. Some methods require that the apartment state not be `unknown`. If the state is not explicitly set, when the application encounters such a method, it defaults to `MTA`, and once set, the apartment state cannot be changed. However, `MTA` causes an exception to be thrown when the operating system shell is managing the thread.
        /// ## Examples
        /// The following example first spawns an instance of Internet Explorer and displays the contents of the Favorites folder in the browser. It then starts some other instances of Internet Explorer and displays some specific pages or sites. Finally it starts Internet Explorer with the window being minimized while navigating to a specific site.
        /// [!code-cpp[Process.Start_static#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_static/CPP/processstartstatic.cpp)]
        /// [!code-csharp[Process.Start_static#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_static/CS/processstartstatic.cs)]
        /// [!code-vb[Process.Start_static#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_static/VB/processstartstatic.vb)]
        /// ]]></format></remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">An error occurred when opening the associated file.
        /// -or-
        /// The file specified in the <paramref name="fileName" /> could not be found.</exception>
        /// <exception cref="System.ObjectDisposedException">The process object has already been disposed.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The PATH environment variable has a string containing quotes.</exception>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.FileName"/>
        /// <altmember cref="System.Diagnostics.Process.StartInfo"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
        public static Process Start(string fileName)
        {
            // the underlying Start method can only return null on Windows platforms,
            // when the ProcessStartInfo.UseShellExecute property is set to true.
            // We can thus safely assert non-nullability for tihs overload.
            return Start(new ProcessStartInfo(fileName))!;
        }

        /// <summary>Starts a process resource by specifying the name of an application and a set of command-line arguments, and associates the resource with a new <see cref="System.Diagnostics.Process" /> component.</summary>
        /// <param name="fileName">The name of an application file to run in the process.</param>
        /// <param name="arguments">Command-line arguments to pass when starting the process.</param>
        /// <returns>A new <see cref="System.Diagnostics.Process" /> that is associated with the process resource, or <see langword="null" /> if no process resource is started. Note that a new process that's started alongside already running instances of the same process will be independent from the others. In addition, Start may return a non-null Process with its <see cref="System.Diagnostics.Process.HasExited" /> property already set to <see langword="true" />. In this case, the started process may have activated an existing instance of itself and then exited.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use this overload to start a process resource by specifying its file name and command-line arguments. The overload associates the resource with a new <xref:System.Diagnostics.Process> object.
        /// > [!NOTE]
        /// >  If the address of the executable file to start is a URL, the process is not started and `null` is returned.
        /// This overload lets you start a process without first creating a new <xref:System.Diagnostics.Process> instance. The overload is an alternative to the explicit steps of creating a new <xref:System.Diagnostics.Process> instance, setting the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> and <xref:System.Diagnostics.ProcessStartInfo.Arguments%2A> members of the <xref:System.Diagnostics.Process.StartInfo%2A> property, and calling <xref:System.Diagnostics.Process.Start%2A> for the <xref:System.Diagnostics.Process> instance.
        /// Starting a process by specifying its file name and arguments is similar to typing the file name and command-line arguments in the `Run` dialog box of the Windows `Start` menu. Therefore, the file name does not need to represent an executable file. It can be of any file type for which the extension has been associated with an application installed on the system. For example the file name can have a .txt extension if you have associated text files with an editor, such as Notepad, or it can have a .doc if you have associated .doc files with a word processing tool, such as Microsoft Word. Similarly, in the same way that the `Run` dialog box can accept an executable file name with or without the .exe extension, the .exe extension is optional in the `fileName` parameter. For example, you can set the `fileName` parameter to either "Notepad.exe" or "Notepad". If the `fileName` parameter represents an executable file, the `arguments` parameter might represent a file to act upon, such as the text file in `Notepad.exe myfile.txt`. If the `fileName` parameter represents a command (.cmd) file, the `arguments` parameter must include either a "`/c`" or "`/k`" argument to specify whether the command window exits or remains after completion.
        /// Unlike the other overloads, the overload of <xref:System.Diagnostics.Process.Start%2A> that has no parameters is not a `static` member. Use that overload when you have already created a <xref:System.Diagnostics.Process> instance and specified start information (including the file name), and you want to start a process resource and associate it with the existing <xref:System.Diagnostics.Process> instance. Use one of the `static` overloads when you want to create a new <xref:System.Diagnostics.Process> component rather than start a process for an existing component. Both this overload and the overload that has no parameters allow you to specify the file name of the process resource to start and command-line arguments to pass.
        /// If you have a path variable declared in your system using quotes, you must fully qualify that path when starting any process found in that location. Otherwise, the system will not find the path. For example, if `c:\mypath` is not in your path, and you add it using quotation marks: `path = %path%;"c:\mypath"`, you must fully qualify any process in `c:\mypath` when starting it.
        /// > [!NOTE]
        /// >  ASP.NET Web page and server control code executes in the context of the ASP.NET worker process on the Web server.  If you use the <xref:System.Diagnostics.Process.Start%2A> method in an ASP.NET Web page or server control, the new process executes on the Web server with restricted permissions. The process does not start in the same context as the client browser, and does not have access to the user desktop.
        /// Whenever you use <xref:System.Diagnostics.Process.Start%2A> to start a process, you might need to close it or you risk losing system resources. Close processes using <xref:System.Diagnostics.Process.CloseMainWindow%2A> or <xref:System.Diagnostics.Process.Kill%2A>. You can check whether a process has already been closed by using its <xref:System.Diagnostics.Process.HasExited%2A> property.
        /// A note about apartment states in managed threads is necessary here. When <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> is `true` on the process component's <xref:System.Diagnostics.Process.StartInfo%2A> property, make sure you have set a threading model on your application by setting the attribute `[STAThread]` on the `main()` method. Otherwise, a managed thread can be in an `unknown` state or put in the `MTA` state, the latter of which conflicts with <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> being `true`. Some methods require that the apartment state not be `unknown`. If the state is not explicitly set, when the application encounters such a method, it defaults to `MTA`, and once set, the apartment state cannot be changed. However, `MTA` causes an exception to be thrown when the operating system shell is managing the thread.
        /// ## Examples
        /// The following example first spawns an instance of Internet Explorer and displays the contents of the Favorites folder in the browser. It then starts some other instances of Internet Explorer and displays some specific pages or sites. Finally it starts Internet Explorer with the window being minimized while navigating to a specific site.
        /// [!code-cpp[Process.Start_static#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_static/CPP/processstartstatic.cpp)]
        /// [!code-csharp[Process.Start_static#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_static/CS/processstartstatic.cs)]
        /// [!code-vb[Process.Start_static#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_static/VB/processstartstatic.vb)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The <paramref name="fileName" /> or <paramref name="arguments" /> parameter is <see langword="null" />.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">An error occurred when opening the associated file.
        /// -or-
        /// The file specified in the <paramref name="fileName" /> could not be found.
        /// -or-
        /// The sum of the length of the arguments and the length of the full path to the process exceeds 2080. The error message associated with this exception can be one of the following: "The data area passed to a system call is too small." or "Access is denied."</exception>
        /// <exception cref="System.ObjectDisposedException">The process object has already been disposed.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The PATH environment variable has a string containing quotes.</exception>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.FileName"/>
        /// <altmember cref="System.Diagnostics.Process.StartInfo"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
        public static Process Start(string fileName, string arguments)
        {
            // the underlying Start method can only return null on Windows platforms,
            // when the ProcessStartInfo.UseShellExecute property is set to true.
            // We can thus safely assert non-nullability for tihs overload.
            return Start(new ProcessStartInfo(fileName, arguments))!;
        }

        /// <summary>Starts a process resource by specifying the name of an application and a set of command line arguments.</summary>
        /// <param name="fileName">The name of a document or application file to run in the process.</param>
        /// <param name="arguments">The command-line arguments to pass when starting the process.</param>
        /// <returns>A new <see cref="System.Diagnostics.Process" /> that is associated with the process resource, or <see langword="null" /> if no process resource is started.</returns>
        /// <remarks>Each argument will be escaped automatically if required.</remarks>
        public static Process Start(string fileName, IEnumerable<string> arguments)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            var startInfo = new ProcessStartInfo(fileName);
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            return Start(startInfo)!;
        }

        /// <summary>Starts the process resource that is specified by the parameter containing process start information (for example, the file name of the process to start) and associates the resource with a new <see cref="System.Diagnostics.Process" /> component.</summary>
        /// <param name="startInfo">The <see cref="System.Diagnostics.ProcessStartInfo" /> that contains the information that is used to start the process, including the file name and any command-line arguments.</param>
        /// <returns>A new <see cref="System.Diagnostics.Process" /> that is associated with the process resource, or <see langword="null" /> if no process resource is started. Note that a new process that's started alongside already running instances of the same process will be independent from the others. In addition, Start may return a non-null Process with its <see cref="System.Diagnostics.Process.HasExited" /> property already set to <see langword="true" />. In this case, the started process may have activated an existing instance of itself and then exited.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// Use this overload to start a process resource by specifying a <xref:System.Diagnostics.ProcessStartInfo> instance. The overload associates the resource with a new <xref:System.Diagnostics.Process> object.
        /// > [!NOTE]
        /// >  If the address of the executable file to start is a URL, the process is not started and `null` is returned.
        /// This overload lets you start a process without first creating a new <xref:System.Diagnostics.Process> instance. Using this overload with a <xref:System.Diagnostics.ProcessStartInfo> parameter is an alternative to the explicit steps of creating a new <xref:System.Diagnostics.Process> instance, setting its <xref:System.Diagnostics.Process.StartInfo%2A> properties, and calling <xref:System.Diagnostics.Process.Start%2A> for the <xref:System.Diagnostics.Process> instance.
        /// Using a <xref:System.Diagnostics.ProcessStartInfo> instance as the parameter lets you call <xref:System.Diagnostics.Process.Start%2A> with the most control over what is passed into the call to start the process. If you need to pass only a file name or a file name and arguments, it is not necessary to create a new <xref:System.Diagnostics.ProcessStartInfo> instance, although that is an option. The only <xref:System.Diagnostics.Process.StartInfo%2A?displayProperty=nameWithType> property that must be set is the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property. The <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property does not need to represent an executable file. It can be of any file type for which the extension has been associated with an application that is installed on the system. For example, the <xref:System.Diagnostics.ProcessStartInfo.FileName%2A> property can have a .txt extension if you have associated text files with an editor, such as Notepad, or it can have a .doc extension if you have associated .doc files with a word processing tool, such as Microsoft Word.
        /// You can start a ClickOnce application by specifying the location (for example, a Web address) from which you originally installed the application. Do not start a ClickOnce application by specifying its installed location on your hard drive.
        /// If the <xref:System.Diagnostics.ProcessStartInfo.UserName%2A?displayProperty=nameWithType> and <xref:System.Diagnostics.ProcessStartInfo.Password%2A?displayProperty=nameWithType> properties of the <xref:System.Diagnostics.Process.StartInfo%2A> instance are set, the unmanaged `CreateProcessWithLogonW` function is called, which starts the process in a new window even if the <xref:System.Diagnostics.ProcessStartInfo.CreateNoWindow%2A?displayProperty=nameWithType> property value is `true` or the <xref:System.Diagnostics.ProcessStartInfo.WindowStyle%2A?displayProperty=nameWithType> property value is <xref:System.Diagnostics.ProcessWindowStyle.Hidden?displayProperty=nameWithType>. If the <xref:System.Diagnostics.ProcessStartInfo.Domain%2A?displayProperty=nameWithType> property is `null`, the <xref:System.Diagnostics.ProcessStartInfo.UserName%2A?displayProperty=nameWithType> property must be in UPN format, *user*@*DNS_domain_name*.
        /// Unlike the other overloads, the overload of <xref:System.Diagnostics.Process.Start%2A> that has no parameters is not a `static` member. Use that overload when you have already created a <xref:System.Diagnostics.Process> instance and specified start information (including the file name), and you want to start a process resource and associate it with the existing <xref:System.Diagnostics.Process> instance. Use one of the `static` overloads when you want to create a new <xref:System.Diagnostics.Process> component rather than start a process for an existing component. Both this overload and the overload that has no parameters allow you to specify the start information for the process resource by using a <xref:System.Diagnostics.ProcessStartInfo> instance.
        /// If you have a path variable declared in your system using quotes, you must fully qualify that path when starting any process found in that location. Otherwise, the system will not find the path. For example, if `c:\mypath` is not in your path, and you add it using quotation marks: `path = %path%;"c:\mypath"`, you must fully qualify any process in `c:\mypath` when starting it.
        /// > [!NOTE]
        /// >  ASP.NET Web page and server control code executes in the context of the ASP.NET worker process on the Web server.  If you use the <xref:System.Diagnostics.Process.Start%2A> method in an ASP.NET Web page or server control, the new process executes on the Web server with restricted permissions. The process does not start in the same context as the client browser, and does not have access to the user desktop.
        /// Whenever you use <xref:System.Diagnostics.Process.Start%2A> to start a process, you might need to close it or you risk losing system resources. Close processes using <xref:System.Diagnostics.Process.CloseMainWindow%2A> or <xref:System.Diagnostics.Process.Kill%2A>. You can check whether a process has already been closed by using its <xref:System.Diagnostics.Process.HasExited%2A> property.
        /// A note about apartment states in managed threads is necessary here. When <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> is `true` on the `startInfo` parameter, make sure you have set a threading model on your application by setting the attribute `[STAThread]` on the `main()` method. Otherwise, a managed thread can be in an `unknown` state or put in the `MTA` state, the latter of which conflicts with <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> being `true`. Some methods require that the apartment state not be `unknown`. If the state is not explicitly set, when the application encounters such a method, it defaults to `MTA`, and once set, the apartment state cannot be changed. However, `MTA` causes an exception to be thrown when the operating system shell is managing the thread.
        /// ## Examples
        /// The following example first spawns an instance of Internet Explorer and displays the contents of the Favorites folder in the browser. It then starts some other instances of Internet Explorer and displays some specific pages or sites. Finally it starts Internet Explorer with the window being minimized while navigating to a specific site.
        /// For additional examples of other uses of this method, refer to the individual properties of the <xref:System.Diagnostics.ProcessStartInfo> class.
        /// [!code-cpp[Process.Start_static#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_static/CPP/processstartstatic.cpp)]
        /// [!code-csharp[Process.Start_static#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_static/CS/processstartstatic.cs)]
        /// [!code-vb[Process.Start_static#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_static/VB/processstartstatic.vb)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">No file name was specified in the <paramref name="startInfo" /> parameter's <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property.
        /// -or-
        /// The <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property of the <paramref name="startInfo" /> parameter is <see langword="true" /> and the <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardInput" />, <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput" />, or <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError" /> property is also <see langword="true" />.
        /// -or-
        /// The <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property of the <paramref name="startInfo" /> parameter is <see langword="true" /> and the <see cref="System.Diagnostics.ProcessStartInfo.UserName" /> property is not <see langword="null" /> or empty or the <see cref="System.Diagnostics.ProcessStartInfo.Password" /> property is not <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="startInfo" /> parameter is <see langword="null" />.</exception>
        /// <exception cref="System.ObjectDisposedException">The process object has already been disposed.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">An error occurred when opening the associated file.
        /// -or-
        /// The file specified in the <paramref name="startInfo" /> parameter's <see cref="System.Diagnostics.ProcessStartInfo.FileName" /> property could not be found.
        /// -or-
        /// The sum of the length of the arguments and the length of the full path to the process exceeds 2080. The error message associated with this exception can be one of the following: "The data area passed to a system call is too small." or "Access is denied."</exception>
        /// <exception cref="System.PlatformNotSupportedException">Method not supported on operating systems without shell support such as Nano Server (.NET Core only).</exception>
        /// <altmember cref="System.Diagnostics.Process.StartInfo"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.FileName"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
        public static Process? Start(ProcessStartInfo startInfo)
        {
            Process process = new Process();
            if (startInfo == null)
                throw new ArgumentNullException(nameof(startInfo));

            process.StartInfo = startInfo;
            return process.Start() ?
                process :
                null;
        }

        /// <devdoc>
        ///     Make sure we are not watching for process exit.
        /// </devdoc>
        /// <internalonly/>
        private void StopWatchingForExit()
        {
            if (_watchingForExit)
            {
                RegisteredWaitHandle? rwh = null;
                WaitHandle? wh = null;

                lock (this)
                {
                    if (_watchingForExit)
                    {
                        _watchingForExit = false;

                        wh = _waitHandle;
                        _waitHandle = null;

                        rwh = _registeredWaitHandle;
                        _registeredWaitHandle = null;
                    }
                }

                if (rwh != null)
                {
                    rwh.Unregister(null);
                }

                if (wh != null)
                {
                    wh.Dispose();
                }
            }
        }

        /// <summary>Formats the process's name as a string, combined with the parent component type, if applicable.</summary>
        /// <returns>The <see cref="System.Diagnostics.Process.ProcessName" />, combined with the base component's <see cref="object.ToString" /> return value.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// ## Examples
        /// The following example starts an instance of Notepad. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays the process's exit code.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        public override string ToString()
        {
            if (Associated)
            {
                string processName = ProcessName;
                if (processName.Length != 0)
                {
                    return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", base.ToString(), processName);
                }
            }
            return base.ToString();
        }

        /// <summary>Instructs the <see cref="System.Diagnostics.Process" /> component to wait indefinitely for the associated process to exit.</summary>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// <xref:System.Diagnostics.Process.WaitForExit> makes the current thread wait until the associated process terminates.  It should be called after all other methods are called on the process. To avoid blocking the current thread, use the <xref:System.Diagnostics.Process.Exited> event.
        /// This method instructs the <xref:System.Diagnostics.Process> component to wait an infinite amount of time for the process and event handlers to exit. This can cause an application to stop responding. For example, if you call <xref:System.Diagnostics.Process.CloseMainWindow%2A> for a process that has a user interface, the request to the operating system to terminate the associated process might not be handled if the process is written to never enter its message loop.
        /// > [!NOTE]
        /// >  In the [!INCLUDE[net_v35_long](~/includes/net-v35-long-md.md)] and earlier versions, the <xref:System.Diagnostics.Process.WaitForExit> overload waited for <xref:int.MaxValue> milliseconds (approximately 24 days), not indefinitely. Also, previous versions did not wait for the event handlers to exit if the full <xref:int.MaxValue> time was reached.
        /// This overload ensures that all processing has been completed, including the handling of asynchronous events for redirected standard output. You should use this overload after a call to the <xref:System.Diagnostics.Process.WaitForExit%28int%29> overload when standard output has been redirected to asynchronous event handlers.
        /// When an associated process exits (that is, when it is shut down by the operation system through a normal or abnormal termination), the system stores administrative information about the process and returns to the component that had called <xref:System.Diagnostics.Process.WaitForExit>. The <xref:System.Diagnostics.Process> component can then access the information, which includes the <xref:System.Diagnostics.Process.ExitTime%2A>, by using the <xref:System.Diagnostics.Process.Handle%2A> to the exited process.
        /// Because the associated process has exited, the <xref:System.Diagnostics.Process.Handle%2A> property of the component no longer points to an existing process resource. Instead, the handle can be used only to access the operating system's information about the process resource. The system is aware of handles to exited processes that have not been released by <xref:System.Diagnostics.Process> components, so it keeps the <xref:System.Diagnostics.Process.ExitTime%2A> and <xref:System.Diagnostics.Process.Handle%2A> information in memory until the <xref:System.Diagnostics.Process> component specifically frees the resources. For this reason, any time you call <xref:System.Diagnostics.Process.Start%2A> for a <xref:System.Diagnostics.Process> instance, call <xref:System.Diagnostics.Process.Close%2A> when the associated process has terminated and you no longer need any administrative information about it. <xref:System.Diagnostics.Process.Close%2A> frees the memory allocated to the exited process.
        /// ## Examples
        /// See the Remarks section of the <xref:System.Diagnostics.Process.StandardError%2A> property reference page.
        /// ]]></format></remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">The wait setting could not be accessed.</exception>
        /// <exception cref="System.SystemException">No process <see cref="System.Diagnostics.Process.Id" /> has been set, and a <see cref="System.Diagnostics.Process.Handle" /> from which the <see cref="System.Diagnostics.Process.Id" /> property can be determined does not exist.
        /// -or-
        /// There is no process associated with this <see cref="System.Diagnostics.Process" /> object.
        /// -or-
        /// You are attempting to call <see cref="O:System.Diagnostics.Process.WaitForExit" /> for a process that is running on a remote computer. This method is available only for processes that are running on the local computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
        /// <altmember cref="System.Diagnostics.Process.Handle"/>
        /// <altmember cref="System.Diagnostics.Process.ExitTime"/>
        /// <altmember cref="System.Diagnostics.Process.EnableRaisingEvents"/>
        /// <altmember cref="System.Diagnostics.Process.HasExited"/>
        /// <altmember cref="System.Diagnostics.Process.Exited"/>
        public void WaitForExit()
        {
            WaitForExit(Timeout.Infinite);
        }

        /// <summary>Instructs the <see cref="System.Diagnostics.Process" /> component to wait the specified number of milliseconds for the associated process to exit.</summary>
        /// <param name="milliseconds">The amount of time, in milliseconds, to wait for the associated process to exit. A value of 0 specifies an immediate return, and a value of -1 specifies an infinite wait.</param>
        /// <returns><see langword="true" /> if the associated process has exited; otherwise, <see langword="false" />.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// <xref:System.Diagnostics.Process.WaitForExit%28int%29> makes the current thread wait until the associated process terminates. It should be called after all other methods are called on the process. To avoid blocking the current thread, use the <xref:System.Diagnostics.Process.Exited> event.
        /// This method instructs the <xref:System.Diagnostics.Process> component to wait a finite amount of time for the process to exit. If the associated process does not exit by the end of the interval because the request to terminate is denied, `false` is returned to the calling procedure. You can specify <xref:System.Threading.Timeout.Infinite?displayProperty=nameWithType> for `milliseconds`, and <xref:System.Diagnostics.Process.WaitForExit%28int%29?displayProperty=nameWithType> will behave the same as the <xref:System.Diagnostics.Process.WaitForExit> overload. If you pass 0 (zero) to the method, it returns `true` only if the process has already exited; otherwise, it immediately returns `false`.
        /// > [!NOTE]
        /// >  In the [!INCLUDE[net_v35_long](~/includes/net-v35-long-md.md)] and earlier versions, if `milliseconds` was -1, the <xref:System.Diagnostics.Process.WaitForExit%28int%29> overload waited for <xref:int.MaxValue> milliseconds (approximately 24 days), not indefinitely.
        /// When standard output has been redirected to asynchronous event handlers, it is possible that output processing will not have completed when this method returns. To ensure that asynchronous event handling has been completed, call the <xref:System.Diagnostics.Process.WaitForExit> overload that takes no parameter after receiving a `true` from this overload. To help ensure that the <xref:System.Diagnostics.Process.Exited> event is handled correctly in Windows Forms applications, set the <xref:System.Diagnostics.Process.SynchronizingObject%2A> property.
        /// When an associated process exits (is shut down by the operating system through a normal or abnormal termination), the system stores administrative information about the process and returns to the component that had called <xref:System.Diagnostics.Process.WaitForExit%28int%29>. The <xref:System.Diagnostics.Process> component can then access the information, which includes the <xref:System.Diagnostics.Process.ExitTime%2A>, by using the <xref:System.Diagnostics.Process.Handle%2A> to the exited process.
        /// Because the associated process has exited, the <xref:System.Diagnostics.Process.Handle%2A> property of the component no longer points to an existing process resource. Instead, the handle can be used only to access the operating system's information about the process resource. The system is aware of handles to exited processes that have not been released by <xref:System.Diagnostics.Process> components, so it keeps the <xref:System.Diagnostics.Process.ExitTime%2A> and <xref:System.Diagnostics.Process.Handle%2A> information in memory until the <xref:System.Diagnostics.Process> component specifically frees the resources. For this reason, any time you call <xref:System.Diagnostics.Process.Start%2A> for a <xref:System.Diagnostics.Process> instance, call <xref:System.Diagnostics.Process.Close%2A> when the associated process has terminated and you no longer need any administrative information about it. <xref:System.Diagnostics.Process.Close%2A> frees the memory allocated to the exited process.
        /// ## Examples
        /// See the code example for the <xref:System.Diagnostics.Process.ExitCode%2A> property.
        /// ]]></format></remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">The wait setting could not be accessed.</exception>
        /// <exception cref="System.SystemException">No process <see cref="System.Diagnostics.Process.Id" /> has been set, and a <see cref="System.Diagnostics.Process.Handle" /> from which the <see cref="System.Diagnostics.Process.Id" /> property can be determined does not exist.
        /// -or-
        /// There is no process associated with this <see cref="System.Diagnostics.Process" /> object.
        /// -or-
        /// You are attempting to call <see cref="System.Diagnostics.Process.WaitForExit(int)" /> for a process that is running on a remote computer. This method is available only for processes that are running on the local computer.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="milliseconds" /> is a negative number other than -1, which represents an infinite time-out.</exception>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
        /// <altmember cref="System.Diagnostics.Process.Handle"/>
        /// <altmember cref="System.Diagnostics.Process.ExitTime"/>
        /// <altmember cref="System.Diagnostics.Process.EnableRaisingEvents"/>
        /// <altmember cref="System.Diagnostics.Process.HasExited"/>
        /// <altmember cref="System.Diagnostics.Process.Exited"/>
        /// <altmember cref="System.Diagnostics.Process.SynchronizingObject"/>
        public bool WaitForExit(int milliseconds)
        {
            bool exited = WaitForExitCore(milliseconds);
            if (exited && _watchForExit)
            {
                RaiseOnExited();
            }
            return exited;
        }

        /// <summary>Instructs the process component to wait for the associated process to exit, or for the <paramref name="cancellationToken" /> to be cancelled.</summary>
        /// <param name="cancellationToken">An optional token to cancel the asynchronous operation.</param>
        /// <returns>A task that will complete when the process has exited, cancellation has been requested, or an error occurs.</returns>
        /// <remarks>Calling this method will set <see cref="System.Diagnostics.Process.EnableRaisingEvents" /> to <see langword="true" />.</remarks>
        public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            // Because the process has already started by the time this method is called,
            // we're in a race against the process to set up our exit handlers before the process
            // exits. As a result, there are several different flows that must be handled:
            //
            // CASE 1: WE ENABLE EVENTS
            // This is the "happy path". In this case we enable events.
            //
            // CASE 1.1: PROCESS EXITS OR IS CANCELED AFTER REGISTERING HANDLER
            // This case continues the "happy path". The process exits or waiting is canceled after
            // registering the handler and no special cases are needed.
            //
            // CASE 1.2: PROCESS EXITS BEFORE REGISTERING HANDLER
            // It's possible that the process can exit after we enable events but before we reigster
            // the handler. In that case we must check for exit after registering the handler.
            //
            //
            // CASE 2: PROCESS EXITS BEFORE ENABLING EVENTS
            // The process may exit before we attempt to enable events. In that case EnableRaisingEvents
            // will throw an exception like this:
            //     System.InvalidOperationException : Cannot process request because the process (42) has exited.
            // In this case we catch the InvalidOperationException. If the process has exited, our work
            // is done and we return. If for any reason (now or in the future) enabling events fails
            // and the process has not exited, bubble the exception up to the user.
            //
            //
            // CASE 3: USER ALREADY ENABLED EVENTS
            // In this case the user has already enabled raising events. Re-enabling events is a no-op
            // as the value hasn't changed. However, no-op also means that if the process has already
            // exited, EnableRaisingEvents won't throw an exception.
            //
            // CASE 3.1: PROCESS EXITS OR IS CANCELED AFTER REGISTERING HANDLER
            // (See CASE 1.1)
            //
            // CASE 3.2: PROCESS EXITS BEFORE REGISTERING HANDLER
            // (See CASE 1.2)

            if (!Associated)
            {
                throw new InvalidOperationException(SR.NoAssociatedProcess);
            }

            if (!HasExited)
            {
                // Early out for cancellation before doing more expensive work
                cancellationToken.ThrowIfCancellationRequested();
            }

            try
            {
                // CASE 1: We enable events
                // CASE 2: Process exits before enabling events (and throws an exception)
                // CASE 3: User already enabled events (no-op)
                EnableRaisingEvents = true;
            }
            catch (InvalidOperationException)
            {
                // CASE 2: If the process has exited, our work is done, otherwise bubble the
                // exception up to the user
                if (HasExited)
                {
                    await WaitUntilOutputEOF().ConfigureAwait(false);
                    return;
                }

                throw;
            }

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler handler = (_, _) => tcs.TrySetResult();
            Exited += handler;

            try
            {
                if (HasExited)
                {
                    // CASE 1.2 & CASE 3.2: Handle race where the process exits before registering the handler
                }
                else
                {
                    // CASE 1.1 & CASE 3.1: Process exits or is canceled here
                    using (cancellationToken.UnsafeRegister(static (s, cancellationToken) => ((TaskCompletionSource)s!).TrySetCanceled(cancellationToken), tcs))
                    {
                        await tcs.Task.ConfigureAwait(false);
                    }
                }

                // Wait until output streams have been drained
                await WaitUntilOutputEOF().ConfigureAwait(false);
            }
            finally
            {
                Exited -= handler;
            }

            async ValueTask WaitUntilOutputEOF()
            {
                if (_output != null)
                {
                    await _output.WaitUntilEOFAsync(cancellationToken).ConfigureAwait(false);
                }

                if (_error != null)
                {
                    await _error.WaitUntilEOFAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>Begins asynchronous read operations on the redirected <see cref="System.Diagnostics.Process.StandardOutput" /> stream of the application.</summary>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Diagnostics.Process.StandardOutput%2A> stream can be read synchronously or asynchronously. Methods such as <xref:System.IO.StreamReader.Read%2A>, <xref:System.IO.StreamReader.ReadLine%2A>, and <xref:System.IO.StreamReader.ReadToEnd%2A> perform synchronous read operations on the output stream of the process. These synchronous read operations do not complete until the associated <xref:System.Diagnostics.Process> writes to its <xref:System.Diagnostics.Process.StandardOutput%2A> stream, or closes the stream.
        /// In contrast, <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> starts asynchronous read operations on the <xref:System.Diagnostics.Process.StandardOutput%2A> stream. This method enables a designated event handler for the stream output and immediately returns to the caller, which can perform other work while the stream output is directed to the event handler.
        /// Follow these steps to perform asynchronous read operations on <xref:System.Diagnostics.Process.StandardOutput%2A> for a <xref:System.Diagnostics.Process> :
        /// 1.  Set <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> to `false`.
        /// 2.  Set <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardOutput%2A> to `true`.
        /// 3.  Add your event handler to the <xref:System.Diagnostics.Process.OutputDataReceived> event. The event handler must match the <xref:System.Diagnostics.DataReceivedEventHandler?displayProperty=nameWithType> delegate signature.
        /// 4.  Start the <xref:System.Diagnostics.Process>.
        /// 5.  Call <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> for the <xref:System.Diagnostics.Process>. This call starts asynchronous read operations on <xref:System.Diagnostics.Process.StandardOutput%2A>.
        /// When asynchronous read operations start, the event handler is called each time the associated <xref:System.Diagnostics.Process> writes a line of text to its <xref:System.Diagnostics.Process.StandardOutput%2A> stream.
        /// You can cancel an asynchronous read operation by calling <xref:System.Diagnostics.Process.CancelOutputRead%2A>. The read operation can be canceled by the caller or by the event handler. After canceling, you can call <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> again to resume asynchronous read operations.
        /// > [!NOTE]
        /// >  You cannot mix asynchronous and synchronous read operations on a redirected stream. Once the redirected stream of a <xref:System.Diagnostics.Process> is opened in either asynchronous or synchronous mode, all further read operations on that stream must be in the same mode. For example, do not follow <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> with a call to <xref:System.IO.StreamReader.ReadLine%2A> on the <xref:System.Diagnostics.Process.StandardOutput%2A> stream, or vice versa. However, you can read two different streams in different modes. For example, you can call <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> and then call <xref:System.IO.StreamReader.ReadLine%2A> for the <xref:System.Diagnostics.Process.StandardError%2A> stream.
        /// ## Examples
        /// The following example illustrates how to perform asynchronous read operations on the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream of the `sort` command. The `sort` command is a console application that reads and sorts text input.
        /// The example creates an event delegate for the `SortOutputHandler` event handler and associates it with the <xref:System.Diagnostics.Process.OutputDataReceived> event. The event handler receives text lines from the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream, formats the text, and writes the text to the screen.
        /// [!code-cpp[Process_AsyncStreams#1](~/samples/snippets/cpp/VS_Snippets_CLR/process_asyncstreams/CPP/sort_async.cpp#1)]
        /// [!code-csharp[Process_AsyncStreams#1](~/samples/snippets/csharp/VS_Snippets_CLR/process_asyncstreams/CS/sort_async.cs#1)]
        /// [!code-vb[Process_AsyncStreams#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_asyncstreams/VB/sort_async.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput" /> property is <see langword="false" />.
        /// -or-
        /// An asynchronous read operation is already in progress on the <see cref="System.Diagnostics.Process.StandardOutput" /> stream.
        /// -or-
        /// The <see cref="System.Diagnostics.Process.StandardOutput" /> stream has been used by a synchronous read operation.</exception>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput"/>
        /// <altmember cref="System.Diagnostics.Process.StandardOutput"/>
        /// <altmember cref="System.Diagnostics.Process.OutputDataReceived"/>
        /// <altmember cref="System.Diagnostics.DataReceivedEventHandler"/>
        /// <altmember cref="System.Diagnostics.Process.CancelOutputRead"/>
        public void BeginOutputReadLine()
        {
            if (_outputStreamReadMode == StreamReadMode.Undefined)
            {
                _outputStreamReadMode = StreamReadMode.AsyncMode;
            }
            else if (_outputStreamReadMode != StreamReadMode.AsyncMode)
            {
                throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
            }

            if (_pendingOutputRead)
                throw new InvalidOperationException(SR.PendingAsyncOperation);

            _pendingOutputRead = true;
            // We can't detect if there's a pending synchronous read, stream also doesn't.
            if (_output == null)
            {
                if (_standardOutput == null)
                {
                    throw new InvalidOperationException(SR.CantGetStandardOut);
                }

                Stream s = _standardOutput.BaseStream;
                _output = new AsyncStreamReader(s, OutputReadNotifyUser, _standardOutput.CurrentEncoding);
            }
            _output.BeginReadLine();
        }

        /// <summary>Begins asynchronous read operations on the redirected <see cref="System.Diagnostics.Process.StandardError" /> stream of the application.</summary>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Diagnostics.Process.StandardError%2A> stream can be read synchronously or asynchronously. Methods such as <xref:System.IO.StreamReader.Read%2A>, <xref:System.IO.StreamReader.ReadLine%2A>, and <xref:System.IO.StreamReader.ReadToEnd%2A> perform synchronous read operations on the error output stream of the process. These synchronous read operations do not complete until the associated <xref:System.Diagnostics.Process> writes to its <xref:System.Diagnostics.Process.StandardError%2A> stream, or closes the stream.
        /// In contrast, <xref:System.Diagnostics.Process.BeginErrorReadLine%2A> starts asynchronous read operations on the <xref:System.Diagnostics.Process.StandardError%2A> stream. This method enables the designated event handler for the stream output and immediately returns to the caller, which can perform other work while the stream output is directed to the event handler.
        /// Follow these steps to perform asynchronous read operations on <xref:System.Diagnostics.Process.StandardError%2A> for a <xref:System.Diagnostics.Process> :
        /// 1.  Set <xref:System.Diagnostics.ProcessStartInfo.UseShellExecute%2A> to `false`.
        /// 2.  Set <xref:System.Diagnostics.ProcessStartInfo.RedirectStandardError%2A> to `true`.
        /// 3.  Add your event handler to the <xref:System.Diagnostics.Process.ErrorDataReceived> event. The event handler must match the <xref:System.Diagnostics.DataReceivedEventHandler?displayProperty=nameWithType> delegate signature.
        /// 4.  Start the <xref:System.Diagnostics.Process>.
        /// 5.  Call <xref:System.Diagnostics.Process.BeginErrorReadLine%2A> for the <xref:System.Diagnostics.Process>. This call starts asynchronous read operations on <xref:System.Diagnostics.Process.StandardError%2A>.
        /// When asynchronous read operations start, the event handler is called each time the associated <xref:System.Diagnostics.Process> writes a line of text to its <xref:System.Diagnostics.Process.StandardError%2A> stream.
        /// You can cancel an asynchronous read operation by calling <xref:System.Diagnostics.Process.CancelErrorRead%2A>. The read operation can be canceled by the caller or by the event handler. After canceling, you can call <xref:System.Diagnostics.Process.BeginErrorReadLine%2A> again to resume asynchronous read operations.
        /// > [!NOTE]
        /// >  You cannot mix asynchronous and synchronous read operations on a redirected stream. Once the redirected stream of a <xref:System.Diagnostics.Process> is opened in either asynchronous or synchronous mode, all further read operations on that stream must be in the same mode. For example, do not follow <xref:System.Diagnostics.Process.BeginErrorReadLine%2A> with a call to <xref:System.IO.StreamReader.ReadLine%2A> on the <xref:System.Diagnostics.Process.StandardError%2A> stream, or vice versa. However, you can read two different streams in different modes. For example, you can call <xref:System.Diagnostics.Process.BeginErrorReadLine%2A> and then call <xref:System.IO.StreamReader.ReadLine%2A> for the <xref:System.Diagnostics.Process.StandardOutput%2A> stream.
        /// ## Examples
        /// The following example uses the `net view` command to list the available network resources on a remote computer. The user supplies the target computer name as a command-line argument. The user can also supply a file name for error output. The example collects the output of the net command, waits for the process to finish, and then writes the output results to the console. If the user supplies the optional error file, the example writes errors to the file.
        /// [!code-cpp[Process_AsyncStreams#2](~/samples/snippets/cpp/VS_Snippets_CLR/process_asyncstreams/CPP/net_async.cpp#2)]
        /// [!code-csharp[Process_AsyncStreams#2](~/samples/snippets/csharp/VS_Snippets_CLR/process_asyncstreams/CS/net_async.cs#2)]
        /// [!code-vb[Process_AsyncStreams#2](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_asyncstreams/VB/net_async.vb#2)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError" /> property is <see langword="false" />.
        /// -or-
        /// An asynchronous read operation is already in progress on the <see cref="System.Diagnostics.Process.StandardError" /> stream.
        /// -or-
        /// The <see cref="System.Diagnostics.Process.StandardError" /> stream has been used by a synchronous read operation.</exception>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError"/>
        /// <altmember cref="System.Diagnostics.Process.StandardError"/>
        /// <altmember cref="System.Diagnostics.Process.ErrorDataReceived"/>
        /// <altmember cref="System.Diagnostics.DataReceivedEventHandler"/>
        /// <altmember cref="System.Diagnostics.Process.CancelErrorRead"/>
        public void BeginErrorReadLine()
        {
            if (_errorStreamReadMode == StreamReadMode.Undefined)
            {
                _errorStreamReadMode = StreamReadMode.AsyncMode;
            }
            else if (_errorStreamReadMode != StreamReadMode.AsyncMode)
            {
                throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
            }

            if (_pendingErrorRead)
            {
                throw new InvalidOperationException(SR.PendingAsyncOperation);
            }

            _pendingErrorRead = true;
            // We can't detect if there's a pending synchronous read, stream also doesn't.
            if (_error == null)
            {
                if (_standardError == null)
                {
                    throw new InvalidOperationException(SR.CantGetStandardError);
                }

                Stream s = _standardError.BaseStream;
                _error = new AsyncStreamReader(s, ErrorReadNotifyUser, _standardError.CurrentEncoding);
            }
            _error.BeginReadLine();
        }

        /// <summary>Cancels the asynchronous read operation on the redirected <see cref="System.Diagnostics.Process.StandardOutput" /> stream of an application.</summary>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> starts an asynchronous read operation on the <xref:System.Diagnostics.Process.StandardOutput%2A> stream. <xref:System.Diagnostics.Process.CancelOutputRead%2A> ends the asynchronous read operation.
        /// After canceling, you can resume asynchronous read operations by calling <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> again.
        /// When you call <xref:System.Diagnostics.Process.CancelOutputRead%2A>, all in-progress read operations for <xref:System.Diagnostics.Process.StandardOutput%2A> are completed and then the event handler is disabled. All further redirected output to <xref:System.Diagnostics.Process.StandardOutput%2A> is saved in a buffer. If you re-enable the event handler with a call to <xref:System.Diagnostics.Process.BeginOutputReadLine%2A>, the saved output is sent to the event handler and asynchronous read operations resume. If you want to change the event handler before resuming asynchronous read operations, you must remove the existing event handler before adding the new event handler:
        /// ```csharp
        /// // At this point the DataReceivedEventHandler(OutputHandler1)
        /// // has executed a CancelOutputRead.
        /// // Remove the prior event handler.
        /// process.OutputDataReceived -=
        /// new DataReceivedEventHandler(OutputHandler1);
        /// // Register a new event handler.
        /// process.OutputDataReceived +=
        /// new DataReceivedEventHandler(OutputHandler2);
        /// // Call the corresponding BeginOutputReadLine.
        /// process.BeginOutputReadLine();
        /// ```
        /// > [!NOTE]
        /// >  You cannot mix asynchronous and synchronous read operations on the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream. Once the redirected stream of a <xref:System.Diagnostics.Process> is opened in either asynchronous or synchronous mode, all further read operations on that stream must be in the same mode. If you cancel an asynchronous read operation on <xref:System.Diagnostics.Process.StandardOutput%2A> and then need to read from the stream again, you must use <xref:System.Diagnostics.Process.BeginOutputReadLine%2A> to resume asynchronous read operations. Do not follow <xref:System.Diagnostics.Process.CancelOutputRead%2A> with a call to the synchronous read methods of <xref:System.Diagnostics.Process.StandardOutput%2A> such as <xref:System.IO.StreamReader.Read%2A>, <xref:System.IO.StreamReader.ReadLine%2A>, or <xref:System.IO.StreamReader.ReadToEnd%2A>.
        /// ## Examples
        /// The following example starts the `nmake` command with user supplied arguments. The error and output streams are read asynchronously; the collected text lines are displayed to the console as well as written to a log file. If the command output exceeds a specified number of lines, the asynchronous read operations are canceled.
        /// [!code-cpp[Process_AsyncStreams#3](~/samples/snippets/cpp/VS_Snippets_CLR/process_asyncstreams/CPP/nmake_async.cpp#3)]
        /// [!code-csharp[Process_AsyncStreams#3](~/samples/snippets/csharp/VS_Snippets_CLR/process_asyncstreams/CS/nmake_async.cs#3)]
        /// [!code-vb[Process_AsyncStreams#3](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_asyncstreams/VB/nmake_async.vb#3)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Diagnostics.Process.StandardOutput" /> stream is not enabled for asynchronous read operations.</exception>
        /// <altmember cref="System.Diagnostics.Process.BeginOutputReadLine"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput"/>
        /// <altmember cref="System.Diagnostics.Process.StandardOutput"/>
        /// <altmember cref="System.Diagnostics.Process.OutputDataReceived"/>
        /// <altmember cref="System.Diagnostics.DataReceivedEventHandler"/>
        public void CancelOutputRead()
        {
            CheckDisposed();
            if (_output != null)
            {
                _output.CancelOperation();
            }
            else
            {
                throw new InvalidOperationException(SR.NoAsyncOperation);
            }

            _pendingOutputRead = false;
        }

        /// <summary>Cancels the asynchronous read operation on the redirected <see cref="System.Diagnostics.Process.StandardError" /> stream of an application.</summary>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// <xref:System.Diagnostics.Process.BeginErrorReadLine%2A> starts an asynchronous read operation on the <xref:System.Diagnostics.Process.StandardError%2A> stream. <xref:System.Diagnostics.Process.CancelErrorRead%2A> ends the asynchronous read operation.
        /// After canceling, you can resume the asynchronous read operation by calling <xref:System.Diagnostics.Process.BeginErrorReadLine%2A> again.
        /// When you call <xref:System.Diagnostics.Process.CancelErrorRead%2A>, all in-progress read operations for <xref:System.Diagnostics.Process.StandardError%2A> are completed and then the event handler is disabled. All further redirected output to <xref:System.Diagnostics.Process.StandardError%2A> will be lost. If you re-enable the event handler with a call to <xref:System.Diagnostics.Process.BeginErrorReadLine%2A>, asynchronous read operations resume. If you want to change the event handler before resuming asynchronous read operations, you must remove the existing event handler before adding the new event handler:
        /// ```csharp
        /// // At this point the DataReceivedEventHandler(ErrorHandler1)
        /// // has executed a CancelErrorRead.
        /// // Remove the prior event handler.
        /// process.ErrorDataReceived -=
        /// new DataReceivedEventHandler(ErrorHandler1);
        /// // Register a new event handler.
        /// process.ErrorDataReceived +=
        /// new DataReceivedEventHandler(ErrorHandler2);
        /// // Call the corresponding BeginErrorReadLine.
        /// process.BeginErrorReadLine();
        /// ```
        /// > [!NOTE]
        /// >  You cannot mix asynchronous and synchronous read operations on the redirected <xref:System.Diagnostics.Process.StandardError%2A> stream. Once the redirected stream of a <xref:System.Diagnostics.Process> is opened in either asynchronous or synchronous mode, all further read operations on that stream must be in the same mode. If you cancel an asynchronous read operation on <xref:System.Diagnostics.Process.StandardError%2A> and then need to read from the stream again, you must use <xref:System.Diagnostics.Process.BeginErrorReadLine%2A> to resume asynchronous read operations. Do not follow <xref:System.Diagnostics.Process.CancelErrorRead%2A> with a call to the synchronous read methods of <xref:System.Diagnostics.Process.StandardError%2A> such as <xref:System.IO.StreamReader.Read%2A>, <xref:System.IO.StreamReader.ReadLine%2A>, or <xref:System.IO.StreamReader.ReadToEnd%2A>.
        /// ## Examples
        /// The following example starts the `nmake` command with user supplied arguments. The error and output streams are read asynchronously; the collected text lines are displayed to the console as well as written to a log file. If the command output exceeds a specified number of lines, the asynchronous read operations are canceled.
        /// [!code-cpp[Process_AsyncStreams#3](~/samples/snippets/cpp/VS_Snippets_CLR/process_asyncstreams/CPP/nmake_async.cpp#3)]
        /// [!code-csharp[Process_AsyncStreams#3](~/samples/snippets/csharp/VS_Snippets_CLR/process_asyncstreams/CS/nmake_async.cs#3)]
        /// [!code-vb[Process_AsyncStreams#3](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_asyncstreams/VB/nmake_async.vb#3)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Diagnostics.Process.StandardError" /> stream is not enabled for asynchronous read operations.</exception>
        /// <altmember cref="System.Diagnostics.Process.BeginErrorReadLine"/>
        /// <altmember cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError"/>
        /// <altmember cref="System.Diagnostics.Process.StandardError"/>
        /// <altmember cref="System.Diagnostics.Process.ErrorDataReceived"/>
        /// <altmember cref="System.Diagnostics.DataReceivedEventHandler"/>
        public void CancelErrorRead()
        {
            CheckDisposed();
            if (_error != null)
            {
                _error.CancelOperation();
            }
            else
            {
                throw new InvalidOperationException(SR.NoAsyncOperation);
            }

            _pendingErrorRead = false;
        }

        internal void OutputReadNotifyUser(string? data)
        {
            // To avoid race between remove handler and raising the event
            DataReceivedEventHandler? outputDataReceived = OutputDataReceived;
            if (outputDataReceived != null)
            {
                // Call back to user informing data is available
                DataReceivedEventArgs e = new DataReceivedEventArgs(data);
                if (SynchronizingObject is ISynchronizeInvoke syncObj && syncObj.InvokeRequired)
                {
                    syncObj.Invoke(outputDataReceived, new object[] { this, e });
                }
                else
                {
                    outputDataReceived(this, e);
                }
            }
        }

        internal void ErrorReadNotifyUser(string? data)
        {
            // To avoid race between remove handler and raising the event
            DataReceivedEventHandler? errorDataReceived = ErrorDataReceived;
            if (errorDataReceived != null)
            {
                // Call back to user informing data is available.
                DataReceivedEventArgs e = new DataReceivedEventArgs(data);
                if (SynchronizingObject is ISynchronizeInvoke syncObj && syncObj.InvokeRequired)
                {
                    syncObj.Invoke(errorDataReceived, new object[] { this, e });
                }
                else
                {
                    errorDataReceived(this, e);
                }
            }
        }

        /// <summary>Throws a System.ObjectDisposedException if the Proces was disposed</summary>
        /// <exception cref="System.ObjectDisposedException">If the Proces has been disposed.</exception>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// This enum defines the operation mode for redirected process stream.
        /// We don't support switching between synchronous mode and asynchronous mode.
        /// </summary>
        private enum StreamReadMode
        {
            Undefined,
            SyncMode,
            AsyncMode
        }

        /// <summary>A desired internal state.</summary>
        private enum State
        {
            HaveId = 0x1,
            IsLocal = 0x2,
            HaveNonExitedId = HaveId | 0x4,
            HaveProcessInfo = 0x8,
            Exited = 0x10,
            Associated = 0x20,
        }
    }
}
