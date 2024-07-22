// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using RuntimeLibrariesTest;
using TypeOfRepo;


public class InterfacesTests
{
    public interface IInterface<T> { }

    public class Gen<T> : IInterface<T> { }

    public class Recursive<T> : IInterface<Recursive<T>> { }

    public class DoublyRecursive<T> : Recursive<T> { }

    public class WithInterfaceOverArrayType<T> : IInterface<T[]> { }

    public interface IFrobber<T>
    {
        string Frob();
    }

    public class Frobber<T> : IFrobber<T>
    {
        public string Frob()
        {
            return typeof(T).FullName;
        }

        public static string FrobStatically()
        {
            return typeof(T).FullName;
        }
    }

    public struct FrobtasticFrobberStruct : IFrobber<string>, IFrobber<CommonType1>
    {
        string IFrobber<string>.Frob() { return "String Frob"; }
        string IFrobber<CommonType1>.Frob() { return "CommonType1 Frob"; }
    }

    public struct AnotherFrobtasticFrobberStruct : IFrobber<object>, IFrobber<CommonType1>
    {
        string IFrobber<object>.Frob() { return "Object Frob"; }
        string IFrobber<CommonType1>.Frob() { return "CommonType1 Frob"; }
    }

    public class UseFrobberBase
    {
        public virtual string UseFrob()
        {
            return "";
        }
    }

    public class UseFrobber<T,U> : UseFrobberBase  where T:IFrobber<U>
    {
        public override string UseFrob()
        {
            return (default(T)).Frob();
        }
    }

    public interface IGnr<T>
    { 
        string Func(); 
    }

    public class GenBaseTypeIGnr<T> : IGnr<T>
    {
        public /*final newslot*/ virtual string Func() { return "G"; }
    }

    public class DerivedTypeIGnr<T,U> : GenBaseTypeIGnr<T>, IGnr<U>
    {
    }

    public class DerivedTypeIGnrExplicitBasetypeInstantiation<T,U> : GenBaseTypeIGnr<string>, IGnr<U>
    {
    }    

    [TestMethod]
    public static void TestGenericCollapsingInInterfaceMap()
    {
#if !USC
        // BUG 634667: Replace <CommonType3,object> instantiation with <object,object> to repro the issue on PN.
        // In PureNative compilations, the template is canonical and we already get the generic collapsing. The bug is fixed for PureNative only, 
        // but PN is still broken because the interface map of the <object,object> instantiation will NOT work for a dynamically created type with
        // different type args in its instantiation.
        // The DynamicGenerics_USC test is also impacted by this bug, since it uses the <UniversalCanon,UniversalCanon> template, which has the same
        // behavior like the <object, object> template
        TestGenericCollapsingInInterfaceMapHelper<CommonType3, object>();

        MethodInfo mi = typeof(InterfacesTests).GetTypeInfo().GetDeclaredMethod("TestGenericCollapsingInInterfaceMapHelper").MakeGenericMethod(
            TypeOf.CommonType1, 
            TypeOf.CommonType2);
            
        mi.Invoke(null, null);
#endif
    }
    public static void TestGenericCollapsingInInterfaceMapHelper<T, U>()
    {
        IGnr<U> o = new DerivedTypeIGnr<T, U>();
        string s = o.Func();
        Assert.AreEqual("G", s);

        o = new DerivedTypeIGnrExplicitBasetypeInstantiation<T, U>();
        s = o.Func();
        Assert.AreEqual("G", s);
    }
    
