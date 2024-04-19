// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

public class Program
{
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        var baseClass = new BaseClass();
        var derivedClass = new DerivedClass();
        var superDerivedClass = new SuperDerivedClass();

        //
        // C# will generate a call to the unmodified version of the method so this is easy to check statically
        //

        if (baseClass.Override(0) != 3)
            return 1;

        if (derivedClass.Override(0) != 103)
            return 2;

        if (superDerivedClass.Override(0) != 203)
            return 3;

        //
        // Reflection-locate rest of the `Override` method overloads
        //

        MethodInfo paramModoptFoo = null;
        MethodInfo paramModoptBar = null;
        MethodInfo paramModreqFoo = null;
        MethodInfo paramUnmodified = null;
        MethodInfo returnModoptFoo = null;
        MethodInfo arrayModopt1 = null;
        MethodInfo arrayModopt2 = null;
        foreach (var method in typeof(BaseClass).GetTypeInfo().DeclaredMethods)
        {
            ParameterInfo param = method.GetParameters()[0];
            Type[] paramRequiredModifiers = param.GetRequiredCustomModifiers();
            Type[] paramOptionalModifiers = param.GetOptionalCustomModifiers();

            ParameterInfo retParam = method.ReturnParameter;
            Type[] retParamOptionalModifiers = retParam.GetOptionalCustomModifiers();

            if (param.ParameterType != typeof(int) && param.ParameterType != typeof(int[]))
                throw new Exception();

            if (paramRequiredModifiers.Length > 0)
            {
                if (paramRequiredModifiers.Length > 1 || paramRequiredModifiers[0] != typeof(FooModifier))
                    throw new Exception();
                else
                    paramModreqFoo = method;
            }
            else if (paramOptionalModifiers.Length > 0)
            {
                if (paramOptionalModifiers.Length > 1)
                    throw new Exception();
                else if (paramOptionalModifiers[0] == typeof(FooModifier))
                    paramModoptFoo = method;
                else if (paramOptionalModifiers[0] == typeof(BarModifier))
                    paramModoptBar = method;
                else
                    throw new Exception();
            }
            else if (retParamOptionalModifiers.Length > 0)
            {
                if (retParamOptionalModifiers.Length > 1 || retParamOptionalModifiers[0] != typeof(FooModifier))
                    throw new Exception();
                else
                    returnModoptFoo = method;
            }
            else
            {
                if (param.ParameterType == typeof(int))
                    paramUnmodified = method;
                else if (param.ParameterType == typeof(int[]))
                {
                    // Reflection can't distinguish between the two overloads

                    if (arrayModopt1 == null)
                        arrayModopt1 = method;
                    else if (arrayModopt2 == null)
                        arrayModopt2 = method;
                    else
                        throw new Exception();
                }
                else
                    throw new Exception();
            }
        }

        if ((int)paramModoptFoo.Invoke(baseClass, new object[] { 0 }) != 0)
            return 101;
        if ((int)paramModoptBar.Invoke(baseClass, new object[] { 0 }) != 1)
            return 102;
        if ((int)paramModreqFoo.Invoke(baseClass, new object[] { 0 }) != 2)
            return 103;
        if ((int)paramUnmodified.Invoke(baseClass, new object[] { 0 }) != 3)
            return 104;
        if ((int)returnModoptFoo.Invoke(baseClass, new object[] { 0 }) != 4)
            return 105;

        if ((int)paramModoptFoo.Invoke(derivedClass, new object[] { 0 }) != 100)
            return 201;
        if ((int)paramModoptBar.Invoke(derivedClass, new object[] { 0 }) != 101)
            return 202;
        if ((int)paramModreqFoo.Invoke(derivedClass, new object[] { 0 }) != 102)
            return 203;
        if ((int)paramUnmodified.Invoke(derivedClass, new object[] { 0 }) != 103)
            return 204;
        if ((int)returnModoptFoo.Invoke(derivedClass, new object[] { 0 }) != 104)
            return 205;

        if ((int)arrayModopt1.Invoke(baseClass, new object[] { null }) + 100 != (int)arrayModopt1.Invoke(derivedClass, new object[] { null }))
            return 301;

        if ((int)arrayModopt2.Invoke(baseClass, new object[] { null }) + 100 != (int)arrayModopt2.Invoke(derivedClass, new object[] { null }))
            return 302;

        //
        // Make sure modifiers are ignored for newobj/box
        //

        object tryAllocWithModifiedArrayResult = Factory.TryAllocWithModifiedArray();
        if (!(tryAllocWithModifiedArrayResult is GenericClass<int[]>))
            return 401;
        if (tryAllocWithModifiedArrayResult.GetType() != typeof(GenericClass<int[]>))
            return 402;

        object tryBoxWithModifiedPointerResult = Factory.TryBoxWithModifiedPointer();
        if (!(tryBoxWithModifiedPointerResult is GenericStruct<int*[]>))
            return 501;
        if (tryBoxWithModifiedPointerResult.GetType() != typeof(GenericStruct<int*[]>))
            return 502;

        return 100;
    }

    class SuperDerivedClass : DerivedClass
    {
        public override int Override(int A_0)
        {
            Console.WriteLine("In int32 SuperDerivedClass::Override(int32)");
            return 203;
        }
    }
}
