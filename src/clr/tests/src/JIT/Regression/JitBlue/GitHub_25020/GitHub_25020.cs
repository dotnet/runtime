// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GitHub_25020
{
    class Program
    {    
        static int Main(string[] args)
        {
            DynamicMethod dm = new DynamicMethod("MyMethod", typeof(string), new Type[] { typeof(string), typeof(string) });
            
            ILGenerator generator = dm.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Tailcall);
            generator.EmitCall(OpCodes.Call, typeof(String).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }), null);
            generator.Emit(OpCodes.Ret);

            string a = "1234";
            string b = "abcd";
            
            Console.WriteLine(dm.Invoke(null, BindingFlags.Default, null, new object[] {a, b}, null));

            return 100;
        }
    }
}