    [TestMethod]
    public static void TestImplementedInterfaces()
    {
        {
            TypeInfo genOfMy = TypeOf.IT_Gen.MakeGenericType(TypeOf.CommonType1).GetTypeInfo();
            TypeInfo intf = genOfMy.ImplementedInterfaces.Single().GetTypeInfo();
            Assert.AreEqual(TypeOf.CommonType1, intf.GenericTypeArguments[0]);
        }

        {
            Type recursiveOfMy = TypeOf.IT_Recursive.MakeGenericType(TypeOf.CommonType1);
            TypeInfo intf = recursiveOfMy.GetTypeInfo().ImplementedInterfaces.Single().GetTypeInfo();
            Assert.AreEqual(recursiveOfMy, intf.GenericTypeArguments[0]);
            Assert.AreEqual(recursiveOfMy.TypeHandle, intf.GenericTypeArguments[0].TypeHandle);
        }

        {
            Type dbRecursiveOfMy = TypeOf.IT_DoublyRecursive.MakeGenericType(TypeOf.CommonType1);
            TypeInfo intf = dbRecursiveOfMy.GetTypeInfo().ImplementedInterfaces.Single().GetTypeInfo();
            Type recursiveOfMy = TypeOf.IT_Recursive.MakeGenericType(TypeOf.CommonType1);
            Assert.AreEqual(recursiveOfMy, intf.GenericTypeArguments[0]);
            Assert.AreEqual(recursiveOfMy.TypeHandle, intf.GenericTypeArguments[0].TypeHandle);
        }

        {
            TypeInfo genOfMy = TypeOf.IT_WithInterfaceOverArrayType.MakeGenericType(TypeOf.CommonType1).GetTypeInfo();
            TypeInfo intf = genOfMy.ImplementedInterfaces.Single().GetTypeInfo();
            Assert.IsTrue(intf.GenericTypeArguments[0].IsArray);
            Assert.AreEqual(TypeOf.CommonType1, intf.GenericTypeArguments[0].GetElementType());
        }
    }

    [TestMethod]
    public static void TestBaseType()
    {
        {
            Type dbRecursiveOfMy = TypeOf.IT_DoublyRecursive.MakeGenericType(TypeOf.CommonType1);
            Type baseType = dbRecursiveOfMy.GetTypeInfo().BaseType;
            Type recursiveOfMy = TypeOf.IT_Recursive.MakeGenericType(TypeOf.CommonType1);
            Assert.AreEqual(recursiveOfMy, baseType);
            Assert.AreEqual(recursiveOfMy.TypeHandle, baseType.TypeHandle);
        }
    }

    [TestMethod]
    public static void TestInterfaceInvoke()
    {
        {
            Type frobberOfMy = TypeOf.IT_Frobber.MakeGenericType(TypeOf.CommonType1);
            TypeInfo iFrobberOfMy = TypeOf.IT_IFrobber.MakeGenericType(TypeOf.CommonType1).GetTypeInfo();
            object o = Activator.CreateInstance(frobberOfMy);
            string result = (string)iFrobberOfMy.GetDeclaredMethod("Frob").Invoke(o, null);
            Assert.AreEqual("CommonType1", result);
        }

        {
            TypeInfo frobberOfMy = TypeOf.IT_Frobber.MakeGenericType(TypeOf.CommonType1).GetTypeInfo();
            string result = (string)frobberOfMy.GetDeclaredMethod("FrobStatically").Invoke(null, null);
            Assert.AreEqual("CommonType1", result);
        }
    }

    [TestMethod]
    public static void TestConstrainedCall()
    {
        {
            // Direct call case
            {
                Type useFrobberType = TypeOf.IT_UseFrobber.MakeGenericType(TypeOf.IT_FrobtasticFrobberStruct, TypeOf.CommonType1);
                UseFrobberBase useFrobber = (UseFrobberBase)Activator.CreateInstance(useFrobberType);

                Assert.AreEqual("CommonType1 Frob", useFrobber.UseFrob());
            }
            
#if UNIVERSAL_GENERICS
            // LoadVirtualFunction case (used by USG callers)
            {
                Type useFrobberType = TypeOf.IT_UseFrobber.MakeGenericType(TypeOf.IT_AnotherFrobtasticFrobberStruct, TypeOf.CommonType1);
                UseFrobberBase useFrobber = (UseFrobberBase)Activator.CreateInstance(useFrobberType);

                Assert.AreEqual("CommonType1 Frob", useFrobber.UseFrob());
            }
#endif
        }
    }
}
