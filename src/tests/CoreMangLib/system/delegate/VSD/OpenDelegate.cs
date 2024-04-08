// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

public class Program
{
    public class ClassA
    {
        public virtual int PublicInstanceMethod() { return 17; }
    }

    public delegate int Delegate_TC_Int(ClassA tc);
    public static MethodInfo GetMethod(Type t, string method)
    {
        TypeInfo typeInfo = t.GetTypeInfo();
        IEnumerator<MethodInfo> enumerator = typeInfo.DeclaredMethods.GetEnumerator();
        MethodInfo result = null;
        while (enumerator.MoveNext())
        {
            bool flag = enumerator.Current.Name.Equals(method);
            if (flag)
            {
                result = enumerator.Current;
                break;
            }
        }
        return result;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        Type typeTestClass = typeof(ClassA);
        ClassA TestClass = (ClassA)Activator.CreateInstance(typeTestClass);
        MethodInfo miPublicInstanceMethod = GetMethod(typeTestClass, "PublicInstanceMethod");
        Delegate dlgt = miPublicInstanceMethod.CreateDelegate(typeof(Delegate_TC_Int));
        Object retValue = ((Delegate_TC_Int)dlgt).DynamicInvoke(new Object[] { TestClass });

        if(retValue.Equals(TestClass.PublicInstanceMethod()))
        {
            return 100;
        }


        return -1;

    }

}
