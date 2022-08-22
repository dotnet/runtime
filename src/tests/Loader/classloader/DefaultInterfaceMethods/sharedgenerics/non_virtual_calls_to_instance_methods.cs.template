// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Sample
{
    public sealed class C1 : I1<string>, I2
    {
    }

    class Program
    {
        static int Main(string[] args)
        {
            CallOpcodeNonGenericInterface();
            CallOpcodeNonGenericInterfaceGenericMethod();
            CallOpcodeGenericInterface();
            CallOpcodeGenericInterfaceGenericMethod();
            CallVirtOpcodeNonGenericInterface();
            CallVirtOpcodeNonGenericInterfaceGenericMethod();
            CallVirtOpcodeGenericInterface();
            CallVirtOpcodeGenericInterfaceGenericMethod();

            return 100;
        }

        private static void CallOpcodeNonGenericInterface()
        {
            Console.WriteLine("Testing call opcode for calling DIM on non-generic interface non-generic method");
            if (((I2)new C1()).GetItemTypeNonGeneric(typeof(string)) != typeof(string))
                throw new Exception("CallOpcodeGenericInterface failed");
        }

        private static void CallOpcodeNonGenericInterfaceGenericMethod()
        {
            Console.WriteLine("Testing call opcode for calling DIM on non-generic interface non-generic method");
            if (((I2)new C1()).GetItemTypeGeneric<object>() != typeof(object))
                throw new Exception("CallOpcodeGenericInterface failed");
        }

        private static void CallOpcodeGenericInterface()
        {
            Console.WriteLine("Testing call opcode for calling DIM on generic interface");
            if (((I1<string>)new C1()).GetItemType() != typeof(string))
                throw new Exception("CallOpcodeGenericInterface failed");
        }

        private static void CallOpcodeGenericInterfaceGenericMethod()
        {
            Console.WriteLine("Testing call opcode for calling generic method on DIM on generic interface");
            if (((I1<string>)new C1()).GetItemTypeMethod<object>() != typeof(object))
                throw new Exception("CallOpcodeGenericInterface failed");
        }

        private static void CallVirtOpcodeNonGenericInterface()
        {
            Console.WriteLine("Testing callvirt opcode for calling DIM on non-generic interface non-generic method");
            I2 c1 = new C1();
            if (c1.GetItemTypeNonGeneric(typeof(string)) != typeof(string))
                throw new Exception("CallOpcodeGenericInterface failed");
        }

        private static void CallVirtOpcodeNonGenericInterfaceGenericMethod()
        {
            Console.WriteLine("Testing callvirt opcode for calling DIM on non-generic interface non-generic method");
            I2 c1 = new C1();
            if (c1.GetItemTypeGeneric<object>() != typeof(object))
                throw new Exception("CallOpcodeGenericInterface failed");
        }

        private static void CallVirtOpcodeGenericInterface()
        {
            Console.WriteLine("Testing callvirt opcode for calling DIM on generic interface");
            I1<string> c1 = new C1();
            if (c1.GetItemType() != typeof(string))
                throw new Exception("CallVirtOpcodeGenericInterface failed");
        }

        private static void CallVirtOpcodeGenericInterfaceGenericMethod()
        {
            Console.WriteLine("Testing callvirt opcode for calling generic method on DIM on generic interface");
            I1<string> c1 = new C1();
            if (c1.GetItemTypeMethod<object>() != typeof(object))
                throw new Exception("CallVirtOpcodeGenericInterfaceGenericMethod failed");
        }
    }
}

public interface I1<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    sealed Type GetItemType() => typeof(T);
    [MethodImpl(MethodImplOptions.NoInlining)]
    sealed Type GetItemTypeMethod<U>() => typeof(U);
}

public interface I2
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    sealed Type GetItemTypeNonGeneric(Type t) => t;
    [MethodImpl(MethodImplOptions.NoInlining)]
    sealed Type GetItemTypeGeneric<U>() => typeof(U);
}
