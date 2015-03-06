// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreXml.Test.XLinq
{
    public class DNException : Exception
    {
        public DNException() : base() { }

        public DNException(string s) : base(s) { }

        static internal Exception DocumentNavigatorNotOnLastNode()
        {
            return new DNException("DocumentNavigatorNotOnLastNode");
        }

        static internal Exception DocumentNavigatorNotOnFirstNode()
        {
            return new DNException("DocumentNavigatorNotOnFirstNode");
        }
    }
}
