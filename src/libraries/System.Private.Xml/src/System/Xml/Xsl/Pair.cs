// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl
{
    internal struct Int32Pair
    {
        private readonly int _left;
        private readonly int _right;

        public Int32Pair(int left, int right)
        {
            _left = left;
            _right = right;
        }

        public int Left { get { return _left; } }
        public int Right { get { return _right; } }

        public override bool Equals([NotNullWhen(true)] object? other)
        {
            if (other is Int32Pair)
            {
                Int32Pair o = (Int32Pair)other;
                return _left == o._left && _right == o._right;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _left.GetHashCode() ^ _right.GetHashCode();
        }
    }

    internal struct StringPair
    {
        private readonly string _left;
        private readonly string _right;

        public StringPair(string left, string right)
        {
            _left = left;
            _right = right;
        }

        public string Left { get { return _left; } }
        public string Right { get { return _right; } }
    }
}
