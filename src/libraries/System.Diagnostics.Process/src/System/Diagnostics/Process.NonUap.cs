// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;

namespace System.Diagnostics
{
    /// <summary>Provides access to local and remote processes and enables you to start and stop local system processes.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// [!INCLUDE[remarks](~/includes/remarks/System.Diagnostics/Process/Process.md)]
    /// ]]></format></remarks>
    /// <altmember cref="O:System.Diagnostics.Process.Start"/>
    /// <altmember cref="System.Diagnostics.ProcessStartInfo"/>
    /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
    /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
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
        /// <exception cref="System.NotSupportedException">You are attempting to call <see cref="O:System.Diagnostics.Process.Kill" /> for a process that is running on a remote computer. The method is available only for processes running on the local computer.</exception>
        /// <exception cref="System.InvalidOperationException">The process has already exited.
        /// -or-
        /// There is no process associated with this <see cref="System.Diagnostics.Process" /> object.
        /// -or-
        /// The calling process is a member of the associated process' descendant tree.</exception>
        /// <exception cref="System.AggregateException">Not all processes in the associated process' descendant tree could be terminated.</exception>
        /// <altmember cref="System.Environment.Exit(int)"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Start"/>
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
