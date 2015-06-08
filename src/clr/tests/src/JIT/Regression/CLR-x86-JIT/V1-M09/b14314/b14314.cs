// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
