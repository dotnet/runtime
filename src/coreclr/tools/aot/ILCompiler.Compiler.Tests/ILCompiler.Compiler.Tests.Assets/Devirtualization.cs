// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Devirtualization
{
    class DevirtualizeSimple
    {
        abstract class Base
        {
            public abstract void Virtual();
        }

        class Derived : Base
        {
            public override void Virtual()
            {
                new Derived();
            }
        }

        static void Run()
        {
            Base p = new Derived();
            p.Virtual();
        }
    }

    class DevirtualizeAbstract
    {
        abstract class Abstract { }

        static void Run()
        {
            typeof(Abstract).GetHashCode();
        }
    }
}
