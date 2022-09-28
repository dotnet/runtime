// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
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
            if (Equals(processOfInterest))
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
                        if (processOfInterest.Equals(candidate))
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
        private List<Process> GetChildProcesses(Process[]? processes = null)
        {
            bool internallyInitializedProcesses = processes == null;
            processes ??= GetProcesses();

            List<Process> childProcesses = new List<Process>();

            foreach (Process possibleChildProcess in processes)
            {
                // Only support disposing if this method initialized the set of processes being searched
                bool dispose = internallyInitializedProcesses;

                try
                {
                    if (IsParentOf(possibleChildProcess))
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

        private static bool IsProcessInvalidException(Exception e) =>
            // InvalidOperationException signifies conditions such as the process already being dead.
            // Win32Exception signifies issues such as insufficient permissions to get details on the process.
            // In either case, the predicate couldn't be applied so return the fallback result.
            e is InvalidOperationException || e is Win32Exception;
    }
}
