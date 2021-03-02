// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Tests;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class ClassWithOptionalArgsCtorWithStructs
    {
        public ConsoleColor? Color { get; }
        public ConsoleColor? ColorNull { get; }

        public int? Integer { get; }
        public int? IntegerNull { get; }

        public StructWithPublicDefaultConstructor StructWithConstructor { get; }

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
            int? integerNull = null,
            StructWithPublicDefaultConstructor structWithConstructor = default
        )
        {
            Color = color;
            ColorNull = colorNull;
            Integer = integer;
            IntegerNull = integerNull;
            StructWithConstructor = structWithConstructor;
        }

        public struct CustomStruct { }
    }
}
