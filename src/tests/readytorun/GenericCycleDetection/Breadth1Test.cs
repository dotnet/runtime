// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    // This test exercises the "breadth cutoff" parameter of generic cycle detector.
    // It mimics generic representation of expressions.
    private struct Expression<T, U>
    {
        public static int Construct(int seed)
        {
            if (--seed <= 0) return 10;
            return seed switch
            {
                1 => Assignment<U, T>.Construct(seed),
                2 => Assignment<Expression<U, T>, T>.Construct(seed),
                3 => Assignment<U, Expression<U, T>>.Construct(seed),
                _ => Assignment<Expression<U, T>, Expression<T, U>>.Construct(seed)
            };
        }
    }
    
    private struct Assignment<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => Conditional<U, T>.Construct(seed),
                2 => Conditional<Assignment<U, T>, T>.Construct(seed),
                3 => Conditional<U, Assignment<U, T>>.Construct(seed),
                _ => Conditional<Assignment<U, T>, Assignment<T, U>>.Construct(seed)
            };
        }
    }

    private struct Conditional<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => LogicalOr<U, T>.Construct(seed),
                2 => LogicalOr<Conditional<U, T>, T>.Construct(seed),
                3 => LogicalOr<U, Conditional<U, T>>.Construct(seed),
                _ => LogicalOr<Conditional<U, T>, Conditional<T, U>>.Construct(seed)
            };
        }
    }

    private struct LogicalOr<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => LogicalAnd<U, T>.Construct(seed),
                2 => LogicalAnd<LogicalOr<U, T>, T>.Construct(seed),
                3 => LogicalAnd<U, LogicalOr<U, T>>.Construct(seed),
                _ => LogicalAnd<LogicalOr<U, T>, LogicalOr<T, U>>.Construct(seed)
            };
        }
    }

    private struct LogicalAnd<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => BitwiseOr<U, T>.Construct(seed),
                2 => BitwiseOr<LogicalAnd<U, T>, T>.Construct(seed),
                3 => BitwiseOr<U, LogicalAnd<U, T>>.Construct(seed),
                _ => BitwiseOr<LogicalAnd<U, T>, LogicalAnd<T, U>>.Construct(seed),
            };
        }
    }

    private struct BitwiseOr<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => BitwiseAnd<U, T>.Construct(seed),
                2 => BitwiseAnd<BitwiseOr<U, T>, T>.Construct(seed),
                3 => BitwiseAnd<U, BitwiseOr<U, T>>.Construct(seed),
                _ => BitwiseAnd<BitwiseOr<U, T>, BitwiseOr<T, U>>.Construct(seed)
            };
        }
    }

    private struct BitwiseAnd<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => Equality<U, T>.Construct(seed),
                2 => Equality<BitwiseAnd<U, T>, T>.Construct(seed),
                3 => Equality<U, BitwiseAnd<U, T>>.Construct(seed),
                _ => Equality<BitwiseAnd<U, T>, BitwiseAnd<T, U>>.Construct(seed)
            };
        }
    }

    private struct Equality<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => Comparison<U, T>.Construct(seed),
                2 => Comparison<Equality<U, T>, T>.Construct(seed),
                3 => Comparison<U, Equality<U, T>>.Construct(seed),
                _ => Comparison<Equality<U, T>, Equality<T, U>>.Construct(seed)
            };
        }
    }

    private struct Comparison<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => BitwiseShift<U, T>.Construct(seed),
                2 => BitwiseShift<Comparison<U, T>, T>.Construct(seed),
                3 => BitwiseShift<U, Comparison<U, T>>.Construct(seed),
                _ => BitwiseShift<Comparison<U, T>, Comparison<T, U>>.Construct(seed)
            };
        }
    }

    private struct BitwiseShift<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => Addition<U, T>.Construct(seed),
                2 => Addition<BitwiseShift<U, T>, T>.Construct(seed),
                3 => Addition<U, BitwiseShift<U, T>>.Construct(seed),
                _ => Addition<BitwiseShift<U, T>, BitwiseShift<T, U>>.Construct(seed)
            };
        }
    }

    private struct Addition<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => Multiplication<U, T>.Construct(seed),
                2 => Multiplication<Addition<U, T>, T>.Construct(seed),
                3 => Multiplication<U, Addition<U, T>>.Construct(seed),
                _ => Multiplication<Addition<U, T>, Addition<T, U>>.Construct(seed)
            };
        }
    }

    private struct Multiplication<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => Nested<U, T>.Construct(seed),
                2 => Nested<Multiplication<U, T>, T>.Construct(seed),
                3 => Nested<U, Multiplication<U, T>>.Construct(seed),
                _ => Nested<Multiplication<U, T>, Multiplication<T, U>>.Construct(seed)
            };
        }
    }
    
    private struct Nested<T, U>
    {
        public static int Construct(int seed)
        {
            return seed switch
            {
                1 => Expression<U, T>.Construct(seed),
                2 => Expression<Nested<U, T>, T>.Construct(seed),
                3 => Expression<U, Nested<U, T>>.Construct(seed),
                _ => Expression<Nested<U, T>, Nested<T, U>>.Construct(seed)
            };
        }
    }
    
    [Fact]
    public static void BreadthTest()
    {
        Assert.Equal(100, Expression<long, int>.Construct(2) * Expression<float, double>.Construct(2));
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReturnTwoAndDontTellJIT() => 2;
}
