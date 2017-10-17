// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// An exception for task schedulers.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Represents an exception used to communicate an invalid operation by a
    /// <see cref="T:System.Threading.Tasks.TaskScheduler"/>.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class TaskSchedulerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Tasks.TaskSchedulerException"/> class.
        /// </summary>
        public TaskSchedulerException() : base(SR.TaskSchedulerException_ctor_DefaultMessage) // 
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Tasks.TaskSchedulerException"/>
        /// class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public TaskSchedulerException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Tasks.TaskSchedulerException"/>
        /// class using the default error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TaskSchedulerException(Exception innerException)
            : base(SR.TaskSchedulerException_ctor_DefaultMessage, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Tasks.TaskSchedulerException"/>
        /// class with a specified error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TaskSchedulerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Tasks.TaskSchedulerException"/>
        /// class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds
        /// the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that
        /// contains contextual information about the source or destination. </param>
        protected TaskSchedulerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
