// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

public class OpenDelegate
{
    public class ClassA
    {
        public virtual int PublicInstanceMethod() { return 17; }
    }

    public class ClassB
    {
        public virtual int PublicInstanceMethod(int a, int b, int c, int d) { return a + b + c + d; }
    }

    public delegate int Delegate_TC_Int(ClassA tc);
    public static MethodInfo GetMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t, string method)
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
    [SkipOnMono("https://github.com/dotnet/runtime/issues/137225")]
    public static void NullReceiverAfterWarmingUnrelatedVirtualDelegates()
    {
        Func<ClassA, int> first = typeof(ClassA).GetMethod(nameof(ClassA.PublicInstanceMethod))
            .CreateDelegate<Func<ClassA, int>>();
        Func<ClassB, int, int, int, int, int> second = typeof(ClassB).GetMethod(nameof(ClassB.PublicInstanceMethod))
            .CreateDelegate<Func<ClassB, int, int, int, int, int>>();

        ClassA firstReceiver = new ClassA();
        ClassB secondReceiver = new ClassB();
        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(17, first(firstReceiver));
            Assert.Equal(10, second(secondReceiver, 1, 2, 3, 4));
        }

        Assert.Throws<NullReferenceException>(() => second(null, 1, 2, 3, 4));
        Assert.Throws<NullReferenceException>(() => first(null));
    }

    [OuterLoop]
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
