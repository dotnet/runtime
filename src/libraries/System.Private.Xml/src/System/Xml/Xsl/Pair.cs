// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl
{
    internal readonly struct Int32Pair : IEquatable<Int32Pair>
    {
        public Int32Pair(int left, int right)
        {
            Left = left;
            Right = right;
        }

        public int Left { get; }
        public int Right { get; }

        public override bool Equals([NotNullWhen(true)] object? other) =>
            other is Int32Pair o && Equals(o);

        public bool Equals(Int32Pair other) => Left == other.Left && Right == other.Right;

        public override int GetHashCode() => Left.GetHashCode() ^ Right.GetHashCode();
    }

    internal readonly struct StringPair
    {
        public StringPair(string left, string right)
        {
            Left = left;
            Right = right;
        }

        public string Left { get; }
        public string Right { get; }
    }
}
