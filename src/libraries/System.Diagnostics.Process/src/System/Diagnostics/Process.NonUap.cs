// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
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

                IEnumerable<Exception> result = KillTree();

                if (result.Any())
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
                Process current = this;

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
        private IReadOnlyList<Process> GetChildProcesses(Process[] processes = null)
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
