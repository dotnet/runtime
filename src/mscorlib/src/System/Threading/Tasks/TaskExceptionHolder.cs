// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// An abstraction for holding and aggregating exceptions.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

// Disable the "reference to volatile field not treated as volatile" error.
#pragma warning disable 0420

namespace System.Threading.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Runtime.ExceptionServices;
    using System.Security;

    /// <summary>
    /// An exception holder manages a list of exceptions for one particular task.
    /// It offers the ability to aggregate, but more importantly, also offers intrinsic
    /// support for propagating unhandled exceptions that are never observed. It does
    /// this by aggregating and throwing if the holder is ever GC'd without the holder's
    /// contents ever having been requested (e.g. by a Task.Wait, Task.get_Exception, etc).
    /// This behavior is prominent in .NET 4 but is suppressed by default beyond that release.
    /// </summary>
    internal class TaskExceptionHolder
    {
        /// <summary>Whether we should propagate exceptions on the finalizer.</summary>
        private readonly static bool s_failFastOnUnobservedException = ShouldFailFastOnUnobservedException();
        /// <summary>Whether the AppDomain has started to unload.</summary>
        private static volatile bool s_domainUnloadStarted;
        /// <summary>An event handler used to notify of domain unload.</summary>
        private static volatile EventHandler s_adUnloadEventHandler;

        /// <summary>The task with which this holder is associated.</summary>
        private readonly Task m_task;
        /// <summary>
        /// The lazily-initialized list of faulting exceptions.  Volatile
        /// so that it may be read to determine whether any exceptions were stored.
        /// </summary>
        private volatile List<ExceptionDispatchInfo> m_faultExceptions;
        /// <summary>An exception that triggered the task to cancel.</summary>
        private ExceptionDispatchInfo m_cancellationException;
        /// <summary>Whether the holder was "observed" and thus doesn't cause finalization behavior.</summary>
        private volatile bool m_isHandled;

        /// <summary>
        /// Creates a new holder; it will be registered for finalization.
        /// </summary>
        /// <param name="task">The task this holder belongs to.</param>
        internal TaskExceptionHolder(Task task)
        {
            Contract.Requires(task != null, "Expected a non-null task.");
            m_task = task;
            EnsureADUnloadCallbackRegistered();
        }

        [SecuritySafeCritical]
        private static bool ShouldFailFastOnUnobservedException()
        {
            bool shouldFailFast = false;
            #if !FEATURE_CORECLR
            shouldFailFast = System.CLRConfig.CheckThrowUnobservedTaskExceptions();
            #endif
            return shouldFailFast;
        }

        private static void EnsureADUnloadCallbackRegistered()
        {
            if (s_adUnloadEventHandler == null && 
                Interlocked.CompareExchange( ref s_adUnloadEventHandler,
                                             AppDomainUnloadCallback, 
                                             null) == null)
            {
                AppDomain.CurrentDomain.DomainUnload += s_adUnloadEventHandler;
            }
        }

        private static void AppDomainUnloadCallback(object sender, EventArgs e)
        {
            s_domainUnloadStarted = true;
        }

        /// <summary>
        /// A finalizer that repropagates unhandled exceptions.
        /// </summary>
        ~TaskExceptionHolder()
        {
            // Raise unhandled exceptions only when we know that neither the process or nor the appdomain is being torn down.
            // We need to do this filtering because all TaskExceptionHolders will be finalized during shutdown or unload
            // regardles of reachability of the task (i.e. even if the user code was about to observe the task's exception),
            // which can otherwise lead to spurious crashes during shutdown.
            if (m_faultExceptions != null && !m_isHandled && 
                !Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload() && !s_domainUnloadStarted)
            {
                // We don't want to crash the finalizer thread if any ThreadAbortExceptions 
                // occur in the list or in any nested AggregateExceptions.  
                // (Don't rethrow ThreadAbortExceptions.)
                foreach (ExceptionDispatchInfo edi in m_faultExceptions)
                {
                    var exp = edi.SourceException;
                    AggregateException aggExp = exp as AggregateException;
                    if (aggExp != null)
                    {
                        AggregateException flattenedAggExp = aggExp.Flatten();
                        foreach (Exception innerExp in flattenedAggExp.InnerExceptions)
                        {
                            if (innerExp is ThreadAbortException)
                                return;
                        }
                    }
                    else if (exp is ThreadAbortException)
                    {
                        return;
                    }
                }

                // We will only propagate if this is truly unhandled. The reason this could
                // ever occur is somewhat subtle: if a Task's exceptions are observed in some
                // other finalizer, and the Task was finalized before the holder, the holder
                // will have been marked as handled before even getting here.

                // Give users a chance to keep this exception from crashing the process
                
                // First, publish the unobserved exception and allow users to observe it
                AggregateException exceptionToThrow = new AggregateException(
                    Environment.GetResourceString("TaskExceptionHolder_UnhandledException"),
                    m_faultExceptions);
                UnobservedTaskExceptionEventArgs ueea = new UnobservedTaskExceptionEventArgs(exceptionToThrow);
                TaskScheduler.PublishUnobservedTaskException(m_task, ueea);
                
                // Now, if we are still unobserved and we're configured to crash on unobserved, throw the exception.
                // We need to publish the event above even if we're not going to crash, hence
                // why this check doesn't come at the beginning of the method.
                if (s_failFastOnUnobservedException && !ueea.m_observed)
                {
                    throw exceptionToThrow;
                }
            }
        }

        /// <summary>Gets whether the exception holder is currently storing any exceptions for faults.</summary>
        internal bool ContainsFaultList { get { return m_faultExceptions != null; } }

        /// <summary>
        /// Add an exception to the holder.  This will ensure the holder is
        /// in the proper state (handled/unhandled) depending on the list's contents.
        /// </summary>
        /// <param name="exceptionObject">
        /// An exception object (either an Exception, an ExceptionDispatchInfo,
        /// an IEnumerable{Exception}, or an IEnumerable{ExceptionDispatchInfo}) 
        /// to add to the list.
        /// </param>
        /// <remarks>
        /// Must be called under lock.
        /// </remarks>
        internal void Add(object exceptionObject)
        {
            Add(exceptionObject, representsCancellation: false);
        }

        /// <summary>
        /// Add an exception to the holder.  This will ensure the holder is
        /// in the proper state (handled/unhandled) depending on the list's contents.
        /// </summary>
        /// <param name="representsCancellation">
        /// Whether the exception represents a cancellation request (true) or a fault (false).
        /// </param>
        /// <param name="exceptionObject">
        /// An exception object (either an Exception, an ExceptionDispatchInfo,
        /// an IEnumerable{Exception}, or an IEnumerable{ExceptionDispatchInfo}) 
        /// to add to the list.
        /// </param>
        /// <remarks>
        /// Must be called under lock.
        /// </remarks>
        internal void Add(object exceptionObject, bool representsCancellation)
        {
            Contract.Requires(exceptionObject != null, "TaskExceptionHolder.Add(): Expected a non-null exceptionObject");
            Contract.Requires(
                exceptionObject is Exception || exceptionObject is IEnumerable<Exception> || 
                exceptionObject is ExceptionDispatchInfo || exceptionObject is IEnumerable<ExceptionDispatchInfo>,
                "TaskExceptionHolder.Add(): Expected Exception, IEnumerable<Exception>, ExceptionDispatchInfo, or IEnumerable<ExceptionDispatchInfo>");

            if (representsCancellation) SetCancellationException(exceptionObject);
            else AddFaultException(exceptionObject);
        }

        /// <summary>Sets the cancellation exception.</summary>
        /// <param name="exceptionObject">The cancellation exception.</param>
        /// <remarks>
        /// Must be called under lock.
        /// </remarks>
        private void SetCancellationException(object exceptionObject)
        {
            Contract.Requires(exceptionObject != null, "Expected exceptionObject to be non-null.");
            
            Contract.Assert(m_cancellationException == null, 
                "Expected SetCancellationException to be called only once.");
                // Breaking this assumption will overwrite a previously OCE,
                // and implies something may be wrong elsewhere, since there should only ever be one.

            Contract.Assert(m_faultExceptions == null, 
                "Expected SetCancellationException to be called before any faults were added.");
                // Breaking this assumption shouldn't hurt anything here, but it implies something may be wrong elsewhere.
                // If this changes, make sure to only conditionally mark as handled below.

            // Store the cancellation exception
            var oce = exceptionObject as OperationCanceledException;
            if (oce != null)
            {
                m_cancellationException = ExceptionDispatchInfo.Capture(oce);
            }
            else
            {
                var edi = exceptionObject as ExceptionDispatchInfo;
                Contract.Assert(edi != null && edi.SourceException is OperationCanceledException,
                    "Expected an OCE or an EDI that contained an OCE");
                m_cancellationException = edi;
            }

            // This is just cancellation, and there are no faults, so mark the holder as handled.
            MarkAsHandled(false);
        }

        /// <summary>Adds the exception to the fault list.</summary>
        /// <param name="exceptionObject">The exception to store.</param>
        /// <remarks>
        /// Must be called under lock.
        /// </remarks>
        private void AddFaultException(object exceptionObject)
        {
            Contract.Requires(exceptionObject != null, "AddFaultException(): Expected a non-null exceptionObject");

            // Initialize the exceptions list if necessary.  The list should be non-null iff it contains exceptions.
            var exceptions = m_faultExceptions;
            if (exceptions == null) m_faultExceptions = exceptions = new List<ExceptionDispatchInfo>(1);
            else Contract.Assert(exceptions.Count > 0, "Expected existing exceptions list to have > 0 exceptions.");

            // Handle Exception by capturing it into an ExceptionDispatchInfo and storing that
            var exception = exceptionObject as Exception;
            if (exception != null)
            {
                exceptions.Add(ExceptionDispatchInfo.Capture(exception));
            }
            else
            {
                // Handle ExceptionDispatchInfo by storing it into the list
                var edi = exceptionObject as ExceptionDispatchInfo;
                if (edi != null)
                {
                    exceptions.Add(edi);
                }
                else
                {
                    // Handle enumerables of exceptions by capturing each of the contained exceptions into an EDI and storing it
                    var exColl = exceptionObject as IEnumerable<Exception>;
                    if (exColl != null)
                    {
#if DEBUG
                        int numExceptions = 0;
#endif
                        foreach (var exc in exColl)
                        {
#if DEBUG
                            Contract.Assert(exc != null, "No exceptions should be null");
                            numExceptions++;
#endif
                            exceptions.Add(ExceptionDispatchInfo.Capture(exc));
                        }
#if DEBUG
                        Contract.Assert(numExceptions > 0, "Collection should contain at least one exception.");
#endif
                    }
                    else
                    {
                        // Handle enumerables of EDIs by storing them directly
                        var ediColl = exceptionObject as IEnumerable<ExceptionDispatchInfo>;
                        if (ediColl != null)
                        {
                            exceptions.AddRange(ediColl);
#if DEBUG
                            Contract.Assert(exceptions.Count > 0, "There should be at least one dispatch info.");
                            foreach(var tmp in exceptions)
                            {
                                Contract.Assert(tmp != null, "No dispatch infos should be null");
                            }
#endif
                        }
                            // Anything else is a programming error
                        else
                        {
                            throw new ArgumentException(Environment.GetResourceString("TaskExceptionHolder_UnknownExceptionType"), "exceptionObject");
                        }
                    }
                }
            }
                

            // If all of the exceptions are ThreadAbortExceptions and/or
            // AppDomainUnloadExceptions, we do not want the finalization
            // probe to propagate them, so we consider the holder to be
            // handled.  If a subsequent exception comes in of a different
            // kind, we will reactivate the holder.
            for (int i = 0; i < exceptions.Count; i++)
            {
                var t = exceptions[i].SourceException.GetType();
                if (t != typeof(ThreadAbortException) && t != typeof(AppDomainUnloadedException))
                {
                    MarkAsUnhandled();
                    break;
                }
                else if (i == exceptions.Count - 1)
                {
                    MarkAsHandled(false);
                }
            }
        }

        /// <summary>
        /// A private helper method that ensures the holder is considered
        /// unhandled, i.e. it is registered for finalization.
        /// </summary>
        private void MarkAsUnhandled()
        {
            // If a thread partially observed this thread's exceptions, we
            // should revert back to "not handled" so that subsequent exceptions
            // must also be seen. Otherwise, some could go missing. We also need
            // to reregister for finalization.
            if (m_isHandled)
            {
                GC.ReRegisterForFinalize(this);
                m_isHandled = false;
            }
        }

        /// <summary>
        /// A private helper method that ensures the holder is considered
        /// handled, i.e. it is not registered for finalization.
        /// </summary>
        /// <param name="calledFromFinalizer">Whether this is called from the finalizer thread.</param> 
        internal void MarkAsHandled(bool calledFromFinalizer)
        {
            if (!m_isHandled)
            {
                if (!calledFromFinalizer)
                {
                    GC.SuppressFinalize(this);
                }

                m_isHandled = true;
            }
        }

        /// <summary>
        /// Allocates a new aggregate exception and adds the contents of the list to
        /// it. By calling this method, the holder assumes exceptions to have been
        /// "observed", such that the finalization check will be subsequently skipped.
        /// </summary>
        /// <param name="calledFromFinalizer">Whether this is being called from a finalizer.</param>
        /// <param name="includeThisException">An extra exception to be included (optionally).</param>
        /// <returns>The aggregate exception to throw.</returns>
        internal AggregateException CreateExceptionObject(bool calledFromFinalizer, Exception includeThisException)
        {
            var exceptions = m_faultExceptions;
            Contract.Assert(exceptions != null, "Expected an initialized list.");
            Contract.Assert(exceptions.Count > 0, "Expected at least one exception.");

            // Mark as handled and aggregate the exceptions.
            MarkAsHandled(calledFromFinalizer);

            // If we're only including the previously captured exceptions, 
            // return them immediately in an aggregate.
            if (includeThisException == null)
                return new AggregateException(exceptions);

            // Otherwise, the caller wants a specific exception to be included, 
            // so return an aggregate containing that exception and the rest.
            Exception[] combinedExceptions = new Exception[exceptions.Count + 1];
            for (int i = 0; i < combinedExceptions.Length - 1; i++)
            {
                combinedExceptions[i] = exceptions[i].SourceException;
            }
            combinedExceptions[combinedExceptions.Length - 1] = includeThisException;
            return new AggregateException(combinedExceptions);
        }

        /// <summary>
        /// Wraps the exception dispatch infos into a new read-only collection. By calling this method, 
        /// the holder assumes exceptions to have been "observed", such that the finalization 
        /// check will be subsequently skipped.
        /// </summary>
        internal ReadOnlyCollection<ExceptionDispatchInfo> GetExceptionDispatchInfos()
        {
            var exceptions = m_faultExceptions;
            Contract.Assert(exceptions != null, "Expected an initialized list.");
            Contract.Assert(exceptions.Count > 0, "Expected at least one exception.");
            MarkAsHandled(false);
            return new ReadOnlyCollection<ExceptionDispatchInfo>(exceptions);
        }

        /// <summary>
        /// Gets the ExceptionDispatchInfo representing the singular exception 
        /// that was the cause of the task's cancellation.
        /// </summary>
        /// <returns>
        /// The ExceptionDispatchInfo for the cancellation exception.  May be null.
        /// </returns>
        internal ExceptionDispatchInfo GetCancellationExceptionDispatchInfo()
        {
            var edi = m_cancellationException;
            Contract.Assert(edi == null || edi.SourceException is OperationCanceledException,
                "Expected the EDI to be for an OperationCanceledException");
            return edi;
        }
    }
}
