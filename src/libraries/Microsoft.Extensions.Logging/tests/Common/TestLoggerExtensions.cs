// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Test
{
    public static class TestLoggerExtensions
    {
        public class ScopeWithoutAnyParameters
        {
            public static Func<ILogger, IDisposable> ScopeDelegate;
            public const string Message = "Order creation started.";
            public const string OriginalFormat = Message;

            static ScopeWithoutAnyParameters()
            {
                ScopeDelegate = LoggerMessage.DefineScope(Message);
            }
        }

        public class ActionMatchedInfo
        {
            public static Action<ILogger, string, string, Exception> MessageDelegate;
            public const string NamedStringFormat = "Request matched controller '{controller}' and action '{action}'.";
            public const string FormatString = "Request matched controller '{0}' and action '{1}'.";

            static ActionMatchedInfo()
            {
                MessageDelegate = LoggerMessage.Define<string, string>(
                    LogLevel.Information,
                    eventId: 1,
                    formatString: NamedStringFormat);
            }
        }

        public class ScopeWithOneParameter
        {
            public static Func<ILogger, string, IDisposable> ScopeDelegate;
            public const string NamedStringFormat = "RequestId: {RequestId}";
            public const string FormatString = "RequestId: {0}";

            static ScopeWithOneParameter()
            {
                ScopeDelegate = LoggerMessage.DefineScope<string>(NamedStringFormat);
            }
        }

        public class ScopeInfoWithTwoParameters
        {
            public static Func<ILogger, string, string, IDisposable> ScopeDelegate;
            public const string NamedStringFormat = "{param1}, {param2}";
            public const string FormatString = "{0}, {1}";

            static ScopeInfoWithTwoParameters()
            {
                ScopeDelegate = LoggerMessage.DefineScope<string, string>(NamedStringFormat);
            }
        }

        public class ScopeInfoWithThreeParameters
        {
            public static Func<ILogger, string, string, int, IDisposable> ScopeDelegate;
            public const string NamedStringFormat = "{param1}, {param2}, {param3}";
            public const string FormatString = "{0}, {1}, {2}";

            static ScopeInfoWithThreeParameters()
            {
                ScopeDelegate = LoggerMessage.DefineScope<string, string, int>(NamedStringFormat);
            }
        }

        public static void ActionMatched(
            this ILogger logger, string controller, string action, Exception exception = null)
        {
            ActionMatchedInfo.MessageDelegate(logger, controller, action, exception);
        }

        public static IDisposable ScopeWithoutAnyParams(this ILogger logger)
        {
            return ScopeWithoutAnyParameters.ScopeDelegate(logger);
        }

        public static IDisposable ScopeWithOneParam(this ILogger logger, string requestId)
        {
            return ScopeWithOneParameter.ScopeDelegate(logger, requestId);
        }

        public static IDisposable ScopeWithTwoParams(this ILogger logger, string param1, string param2)
        {
            return ScopeInfoWithTwoParameters.ScopeDelegate(logger, param1, param2);
        }

        public static IDisposable ScopeWithThreeParams(this ILogger logger, string param1, string param2, int param3)
        {
            return ScopeInfoWithThreeParameters.ScopeDelegate(logger, param1, param2, param3);
        }
    }
}
