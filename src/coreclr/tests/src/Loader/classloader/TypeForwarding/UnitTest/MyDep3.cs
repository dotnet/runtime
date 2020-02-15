// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace MyDep3
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
