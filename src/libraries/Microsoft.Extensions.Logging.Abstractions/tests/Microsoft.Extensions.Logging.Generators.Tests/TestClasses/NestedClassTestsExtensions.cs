// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class NestedClassTestsExtensions<T> where T : ABC
    {
        internal static partial class NestedMiddleParentClass
        {
            internal static partial class NestedClass
            {
                [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "M8")]
                public static partial void M8(ILogger logger);
            }
        }
    }
    
    internal partial class NonStaticNestedClassTestsExtensions<T> where T : ABC
    {
        internal partial class NonStaticNestedMiddleParentClass
        {
            internal static partial class NestedClass
            {
                [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "M9")]
                public static partial void M9(ILogger logger);
            }
        }
    }
    public class ABC {}

    public partial struct NestedStruct
    {
        internal static partial class Logger
        {
            [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "M10")]
            public static partial void M10(ILogger logger);
        }
    }

    public partial record NestedRecord(string Name, string Address)
    {
        internal static partial class Logger
        {
            [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "M11")]
            public static partial void M11(ILogger logger);
        }
    }

    public static partial class MultiLevelNestedClass
    {
        public partial struct NestedStruct
        {
            internal partial record NestedRecord(string Name, string Address)
            {
                internal static partial class Logger
                {
                    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "M12")]
                    public static partial void M12(ILogger logger);
                }
            }
        }
    }
}
