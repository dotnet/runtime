// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace Microsoft.NET.Build.Tasks
{
    public abstract class TaskBase : Task
    {
        private Logger? _logger;

        internal TaskBase(Logger? logger = null)
        {
            _logger = logger;
        }

        internal new Logger Log
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new LogAdapter(base.Log);
                }

                return _logger;
            }
        }

        public override bool Execute()
        {
            try
            {
                ExecuteCore();
            }
            catch (BuildErrorException e)
            {
                Log.LogError(e.Message);
            }
            catch (Exception e)
            {
                LogErrorTelemetry("taskBaseCatchException", e);
                throw;
            }

            return !Log.HasLoggedErrors;
        }

        private void LogErrorTelemetry(string eventName, Exception e)
        {
            (BuildEngine as IBuildEngine5)?.LogTelemetry(eventName, new Dictionary<string, string> {
                        {"exceptionType", e.GetType().ToString() },
                        {"detail", ExceptionToStringWithoutMessage(e) }});
        }

        private static string ExceptionToStringWithoutMessage(Exception e)
        {
            const string AggregateException_ToString = "{0}{1}---> (Inner Exception #{2}) {3}{4}{5}";
            if (e is AggregateException aggregate)
            {
                string text = NonAggregateExceptionToStringWithoutMessage(aggregate);

                for (int i = 0; i < aggregate.InnerExceptions.Count; i++)
                {
                    text = string.Format(CultureInfo.InvariantCulture,
                                         AggregateException_ToString,
                                         text,
                                         Environment.NewLine,
                                         i,
                                         ExceptionToStringWithoutMessage(aggregate.InnerExceptions[i]),
                                         "<---",
                                         Environment.NewLine);
                }

                return text;
            }
            else
            {
                return NonAggregateExceptionToStringWithoutMessage(e);
            }
        }

        private static string NonAggregateExceptionToStringWithoutMessage(Exception e)
        {
            string s;
            const string Exception_EndOfInnerExceptionStack = "--- End of inner exception stack trace ---";


            s = e.GetType().ToString();

            if (e.InnerException != null)
            {
                s = s + " ---> " + ExceptionToStringWithoutMessage(e.InnerException) + Environment.NewLine +
                "   " + Exception_EndOfInnerExceptionStack;

            }

            var stackTrace = e.StackTrace;

            if (stackTrace != null)
            {
                s += Environment.NewLine + stackTrace;
            }

            return s;
        }

        protected abstract void ExecuteCore();
    }
}
