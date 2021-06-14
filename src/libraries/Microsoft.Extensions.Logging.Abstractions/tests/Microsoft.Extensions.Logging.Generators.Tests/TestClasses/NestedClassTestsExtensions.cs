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
                [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "M9")]
                public static partial void M9(ILogger logger);
            }
        }
    }
    public class ABC {}
}