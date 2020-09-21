// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;

namespace Microsoft.Extensions.DependencyInjection.Tests.Fakes
{
    public class ClassWithServiceAndOptionalArgsCtorWithStructs
    {
        public DateTime DateTime { get; }
        public DateTime DateTimeDefault { get; }
        public TimeSpan TimeSpan { get; }
        public TimeSpan TimeSpanDefault { get; }
        public DateTimeOffset DateTimeOffset { get; }
        public DateTimeOffset DateTimeOffsetDefault { get; }
        public Guid Guid { get; }
        public Guid GuidDefault { get; }
        public CustomStruct CustomStructValue { get; }
        public CustomStruct CustomStructDefault { get; }

        public ClassWithServiceAndOptionalArgsCtorWithStructs(IFakeService fake,
            DateTime dateTime = new DateTime(),
            DateTime dateTimeDefault = default(DateTime),
            TimeSpan timeSpan = new TimeSpan(),
            TimeSpan timeSpanDefault = default(TimeSpan),
            DateTimeOffset dateTimeOffset = new DateTimeOffset(),
            DateTimeOffset dateTimeOffsetDefault = default(DateTimeOffset),
            Guid guid = new Guid(),
            Guid guidDefault = default(Guid),
            CustomStruct customStruct = new CustomStruct(),
            CustomStruct customStructDefault = default(CustomStruct)
        )
        {
            DateTime = dateTime;
            DateTimeDefault = dateTimeDefault;
            TimeSpan = timeSpan;
            TimeSpanDefault = timeSpanDefault;
            DateTimeOffset = dateTimeOffset;
            DateTimeOffsetDefault = dateTimeOffsetDefault;
            Guid = guid;
            GuidDefault = guidDefault;
            CustomStructValue = customStruct;
            CustomStructDefault = customStructDefault;
        }

        public struct CustomStruct { }
    }
}
