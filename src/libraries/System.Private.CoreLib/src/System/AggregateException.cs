// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
    /// <summary>Represents one or more errors that occur during application execution.</summary>
    /// <remarks>
    /// <see cref="AggregateException"/> is used to consolidate multiple failures into a single, throwable
    /// exception object.
    /// </remarks>
    [Serializable]
    [DebuggerDisplay("Count = {InnerExceptionCount}")]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class AggregateException : Exception
    {
        private readonly Exception[] _innerExceptions; // Complete set of exceptions.
        private ReadOnlyCollection<Exception>? _rocView; // separate from _innerExceptions to enable trimming if InnerExceptions isn't used

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class.
        /// </summary>
        public AggregateException()
            : this(SR.AggregateException_ctor_DefaultMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public AggregateException(string? message)
            : base(message ?? SR.AggregateException_ctor_DefaultMessage)
        {
            _innerExceptions = Array.Empty<Exception>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="innerException"/> argument
        /// is null.</exception>
        public AggregateException(string? message, Exception innerException)
            : base(message ?? SR.AggregateException_ctor_DefaultMessage, innerException)
        {
            ArgumentNullException.ThrowIfNull(innerException);

            _innerExceptions = new[] { innerException };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(IEnumerable<Exception> innerExceptions) :
            this(SR.AggregateException_ctor_DefaultMessage, innerExceptions ?? throw new ArgumentNullException(nameof(innerExceptions)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(params Exception[] innerExceptions) :
            this(SR.AggregateException_ctor_DefaultMessage, innerExceptions ?? throw new ArgumentNullException(nameof(innerExceptions)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(string? message, IEnumerable<Exception> innerExceptions)
            : this(message, new List<Exception>(innerExceptions ?? throw new ArgumentNullException(nameof(innerExceptions))).ToArray(), cloneExceptions: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(string? message, params Exception[] innerExceptions) :
            this(message, innerExceptions ?? throw new ArgumentNullException(nameof(innerExceptions)), cloneExceptions: true)
        {
        }

        private AggregateException(string? message, Exception[] innerExceptions, bool cloneExceptions) :
            base(message ?? SR.AggregateException_ctor_DefaultMessage, innerExceptions.Length > 0 ? innerExceptions[0] : null)
        {
            _innerExceptions = cloneExceptions ? new Exception[innerExceptions.Length] : innerExceptions;

            for (int i = 0; i < _innerExceptions.Length; i++)
            {
                _innerExceptions[i] = innerExceptions[i];

                if (innerExceptions[i] == null)
                {
                    throw new ArgumentException(SR.AggregateException_ctor_InnerExceptionNull);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// references to the inner exception dispatch info objects that represent the cause of this exception.
        /// </summary>
        /// <param name="innerExceptionInfos">
        /// Information about the exceptions that are the cause of the current exception.
        /// </param>
        /// <exception cref="ArgumentNullException">The <paramref name="innerExceptionInfos"/> argument
        /// is null.</exception>
        /// <exception cref="ArgumentException">An element of <paramref name="innerExceptionInfos"/> is
        /// null.</exception>
        internal AggregateException(List<ExceptionDispatchInfo> innerExceptionInfos) :
            this(SR.AggregateException_ctor_DefaultMessage, innerExceptionInfos)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and references to the inner exception dispatch info objects that represent the cause of
        /// this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptionInfos">
        /// Information about the exceptions that are the cause of the current exception.
        /// </param>
        /// <exception cref="ArgumentNullException">The <paramref name="innerExceptionInfos"/> argument
        /// is null.</exception>
        /// <exception cref="ArgumentException">An element of <paramref name="innerExceptionInfos"/> is
        /// null.</exception>
        internal AggregateException(string message, List<ExceptionDispatchInfo> innerExceptionInfos)
            : base(message, innerExceptionInfos.Count != 0 ? innerExceptionInfos[0].SourceException : null)
        {
            _innerExceptions = new Exception[innerExceptionInfos.Count];

            for (int i = 0; i < _innerExceptions.Length; i++)
            {
                _innerExceptions[i] = innerExceptionInfos[i].SourceException;
                Debug.Assert(_innerExceptions[i] != null);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds
        /// the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that
        /// contains contextual information about the source or destination. </param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> argument is null.</exception>
        /// <exception cref="SerializationException">The exception could not be deserialized correctly.</exception>
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected AggregateException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
            Exception[]? innerExceptions = info.GetValue("InnerExceptions", typeof(Exception[])) as Exception[]; // Do not rename (binary serialization)
            if (innerExceptions is null)
            {
                throw new SerializationException(SR.AggregateException_DeserializationFailure);
            }

            _innerExceptions = innerExceptions;
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about
        /// the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds
        /// the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that
        /// contains contextual information about the source or destination. </param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> argument is null.</exception>
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("InnerExceptions", _innerExceptions, typeof(Exception[])); // Do not rename (binary serialization)
        }

        /// <summary>
        /// Returns the <see cref="AggregateException"/> that is the root cause of this exception.
        /// </summary>
        public override Exception GetBaseException()
        {
            // Returns the first inner AggregateException that contains more or less than one inner exception

            // Recursively traverse the inner exceptions as long as the inner exception of type AggregateException and has only one inner exception
            Exception? back = this;
            AggregateException? backAsAggregate = this;
            while (backAsAggregate != null && backAsAggregate.InnerExceptions.Count == 1)
            {
                back = back!.InnerException;
                backAsAggregate = back as AggregateException;
            }
            return back!;
        }

        /// <summary>
        /// Gets a read-only collection of the <see cref="Exception"/> instances that caused the
        /// current exception.
        /// </summary>
        public ReadOnlyCollection<Exception> InnerExceptions => _rocView ??= new ReadOnlyCollection<Exception>(_innerExceptions);


        /// <summary>
        /// Invokes a handler on each <see cref="Exception"/> contained by this <see
        /// cref="AggregateException"/>.
        /// </summary>
        /// <param name="predicate">The predicate to execute for each exception. The predicate accepts as an
        /// argument the <see cref="Exception"/> to be processed and returns a Boolean to indicate
        /// whether the exception was handled.</param>
        /// <remarks>
        /// Each invocation of the <paramref name="predicate"/> returns true or false to indicate whether the
        /// <see cref="Exception"/> was handled. After all invocations, if any exceptions went
        /// unhandled, all unhandled exceptions will be put into a new <see cref="AggregateException"/>
        /// which will be thrown. Otherwise, the <see cref="Handle"/> method simply returns. If any
        /// invocations of the <paramref name="predicate"/> throws an exception, it will halt the processing
        /// of any more exceptions and immediately propagate the thrown exception as-is.
        /// </remarks>
        /// <exception cref="AggregateException">An exception contained by this <see
        /// cref="AggregateException"/> was not handled.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="predicate"/> argument is
        /// null.</exception>
        public void Handle(Func<Exception, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            List<Exception>? unhandledExceptions = null;
            for (int i = 0; i < _innerExceptions.Length; i++)
            {
                // If the exception was not handled, lazily allocate a list of unhandled
                // exceptions (to be rethrown later) and add it.
                if (!predicate(_innerExceptions[i]))
                {
                    unhandledExceptions ??= new List<Exception>();
                    unhandledExceptions.Add(_innerExceptions[i]);
                }
            }

            // If there are unhandled exceptions remaining, throw them.
            if (unhandledExceptions != null)
            {
                throw new AggregateException(Message, unhandledExceptions.ToArray(), cloneExceptions: false);
            }
        }


        /// <summary>
        /// Flattens the inner instances of <see cref="AggregateException"/> by expanding its contained <see cref="Exception"/> instances
        /// into a new <see cref="AggregateException"/>
        /// </summary>
        /// <returns>A new, flattened <see cref="AggregateException"/>.</returns>
        /// <remarks>
        /// If any inner exceptions are themselves instances of
        /// <see cref="AggregateException"/>, this method will recursively flatten all of them. The
        /// inner exceptions returned in the new <see cref="AggregateException"/>
        /// will be the union of all of the inner exceptions from exception tree rooted at the provided
        /// <see cref="AggregateException"/> instance.
        /// </remarks>
        public AggregateException Flatten()
        {
            // Initialize a collection to contain the flattened exceptions.
            List<Exception> flattenedExceptions = new List<Exception>();

            // Create a list to remember all aggregates to be flattened, this will be accessed like a FIFO queue
            var exceptionsToFlatten = new List<AggregateException> { this };
            int nDequeueIndex = 0;

            // Continue removing and recursively flattening exceptions, until there are no more.
            while (exceptionsToFlatten.Count > nDequeueIndex)
            {
                // dequeue one from exceptionsToFlatten
                ReadOnlyCollection<Exception> currentInnerExceptions = exceptionsToFlatten[nDequeueIndex++].InnerExceptions;

                for (int i = 0; i < currentInnerExceptions.Count; i++)
                {
                    Exception currentInnerException = currentInnerExceptions[i];

                    if (currentInnerException == null)
                    {
                        continue;
                    }

                    // If this exception is an aggregate, keep it around for later.  Otherwise,
                    // simply add it to the list of flattened exceptions to be returned.
                    if (currentInnerException is AggregateException currentInnerAsAggregate)
                    {
                        exceptionsToFlatten.Add(currentInnerAsAggregate);
                    }
                    else
                    {
                        flattenedExceptions.Add(currentInnerException);
                    }
                }
            }

            return new AggregateException(GetType() == typeof(AggregateException) ? base.Message : Message, flattenedExceptions.ToArray(), cloneExceptions: false);
        }

        /// <summary>Gets a message that describes the exception.</summary>
        public override string Message
        {
            get
            {
                if (_innerExceptions.Length == 0)
                {
                    return base.Message;
                }

                var sb = new ValueStringBuilder(stackalloc char[256]);
                sb.Append(base.Message);
                sb.Append(' ');
                for (int i = 0; i < _innerExceptions.Length; i++)
                {
                    sb.Append('(');
                    sb.Append(_innerExceptions[i].Message);
                    sb.Append(") ");
                }
                sb.Length--;
                return sb.ToString();
            }
        }

        /// <summary>
        /// Creates and returns a string representation of the current <see cref="AggregateException"/>.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            text.Append(base.ToString());

            for (int i = 0; i < _innerExceptions.Length; i++)
            {
                if (_innerExceptions[i] == InnerException)
                    continue; // Already logged in base.ToString()

                text.Append(Environment.NewLineConst + InnerExceptionPrefix);
                text.AppendFormat(CultureInfo.InvariantCulture, SR.AggregateException_InnerException, i);
                text.Append(_innerExceptions[i].ToString());
                text.Append("<---");
                text.AppendLine();
            }

            return text.ToString();
        }

        /// <summary>
        /// This helper property is used by the DebuggerDisplay.
        ///
        /// Note that we don't want to remove this property and change the debugger display to {InnerExceptions.Count}
        /// because DebuggerDisplay should be a single property access or parameterless method call, so that the debugger
        /// can use a fast path without using the expression evaluator.
        ///
        /// See https://docs.microsoft.com/en-us/visualstudio/debugger/using-the-debuggerdisplay-attribute
        /// </summary>
        internal int InnerExceptionCount => _innerExceptions.Length;

        internal Exception[] InternalInnerExceptions => _innerExceptions;
    }
}
