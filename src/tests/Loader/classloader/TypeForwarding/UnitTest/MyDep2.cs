// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

[assembly:TypeForwardedToAttribute(typeof(MyDep3.Enclosing1))]
[assembly:TypeForwardedToAttribute(typeof(MyDep3.Enclosing2))]

namespace MyDep2
{
    public class Enclosing1
    {
        public class Nested1
        {
            public class SubNested1 { }
            public class SubNested2 { }
        }

        public class Nested2
        {
            public class SubNested1 { }
            public class SubNested2 { }
        }
    }

    public class Enclosing2
    {
        public class Nested1
        {
            public class SubNested1 { }
            public class SubNested2 { }
        }

        public class Nested2
        {
            public class SubNested1 { }
            public class SubNested2 { }
        }
    }
}
