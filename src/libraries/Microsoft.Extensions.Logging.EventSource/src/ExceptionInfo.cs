// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// Represents information about exceptions that is captured by EventSourceLogger
    /// </summary>
    [System.Diagnostics.Tracing.EventData(Name ="ExceptionInfo")]
    internal sealed class ExceptionInfo
    {
        public static ExceptionInfo Empty { get; } = new ExceptionInfo();

        private ExceptionInfo()
        {
        }

        public ExceptionInfo(Exception exception)
        {
            TypeName = exception.GetType().FullName;
            Message = exception.Message;
            HResult = exception.HResult;
            VerboseMessage = exception.ToString();
        }

        public string TypeName { get; }
        public string Message { get; }
        public int HResult { get; }
        public string VerboseMessage { get; } // This is the ToString() of the Exception
    }
}
