// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.ExceptionServices
{
    // This class defines support for separating the exception dispatch details
    // (like stack trace, watson buckets, etc) from the actual managed exception
    // object. This allows us to track error (via the exception object) independent
    // of the path the error takes.
    //
    // This is particularly useful for frameworks that wish to propagate
    // exceptions (i.e. errors to be precise) across threads.
    public sealed class ExceptionDispatchInfo
    {
        private readonly Exception _exception;
        private readonly Exception.DispatchState _dispatchState;

        private ExceptionDispatchInfo(Exception exception)
        {
            _exception = exception;
            _dispatchState = exception.CaptureDispatchState();
        }

        // This static method is used to create an instance of ExceptionDispatchInfo for
        // the specified exception object and save all the required details that maybe
        // needed to be propagated when the exception is "rethrown" on a different thread.
        public static ExceptionDispatchInfo Capture(Exception source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ExceptionDispatchInfo(source);
        }

        // Return the exception object represented by this ExceptionDispatchInfo instance
        public Exception SourceException => _exception;

        // When a framework needs to "Rethrow" an exception on a thread different (but not necessarily so) from
        // where it was thrown, it should invoke this method against the ExceptionDispatchInfo
        // created for the exception in question.
        //
        // This method will restore the original stack trace and bucketing details before throwing
        // the exception so that it is easy, from debugging standpoint, to understand what really went wrong on
        // the original thread.
        [DoesNotReturn]
        [StackTraceHidden]
        public void Throw()
        {
            // Restore the exception dispatch details before throwing the exception.
            _exception.RestoreDispatchState(_dispatchState);
            throw _exception;
        }

        // Throws the source exception, maintaining the original bucketing details and augmenting
        // rather than replacing the original stack trace.
        [DoesNotReturn]
        [StackTraceHidden]
        public static void Throw(Exception source) => Capture(source).Throw();

        /// <summary>Stores the current stack trace into the specified <see cref="Exception"/> instance.</summary>
        /// <param name="source">The unthrown <see cref="Exception"/> instance.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="source"/> argument was null.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> argument was previously thrown or previously had a stack trace stored into it.</exception>
        /// <returns>The <paramref name="source"/> exception instance.</returns>
        [StackTraceHidden]
        public static Exception SetCurrentStackTrace(Exception source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            source.SetCurrentStackTrace();

            return source;
        }

        /// <summary>
        /// Stores the provided stack trace into the specified <see cref="Exception"/> instance.
        /// </summary>
        /// <param name="source">The unthrown <see cref="Exception"/> instance.</param>
        /// <param name="stackTrace">The stack trace string to persist within <paramref name="source"/>. This is normally acquired
        /// from the <see cref="Exception.StackTrace"/> property from the remote exception instance.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="source"/> or <paramref name="stackTrace"/> argument was null.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> argument was previously thrown or previously had a stack trace stored into it.</exception>
        /// <returns>The <paramref name="source"/> exception instance.</returns>
        /// <remarks>
        /// This method populates the <see cref="Exception.StackTrace"/> property from an arbitrary string value.
        /// The typical use case is the transmission of <see cref="Exception"/> objects across processes with high fidelity,
        /// allowing preservation of the exception object's stack trace information. .NET does not attempt to parse the
        /// provided string value.
        ///
        /// The caller is responsible for canonicalizing line endings if required. <see cref="string.ReplaceLineEndings"/>
        /// can be used to canonicalize line endings.
        /// </remarks>
        public static Exception SetRemoteStackTrace(Exception source, string stackTrace)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (stackTrace is null)
            {
                throw new ArgumentNullException(nameof(stackTrace));
            }

            source.SetRemoteStackTrace(stackTrace);

            return source;
        }
    }
}
