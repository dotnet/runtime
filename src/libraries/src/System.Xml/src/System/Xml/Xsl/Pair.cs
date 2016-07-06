// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Xml.Xsl
{
    internal struct Int32Pair
    {
        private int _left;
        private int _right;

        public Int32Pair(int left, int right)
        {
            _left = left;
            _right = right;
        }

        public int Left { get { return _left; } }
        public int Right { get { return _right; } }

        public override bool Equals(object other)
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
        private string _left;
        private string _right;

        public StringPair(string left, string right)
        {
            _left = left;
            _right = right;
        }

        public string Left { get { return _left; } }
        public string Right { get { return _right; } }
    }
}


