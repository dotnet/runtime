// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

class Program
{
    static int Main()
    {
        var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Mine"), AssemblyBuilderAccess.Run);
        var modb = ab.DefineDynamicModule("Mine.dll");

        //
        // Set up the IFoo interface
        //

        var ifooType = modb.DefineType("IFoo", TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Public);

        // Define a simple instance method on the interface
        {
            var mb = ifooType.DefineMethod("InstanceMethod", MethodAttributes.Public, typeof(int), Type.EmptyTypes);
            var ilg = mb.GetILGenerator();
            ilg.Emit(OpCodes.Ldc_I4_1);
            ilg.Emit(OpCodes.Ret);
        }

        // Define a default interface method
        {
            var mb = ifooType.DefineMethod("DefaultMethod", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot, typeof(int), Type.EmptyTypes);
            var ilg = mb.GetILGenerator();
            ilg.Emit(OpCodes.Ldc_I4_2);
            ilg.Emit(OpCodes.Ret);
        }

        // Define a regular interface method
        {
            var mb = ifooType.DefineMethod("InterfaceMethod", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Abstract, typeof(int), Type.EmptyTypes);
        }

        ifooType.CreateTypeInfo();

        //
        // Set up the IBar interface
        //

        var ibarType = modb.DefineType("IBar", TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Public, null, new Type[] { ifooType });

        // Override the regular interface method on IFoo with a default implementation
        {
            var mb = ibarType.DefineMethod("InterfaceMethodImpl", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final, typeof(int), Type.EmptyTypes);
            var ilg = mb.GetILGenerator();
            ilg.Emit(OpCodes.Ldc_I4_4);
            ilg.Emit(OpCodes.Ret);

            ibarType.DefineMethodOverride(mb, ifooType.GetMethod("InterfaceMethod"));
        }

        ibarType.CreateTypeInfo();

        //
        // Make a simple Foo class that implements IBar
        //

        var fooType = modb.DefineType("Foo", TypeAttributes.Class | TypeAttributes.Public, typeof(object), new Type[] { ibarType });

        fooType.CreateTypeInfo();

        //
        // Test what we created
        //

        object o = Activator.CreateInstance(fooType);

        int result = 0;

        result |= (int)ifooType.GetMethod("InstanceMethod").Invoke(o, null);
        result |= (int)ifooType.GetMethod("DefaultMethod").Invoke(o, null);
        result |= (int)ifooType.GetMethod("InterfaceMethod").Invoke(o, null);

        return result == 0x07 ? 100 : result;
    }
}
