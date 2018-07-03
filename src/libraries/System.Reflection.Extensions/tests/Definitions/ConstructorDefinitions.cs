// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 3026

namespace System.Reflection.Tests
{
    public static class ConstructorTestStaticClass
    {
        public static int Members = 3;
        public static int MembersEverything = 9;
        static ConstructorTestStaticClass() { }
    }

    public class ConstructorTestClassWithMultipleConstructors
    {
        public static int Members = 9;
        public static int MembersEverything = 15;

        static ConstructorTestClassWithMultipleConstructors() { }

        private ConstructorTestClassWithMultipleConstructors(string s) { }

        protected ConstructorTestClassWithMultipleConstructors(int i) { }

        public ConstructorTestClassWithMultipleConstructors() { }
        public ConstructorTestClassWithMultipleConstructors(TimeSpan ts) { }
        public ConstructorTestClassWithMultipleConstructors(object o1, object o2) { }
        public ConstructorTestClassWithMultipleConstructors(object obj0, int i4) { }
    }

    public class ConstructorTestBaseClass
    {
        public static int Members = 5;
        public static int MembersEverything = 11;

        static ConstructorTestBaseClass() { }
        public ConstructorTestBaseClass() { }
        public ConstructorTestBaseClass(short i2) { }
    }

    public class ConstructorTestSubClass : ConstructorTestBaseClass
    {
        public new static int Members = 5; //.cctor is added
        public new static int MembersEverything = 11;

        public ConstructorTestSubClass(string s) { }
        public ConstructorTestSubClass(short i2) { }
    }
}
