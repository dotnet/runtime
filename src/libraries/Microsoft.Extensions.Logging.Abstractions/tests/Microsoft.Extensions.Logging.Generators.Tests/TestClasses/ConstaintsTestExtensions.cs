// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    using ConstraintInAnotherNamespace;
    namespace UsesConstraintInAnotherNamespace
    {
        public partial class MessagePrinter<T>
            where T : Message
        {
            public void Print(ILogger logger, T message)
            {
                Log.Message(logger, message.Text);
            }

            internal static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "The message is {Text}.")]
                internal static partial void Message(ILogger logger, string? text);
            }
        }

        public partial class MessagePrinterHasConstraintOnLogClassAndLogMethod<T>
            where T : Message
        {
            public void Print(ILogger logger, T message)
            {
                Log<Message>.Message(logger, message);
            }

            internal static partial class Log<U> where U : Message
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "The message is {Text}.")]
                internal static partial void Message(ILogger logger, U text);
            }
        }
    }

    internal static partial class ConstraintsTestExtensions<T>
        where T : class
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T dummy)
        {
        }
    }

    internal static partial class ConstraintsTestExtensions1<T>
        where T : struct
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T dummy)
        {
        }
    }

    internal static partial class ConstraintsTestExtensions2<T>
        where T : unmanaged
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T dummy)
        {
        }
    }

    internal static partial class ConstraintsTestExtensions3<T>
        where T : new()
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T dummy)
        {
        }
    }

    internal static partial class ConstraintsTestExtensions4<T>
        where T : System.Attribute
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T dummy)
        {
        }
    }

    internal static partial class ConstraintsTestExtensions5<T>
        where T : notnull
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T dummy)
        {
        }
    }
}

namespace ConstraintInAnotherNamespace
{
    public class Message
    {
        public string? Text { get; set; }

        public override string ToString()
        {
            return $"`{Text}`";
        }
    }
}
