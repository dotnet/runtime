// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    using System;
    using NamespaceForABC;

    public partial class NestedClassWithNoTypeConstraintTestsExtensions<T>
    {
        public void M7(ILogger logger) => Log.M7(logger);

        private static partial class Log
        {
            [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "M7")]
            public static partial void M7(ILogger logger);
        }
    }

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

    public partial class NestedClassWithGenericTypesWithAttributesTestsExtensions<[Foo] A, [Bar] B, C>
    {
        public void M13<D>(A a, B b, C c, ILogger logger) => Log<D>.M13(logger, a, b, c);

        private static partial class Log<[Foo] D>
        {
            [LoggerMessage(EventId = 13, Level = LogLevel.Debug, Message = "M13: A = {a}; B = {b}; C = {c}")]
            public static partial void M13(ILogger logger, A a, B b, C c);
        }
    }

    [AttributeUsage(AttributeTargets.GenericParameter)]
    public sealed class FooAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.GenericParameter)]
    public sealed class BarAttribute : Attribute { }
}

namespace NamespaceForABC
{
    public class ABC {}
}