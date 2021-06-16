// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class ClassWithOptionalArgsCtorWithStructs
    {
        public ConsoleColor? Color { get; }
        public ConsoleColor? ColorNull { get; }

        public int? Integer { get; }
        public int? IntegerNull { get; }

        // re-enable once https://github.com/dotnet/csharplang/issues/99 is implemented
        // see https://github.com/dotnet/runtime/issues/49069
        //public StructWithPublicDefaultConstructor StructWithConstructor { get; }

#pragma warning disable SA1129
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
            //StructWithPublicDefaultConstructor structWithConstructor = default
        )
#pragma warning restore SA1129
        {
            Color = color;
            ColorNull = colorNull;
            Integer = integer;
            IntegerNull = integerNull;
            //StructWithConstructor = structWithConstructor;
        }

        public struct CustomStruct { }
    }
}
