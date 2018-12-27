// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// Represents information about exceptions that is captured by EventSourceLogger
    /// </summary>
    [System.Diagnostics.Tracing.EventData(Name ="ExceptionInfo")]
    internal class ExceptionInfo
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
