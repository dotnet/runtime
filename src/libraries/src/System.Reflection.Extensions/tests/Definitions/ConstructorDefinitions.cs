// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable 3026
using System;

namespace ConstructorDefinitions
{
    public static class StaticClass
    {
        public static int Members = 3;
        public static int MembersEverything = 9;
        static StaticClass() { }
    }

    public class ClassWithMultipleConstructors
    {
        public static int Members = 9;
        public static int MembersEverything = 15;

        static ClassWithMultipleConstructors() { }

        private ClassWithMultipleConstructors(String s) { }

        protected ClassWithMultipleConstructors(int i) { }

        public ClassWithMultipleConstructors() { }
        public ClassWithMultipleConstructors(TimeSpan ts) { }
        public ClassWithMultipleConstructors(Object o1, Object o2) { }
        public ClassWithMultipleConstructors(Object obj0, Int32 i4) { }
    }

    public class BaseClass
    {
        public static int Members = 5;
        public static int MembersEverything = 11;

        static BaseClass() { }
        public BaseClass() { }
        public BaseClass(Int16 i2) { }
    }

    public class SubClass : BaseClass
    {
        public new static int Members = 5; //.cctor is added
        public new static int MembersEverything = 11;

        public SubClass(String s) { }
        public SubClass(Int16 i2) { }
    }
}
