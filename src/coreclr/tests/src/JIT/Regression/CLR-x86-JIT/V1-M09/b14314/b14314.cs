// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    internal struct ValueCls1
    {
        //internal ValueCls1() {}

    }


    internal class Class1
    {
        public ValueCls1[] valCls1Array = new ValueCls1[1];

        public virtual void Method()
        {
            valCls1Array[0] = new ValueCls1();
        }

    }

    internal class Finalize
    {
        public static int Main(String[] args)
        {
            Class1 Cls = new Class1();
            Cls.Method();
            return 100;
        }
    }
}
