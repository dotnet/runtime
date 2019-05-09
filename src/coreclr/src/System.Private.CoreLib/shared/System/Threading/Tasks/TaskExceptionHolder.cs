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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks
{
    /// <summary>
    /// An exception holder manages a list of exceptions for one particular task.
    /// It offers the ability to aggregate, but more importantly, also offers intrinsic
    /// support for propagating unhandled exceptions that are never observed. It does
    /// this by aggregating and calling UnobservedTaskException event event if the holder 
    /// is ever GC'd without the holder's contents ever having been requested 
    /// (e.g. by a Task.Wait, Task.get_Exception, etc).
    /// </summary>
    internal class TaskExceptionHolder
    {
        /// <summary>The task with which this holder is associated.</summary>
        private readonly Task m_task;
        /// <summary>
        /// The lazily-initialized list of faulting exceptions.  Volatile
        /// so that it may be read to determine whether any exceptions were stored.
        /// </summary>
        private volatile List<ExceptionDispatchInfo>? m_faultExceptions;
        /// <summary>An exception that triggered the task to cancel.</summary>
        private ExceptionDispatchInfo? m_cancellationException;
        /// <summary>Whether the holder was "observed" and thus doesn't cause finalization behavior.</summary>
        private volatile bool m_isHandled;

        /// <summary>
        /// Creates a new holder; it will be registered for finalization.
        /// </summary>
        /// <param name="task">The task this holder belongs to.</param>
        internal TaskExceptionHolder(Task task)
        {
            Debug.Assert(task != null, "Expected a non-null task.");
            m_task = task;
        }

        /// <summary>
        /// A finalizer that repropagates unhandled exceptions.
        /// </summary>
        ~TaskExceptionHolder()
        {
            if (m_faultExceptions != null && !m_isHandled)
            {
                // We will only propagate if this is truly unhandled. The reason this could
                // ever occur is somewhat subtle: if a Task's exceptions are observed in some
                // other finalizer, and the Task was finalized before the holder, the holder
                // will have been marked as handled before even getting here.

                // Publish the unobserved exception and allow users to observe it
                AggregateException exceptionToThrow = new AggregateException(
                    SR.TaskExceptionHolder_UnhandledException,
                    m_faultExceptions);
                UnobservedTaskExceptionEventArgs ueea = new UnobservedTaskExceptionEventArgs(exceptionToThrow);
                TaskScheduler.PublishUnobservedTaskException(m_task, ueea);
            }
        }

        /// <summary>Gets whether the exception holder is currently storing any exceptions for faults.</summary>
        internal bool ContainsFaultList { get { return m_faultExceptions != null; } }

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
            Debug.Assert(exceptionObject != null, "TaskExceptionHolder.Add(): Expected a non-null exceptionObject");
            Debug.Assert(
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
            Debug.Assert(exceptionObject != null, "Expected exceptionObject to be non-null.");

            Debug.Assert(m_cancellationException == null,
                "Expected SetCancellationException to be called only once.");
            // Breaking this assumption will overwrite a previously OCE,
            // and implies something may be wrong elsewhere, since there should only ever be one.

            Debug.Assert(m_faultExceptions == null,
                "Expected SetCancellationException to be called before any faults were added.");
            // Breaking this assumption shouldn't hurt anything here, but it implies something may be wrong elsewhere.
            // If this changes, make sure to only conditionally mark as handled below.

            // Store the cancellation exception
            if (exceptionObject is OperationCanceledException oce)
            {
                m_cancellationException = ExceptionDispatchInfo.Capture(oce);
            }
            else
            {
                var edi = exceptionObject as ExceptionDispatchInfo;
                Debug.Assert(edi != null && edi.SourceException is OperationCanceledException,
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
            Debug.Assert(exceptionObject != null, "AddFaultException(): Expected a non-null exceptionObject");

            // Initialize the exceptions list if necessary.  The list should be non-null iff it contains exceptions.
            var exceptions = m_faultExceptions;
            if (exceptions == null) m_faultExceptions = exceptions = new List<ExceptionDispatchInfo>(1);
            else Debug.Assert(exceptions.Count > 0, "Expected existing exceptions list to have > 0 exceptions.");

            // Handle Exception by capturing it into an ExceptionDispatchInfo and storing that
            if (exceptionObject is Exception exception)
            {
                exceptions.Add(ExceptionDispatchInfo.Capture(exception));
            }
            else
            {
                // Handle ExceptionDispatchInfo by storing it into the list
                if (exceptionObject is ExceptionDispatchInfo edi)
                {
                    exceptions.Add(edi);
                }
                else
                {
                    // Handle enumerables of exceptions by capturing each of the contained exceptions into an EDI and storing it
                    if (exceptionObject is IEnumerable<Exception> exColl)
                    {
#if DEBUG
                        int numExceptions = 0;
#endif
                        foreach (var exc in exColl)
                        {
#if DEBUG
                            Debug.Assert(exc != null, "No exceptions should be null");
                            numExceptions++;
#endif
                            exceptions.Add(ExceptionDispatchInfo.Capture(exc));
                        }
#if DEBUG
                        Debug.Assert(numExceptions > 0, "Collection should contain at least one exception.");
#endif
                    }
                    else
                    {
                        // Handle enumerables of EDIs by storing them directly
                        if (exceptionObject is IEnumerable<ExceptionDispatchInfo> ediColl)
                        {
                            exceptions.AddRange(ediColl);
#if DEBUG
                            Debug.Assert(exceptions.Count > 0, "There should be at least one dispatch info.");
                            foreach (var tmp in exceptions)
                            {
                                Debug.Assert(tmp != null, "No dispatch infos should be null");
                            }
#endif
                        }
                        // Anything else is a programming error
                        else
                        {
                            throw new ArgumentException(SR.TaskExceptionHolder_UnknownExceptionType, nameof(exceptionObject));
                        }
                    }
                }
            }

            if (exceptions.Count > 0)
                MarkAsUnhandled();
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
        internal AggregateException CreateExceptionObject(bool calledFromFinalizer, Exception? includeThisException)
        {
            var exceptions = m_faultExceptions;
            Debug.Assert(exceptions != null, "Expected an initialized list.");
            Debug.Assert(exceptions.Count > 0, "Expected at least one exception.");

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
            List<ExceptionDispatchInfo>? exceptions = m_faultExceptions;
            Debug.Assert(exceptions != null, "Expected an initialized list.");
            Debug.Assert(exceptions.Count > 0, "Expected at least one exception.");
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
        internal ExceptionDispatchInfo? GetCancellationExceptionDispatchInfo()
        {
            var edi = m_cancellationException;
            Debug.Assert(edi == null || edi.SourceException is OperationCanceledException,
                "Expected the EDI to be for an OperationCanceledException");
            return edi;
        }
    }
}
