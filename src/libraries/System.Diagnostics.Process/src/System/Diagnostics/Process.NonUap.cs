// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;

namespace System.Diagnostics
{
    /// <summary>Provides access to local and remote processes and enables you to start and stop local system processes.</summary>
    /// <remarks>A <see cref="System.Diagnostics.Process" /> component provides access to a process that is running on a computer. A process, in the simplest terms, is a running app. A thread is the basic unit to which the operating system allocates processor time. A thread can execute any part of the code of the process, including parts currently being executed by another thread.
    /// The <see cref="System.Diagnostics.Process" /> component is a useful tool for starting, stopping, controlling, and monitoring apps. You can use the <see cref="System.Diagnostics.Process" /> component, to obtain a list of the processes that are running, or you can start a new process. A <see cref="System.Diagnostics.Process" /> component is used to access system processes. After a <see cref="System.Diagnostics.Process" /> component has been initialized, it can be used to obtain information about the running process. Such information includes the set of threads, the loaded modules (.dll and .exe files), and performance information such as the amount of memory the process is using.
    /// This type implements the <see cref="System.IDisposable" /> interface. When you have finished using the type, you should dispose of it either directly or indirectly. To dispose of the type directly, call its <see cref="System.IDisposable.Dispose" /> method in a `try`/`finally` block. To dispose of it indirectly, use a language construct such as `using` (in C#) or `Using` (in Visual Basic). For more information, see the "Using an Object that Implements IDisposable" section in the <see cref="System.IDisposable" /> interface topic.
    /// > [!NOTE]
    /// >  32-bit processes cannot access the modules of a 64-bit process. If you try to get information about a 64-bit process from a 32-bit process, you will get a <see cref="System.ComponentModel.Win32Exception" /> exception. A 64-bit process, on the other hand, can access the modules of a 32-bit process.
    /// The process component obtains information about a group of properties all at once. After the <see cref="System.Diagnostics.Process" /> component has obtained information about one member of any group, it will cache the values for the other properties in that group and not obtain new information about the other members of the group until you call the <see cref="System.Diagnostics.Process.Refresh" /> method. Therefore, a property value is not guaranteed to be any newer than the last call to the <see cref="System.Diagnostics.Process.Refresh" /> method. The group breakdowns are operating-system dependent.
    /// If you have a path variable declared in your system using quotes, you must fully qualify that path when starting any process found in that location. Otherwise, the system will not find the path. For example, if `c:\mypath` is not in your path, and you add it using quotation marks: `path = %path%;"c:\mypath"`, you must fully qualify any process in `c:\mypath` when starting it.
    /// A system process is uniquely identified on the system by its process identifier. Like many Windows resources, a process is also identified by its handle, which might not be unique on the computer. A handle is the generic term for an identifier of a resource. The operating system persists the process handle, which is accessed through the <see cref="System.Diagnostics.Process.Handle" /> property of the <see cref="System.Diagnostics.Process" /> component, even when the process has exited. Thus, you can get the process's administrative information, such as the <see cref="System.Diagnostics.Process.ExitCode" /> (usually either zero for success or a nonzero error code) and the <see cref="System.Diagnostics.Process.ExitTime" />. Handles are an extremely valuable resource, so leaking handles is more virulent than leaking memory.
    /// > [!NOTE]
    /// >  This class contains a link demand and an inheritance demand at the class level that applies to all members. A <see cref="System.Security.SecurityException" /> is thrown when either the immediate caller or the derived class does not have full-trust permission. For details about security demands, see [Link Demands](/dotnet/framework/misc/link-demands).
    /// <a name="Core"></a>
    /// ## [!INCLUDE[net_core](~/includes/net-core-md.md)] Notes
    /// In the .NET Framework, the <see cref="System.Diagnostics.Process" /> class by default uses <see cref="System.Console" /> encodings, which are typically code page encodings, for the input, output, and error streams. For example code, on systems whose culture is English (United States), code page 437 is the default encoding for the <see cref="System.Console" /> class. However, [!INCLUDE[net_core](~/includes/net-core-md.md)] may make only a limited subset of these encodings available. If this is the case, it uses <see cref="System.Text.Encoding.UTF8" /> as the default encoding.
    /// If a <see cref="System.Diagnostics.Process" /> object depends on specific code page encodings, you can still make them available by doing the following *before* you call any <see cref="System.Diagnostics.Process" /> methods:
    /// 1.  Add a reference to the System.Text.Encoding.CodePages.dll assembly to your project.
    /// 2.  Retrieve the <see cref="System.Text.EncodingProvider" /> object from the <see cref="System.Text.CodePagesEncodingProvider.Instance" /> property.
    /// 3.  Pass the <see cref="System.Text.EncodingProvider" /> object to the <see cref="System.Text.Encoding.RegisterProvider" /> method to make the additional encodings supported by the encoding provider available.
    /// The <see cref="System.Diagnostics.Process" /> class will then automatically use the default system encoding rather than UTF8, provided that you have registered the encoding provider before calling any <see cref="System.Diagnostics.Process" /> methods.
    /// ## Examples
    /// The following example uses an instance of the <see cref="System.Diagnostics.Process" /> class to start a process.
    /// [!code-cpp[Process.Start_instance#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_instance/CPP/processstart.cpp#1)]
    /// [!code-csharp[Process.Start_instance#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_instance/CS/processstart.cs#1)]
    /// [!code-vb[Process.Start_instance#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_instance/VB/processstart.vb#1)]
    /// The following example uses the <see cref="System.Diagnostics.Process" /> class itself and a static <see cref="System.Diagnostics.Process.Start" /> method to start a process.
    /// [!code-cpp[Process.Start_static#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process.Start_static/CPP/processstartstatic.cpp)]
    /// [!code-csharp[Process.Start_static#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process.Start_static/CS/processstartstatic.cs)]
    /// [!code-vb[Process.Start_static#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process.Start_static/VB/processstartstatic.vb)]
    /// The following F# example defines a `runProc` function that starts a process, captures all output and error information, and records the number of milliseconds that the process has run.  The `runProc` function has three parameters: the name of application to launch, the arguments to supply to the application, and the starting directory.
    /// [!code-fsharp[System.Diagnostics.Process#1](~/samples/snippets/fsharp/VS_Snippets_CLR_System/system.diagnostics.process/fs/Start1.fs#1)]
    /// The code for the `runProc` function was written by [ImaginaryDevelopment](http://fssnip.net/authors/ImaginaryDevelopment) and is available under the [Microsoft Public License](https://opensource.org/licenses/ms-pl).</remarks>
    /// <altmember cref="System.Diagnostics.Process.Start"/>
    /// <altmember cref="System.Diagnostics.ProcessStartInfo"/>
    /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
    /// <altmember cref="System.Diagnostics.Process.Kill"/>
    /// <altmember cref="System.Diagnostics.ProcessThread"/>
    /// <related type="ExternalDocumentation" href="https://code.msdn.microsoft.com/windowsdesktop/Using-the-NET-Process-Class-d70597ef">Using the .NET Process Class</related>
    public partial class Process : IDisposable
    {
        /// <summary>Immediately stops the associated process, and optionally its child/descendent processes.</summary>
        /// <param name="entireProcessTree"><see langword="true" /> to kill the associated process and its descendants; <see langword="false" /> to kill only the associated process.</param>
        /// <remarks>When <paramref name="entireProcessTree" /> is set to <see langword="true" />, processes where the call lacks permissions to view details are silently skipped by the descendant termination process because the termination process is unable to determine whether those processes are descendants.</remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">The associated process could not be terminated.
        /// -or-
        /// The process is terminating.</exception>
        /// <exception cref="System.NotSupportedException">You are attempting to call <see cref="System.Diagnostics.Process.Kill" /> for a process that is running on a remote computer. The method is available only for processes running on the local computer.</exception>
        /// <exception cref="System.InvalidOperationException">The process has already exited.
        /// -or-
        /// There is no process associated with this <see cref="System.Diagnostics.Process" /> object.
        /// -or-
        /// The calling process is a member of the associated process' descendant tree.</exception>
        /// <exception cref="System.AggregateException">Not all processes in the associated process' descendant tree could be terminated.</exception>
        /// <altmember cref="System.Environment.Exit(int)"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="System.Diagnostics.Process.Start"/>
        public void Kill(bool entireProcessTree)
        {
            if (!entireProcessTree)
            {
                Kill();
            }
            else
            {
                EnsureState(State.Associated | State.IsLocal);

                if (IsSelfOrDescendantOf(GetCurrentProcess()))
                    throw new InvalidOperationException(SR.KillEntireProcessTree_DisallowedBecauseTreeContainsCallingProcess);

                List<Exception>? result = KillTree();

                if (result != null && result.Count != 0)
                    throw new AggregateException(SR.KillEntireProcessTree_TerminationIncomplete, result);
            }
        }

