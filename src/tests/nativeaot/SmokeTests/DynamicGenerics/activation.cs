// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using CoreFXTestLibrary;
using TypeOfRepo;

public class Foo
{

}

public class My {

    [TestMethod]
    public static void TestActivatorCreateInstance() 
    {
        var my = new My();
        var foo = new Foo();
        
        //
        // Validate that a generic instantiation of a type not present in the compiled
        // application can be created
        //
        {
            Console.WriteLine("Calling List<My>.ctor");
            Type listOfMyType = TypeOf.List.MakeGenericType(TypeOf.My);
            var listOfMy = Activator.CreateInstance(listOfMyType);
            Console.WriteLine(listOfMy.ToString());
            
            Console.WriteLine("Adding item to List<My>");
            var addMethodInfo = listOfMyType.GetTypeInfo().GetDeclaredMethod("Add");
            addMethodInfo.Invoke(listOfMy, new object[] {my});

            Console.WriteLine("Calling List<My>.get_Count");
            var getCountMethodInfo = listOfMyType.GetTypeInfo().GetDeclaredMethod("get_Count");
            int result = (int)getCountMethodInfo.Invoke(listOfMy, null);
            
            Assert.AreEqual(result, 1);

            Console.WriteLine("Calling GC.Collect");
            GC.Collect();
            GC.KeepAlive(listOfMy);
        }
        //
        // Validate that a generic instantiation for which metadata exists through rd.xml pinning
        // can still be constructed and its methods invoked
        //
        {
            Console.WriteLine("Calling List<Foo>.ctor");
            Type listOfFooType = TypeOf.List.MakeGenericType(TypeOf.Foo);
            var listOfFoo = Activator.CreateInstance(listOfFooType);
            Console.WriteLine(listOfFoo.ToString());
            
            Console.WriteLine("Adding item to List<Foo>");
            var addMethodInfo = listOfFooType.GetTypeInfo().GetDeclaredMethod("Add");
            addMethodInfo.Invoke(listOfFoo, new object[] {foo});

            Console.WriteLine("Calling List<My>.get_Count");
            var getCountMethodInfo = listOfFooType.GetTypeInfo().GetDeclaredMethod("get_Count");
            int result = (int)getCountMethodInfo.Invoke(listOfFoo, null);
            
            Assert.AreEqual(result, 1);
        }
        //
        // Validate that a generic instantiation over a struct of a type not present in the compiled
        // application can be created
        //
        {
            Console.WriteLine("Calling Dictionary<My,My>.ctor");
            Type dictOfMyType = TypeOf.Dictionary.MakeGenericType(TypeOf.My, TypeOf.My);
            var dictOfMy = Activator.CreateInstance(dictOfMyType);
            Console.WriteLine(dictOfMy.ToString());
            
            Console.WriteLine("Adding item to Dictionary<My,My>");
            var addMethodInfo = dictOfMyType.GetTypeInfo().GetDeclaredMethod("Add");
            addMethodInfo.Invoke(dictOfMy, new object[] {my, my});

            Console.WriteLine("Calling Dictionary<My,My>.get_Count");
            var getCountMethodInfo = dictOfMyType.GetTypeInfo().GetDeclaredMethod("get_Count");
            int result = (int)getCountMethodInfo.Invoke(dictOfMy, null);
            
            Assert.AreEqual(result, 1);

            Console.WriteLine("Calling GC.Collect");
            GC.Collect();
            GC.KeepAlive(dictOfMy);
        }
        //
        // Validate that a non-generic type can still be constructed and its methods invoked
        //
        Console.WriteLine("Calling My.ctor");
        var myObject = Activator.CreateInstance(TypeOf.My);

        Console.WriteLine("Calling My.TestMethod1() instance method");
        var testMethod1Info = TypeOf.My.GetTypeInfo().GetDeclaredMethod("TestMethod1");
        string retString = (string)testMethod1Info.Invoke(myObject, null);

        Assert.AreEqual(retString, "I have been called");
    }

    public string TestMethod1()
    {
        return "I have been called";
    }


    class ToStringIsInteresting1
    {
        string memberVar;
        public ToStringIsInteresting1()
        {
            memberVar = "ToStringIsInteresting1";
        }
        public override string ToString()
        {
            return memberVar;
        }
    }
    class ToStringIsInteresting2
    {
        string memberVar;
        public ToStringIsInteresting2()
        {
            memberVar = "ToStringIsInteresting2";
        }
        public override string ToString()
        {
            return memberVar;
        }
    }
    struct StructToString<T>
    {
        string memberVar;
        public StructToString()
        {
            memberVar = typeof(T).Name;
        }
        public override string ToString()
        {
            return memberVar;
        }
    }
    class SomeUnrealtedType<T>
    {
        string memberVar;
        public SomeUnrealtedType()
        {
            memberVar = String.Format("SomeUnrealtedType<{0}>", typeof(T));
        }
        public override string ToString()
        {
            return memberVar;
        }
    }
    
    class AllocViaGVMSecondLevelBase
    {
        public virtual T ActuallyAlloc<T>() where T : new()
        {
            throw new Exception();
        }
    }

    class AllocViaGVMBase
    {
        public virtual T Alloc<T>() where T : new()
        {
            throw new Exception();
        }
    }

    class AllocViaSecondLevelDerived : AllocViaGVMSecondLevelBase
    {
        public override T ActuallyAlloc<T>()
        {
            // Verify Activator.CreateInstance codepath works for types other than just T.
            var a = Activator.CreateInstance<SomeUnrealtedType<T>>();
            return new T();
        }
    }

    class AllocViaGVMDerived : AllocViaGVMBase
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static AllocViaGVMBase Alloc()
        {
            return new AllocViaGVMDerived();
        }
        public override T Alloc<T>()
        {
            return new AllocViaSecondLevelDerived().ActuallyAlloc<T>();
        }
    }

    [TestMethod]
    public static void TestDefaultCtorInLazyGenerics()
    {
        // Test default ctor in lazy generics. 
        // 
        // Use odd construction with GVM's to get to the default constructor path as what I'm testing
        // is the template type loader in use for using the default constructor constraint
        // without reflection information for the types in use. In particular lazy generics will
        // put us into this path.
        AllocViaGVMBase typeWithGVM = AllocViaGVMDerived.Alloc();
        Assert.AreEqual("ToStringIsInteresting1", typeWithGVM.Alloc<ToStringIsInteresting1>().ToString());
        Assert.AreEqual("ToStringIsInteresting2", typeWithGVM.Alloc<ToStringIsInteresting2>().ToString());
        Assert.AreEqual("ToStringIsInteresting1", typeWithGVM.Alloc<StructToString<ToStringIsInteresting1>>().ToString());
        Assert.AreEqual("ToStringIsInteresting2", typeWithGVM.Alloc<StructToString<ToStringIsInteresting2>>().ToString());
    }
}
