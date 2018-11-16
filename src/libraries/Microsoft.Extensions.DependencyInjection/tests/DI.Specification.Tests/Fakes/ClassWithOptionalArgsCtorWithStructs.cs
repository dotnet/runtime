// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class ClassWithOptionalArgsCtorWithStructs
    {
        public ConsoleColor? Color { get; }
        public ConsoleColor? ColorNull { get; }

        public int? Integer { get; }
        public int? IntegerNull { get; }

        public ClassWithOptionalArgsCtorWithStructs(
            DateTime dateTime = new DateTime(),
            DateTime dateTimeDefault = default(DateTime),
            TimeSpan timeSpan = new TimeSpan(),
            TimeSpan timeSpanDefault = default(TimeSpan),
            DateTimeOffset dateTimeOffset = new DateTimeOffset(),
            DateTimeOffset dateTimeOffsetDefault = default(DateTimeOffset),
            Guid guid = new Guid(),
            Guid guidDefault = default(Guid),
            CustomStruct customStruct = new CustomStruct(),
            CustomStruct customStructDefault = default(CustomStruct),
            ConsoleColor? color = ConsoleColor.DarkGreen,
            ConsoleColor? colorNull = null,
            int? integer = 12,
            int? integerNull = null
        )
        {
            Color = color;
            ColorNull = colorNull;
            Integer = integer;
            IntegerNull = integerNull;
        }

        public struct CustomStruct { }
    }
}
