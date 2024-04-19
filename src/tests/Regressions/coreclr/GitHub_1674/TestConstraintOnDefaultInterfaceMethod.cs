// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace TestConstraint
{
    public class TestConstraintOnDefaultInterfaceMethod
    {
        [Fact]
        public static void TestEntryPoint()
        {
            IAuditTrail<IRaftLogEntry> auditTrail = new AuditTrail();
            // This should not fail per C# specs here:
            // https://github.com/dotnet/csharplang/blob/master/spec/classes.md#type-parameter-constraints
            auditTrail.AppendAsync<EmptyLogEntry>(new EmptyLogEntry(), CancellationToken.None);

            // This should work since int always meets the constraint of IBuggy<T1>.Foo<T2> where T2: T1
            ((IBuggy<int>)new Worky()).Foo<int>();
            // This should work since Object always meets the constraint of IBuggy<T1>.Foo<T2> where T2: T1
            ((IBuggy<object>)new Worky2()).Foo<string>();
            // This should not throw since Open meets the constraint of IBuggy<T1>.Foo<T2> where T2: T1
            ((IBuggy<Open>)new Buggy()).Foo<Open>();
        }

        interface IBuggy<T1>
        {
            public void Foo<T2>() where T2 : T1 => Console.WriteLine($"Works for type: {typeof(T1)}");
        }
        public class Worky : IBuggy<int> { }
        public class Worky2 : IBuggy<object> { }
        public class Buggy : IBuggy<Open> { }
        public class Open { }


        private interface ILogEntry
        {
            long Index { get; }
        }

        private interface IAuditTrail<TRecord>
            where TRecord : class, ILogEntry
        {
            ValueTask AppendAsync<TRecordImpl>(TRecordImpl impl, CancellationToken token)
                where TRecordImpl : notnull, TRecord {Console.WriteLine("works.."); return new ValueTask();}
        }

        private interface IRaftLogEntry : ILogEntry
        {
            long Term { get; }
        }

        private sealed class AuditTrail : IAuditTrail<IRaftLogEntry>
        {
        }

        private readonly struct EmptyLogEntry : IRaftLogEntry
        {
            long IRaftLogEntry.Term => 0L;

            long ILogEntry.Index => 0L;
        }
    }
}