        private bool IsSelfOrDescendantOf(Process processOfInterest)
        {
            if (SafePredicateTest(() => Equals(processOfInterest)))
                return true;

            Process[] allProcesses = GetProcesses();

            try
            {
                var descendantProcesses = new Queue<Process>();
                Process? current = this;

                do
                {
                    foreach (Process candidate in current.GetChildProcesses(allProcesses))
                    {
                        if (SafePredicateTest(() => processOfInterest.Equals(candidate)))
                            return true;

                        descendantProcesses.Enqueue(candidate);
                    }
                } while (descendantProcesses.TryDequeue(out current));
            }
            finally
            {
                foreach (Process process in allProcesses)
                {
                    process.Dispose();
                }
            }

            return false;
        }

        /// <summary>
        /// Returns all immediate child processes.
        /// </summary>
        private IReadOnlyList<Process> GetChildProcesses(Process[]? processes = null)
        {
            bool internallyInitializedProcesses = processes == null;
            processes = processes ?? GetProcesses();

            List<Process> childProcesses = new List<Process>();

            foreach (Process possibleChildProcess in processes)
            {
                // Only support disposing if this method initialized the set of processes being searched
                bool dispose = internallyInitializedProcesses;

                try
                {
                    if (SafePredicateTest(() => IsParentOf(possibleChildProcess)))
                    {
                        childProcesses.Add(possibleChildProcess);
                        dispose = false;
                    }
                }
                finally
                {
                    if (dispose)
                        possibleChildProcess.Dispose();
                }
            }

            return childProcesses;
        }

        private bool SafePredicateTest(Func<bool> predicate)
        {
            try
            {
                return predicate();
            }
            catch (Exception e) when (e is InvalidOperationException || e is Win32Exception)
            {
                // InvalidOperationException signifies conditions such as the process already being dead.
                // Win32Exception signifies issues such as insufficient permissions to get details on the process.
                // In either case, the predicate couldn't be applied so return the fallback result.
                return false;
            }
        }
    }
}
