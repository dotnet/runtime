// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GenericFunctions
{
    struct UserDefinedStruct
    {
        int n;
        public UserDefinedStruct(int num)
        {
            n = num;
        }
    }

    class UserDefinedClass
    {
        int n;
        public UserDefinedClass(int num)
        {
            n = num;
        }
    }

    class GenericFunctions
    {
        static T GenericFunction<S, T>(T t, S s)
        {
            return t;
        }

        static void Main(string[] args)
        {
            string str = "hello";
            UserDefinedStruct userDefinedStruct = new UserDefinedStruct(2);
            GenericFunction(userDefinedStruct, str);

            int integer = 1;
            UserDefinedClass userDefinedClass = new UserDefinedClass(2);
            GenericFunction(integer, userDefinedClass);
        }
    }
}
