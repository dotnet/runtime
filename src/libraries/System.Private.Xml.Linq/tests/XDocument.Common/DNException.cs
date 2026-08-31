// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace CoreXml.Test.XLinq
{
    public class DNException : Exception
    {
        public DNException() : base() { }

        public DNException(string s) : base(s) { }

        internal static Exception DocumentNavigatorNotOnLastNode()
        {
            return new DNException("DocumentNavigatorNotOnLastNode");
        }

        internal static Exception DocumentNavigatorNotOnFirstNode()
        {
            return new DNException("DocumentNavigatorNotOnFirstNode");
        }
    }
}
