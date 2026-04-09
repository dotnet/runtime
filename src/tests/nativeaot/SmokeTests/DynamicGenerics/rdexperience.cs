// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CoreFXTestLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TypeOfRepo;

public class RdExperienceTests
{
    public class Foo<T>
    {
        public string Frob(T t) { return "Frob: " + t.GetType().ToString(); }
        public string Gizmo(T t) { return "Gizmo: " + t.GetType().ToString(); }
    }

    public class Bar { }

    [TestMethod]
    public static void TestRdExperience()
    {
        Type t = TypeOf.RDE_Foo.MakeGenericType(typeof(Bar));
        TypeInfo ti = t.GetTypeInfo();
        object o = Activator.CreateInstance(t);

        {
            MethodInfo mi = ti.GetDeclaredMethod("Frob");
            string result = (string)mi.Invoke(o, new object[] { new Bar() });
            Assert.AreEqual("Frob: RdExperienceTests+Bar", result);
        }

        {
            MethodInfo mi = ti.GetDeclaredMethod("Gizmo");
            string result = (string)mi.Invoke(o, new object[] { new Bar() });
            Assert.AreEqual("Gizmo: RdExperienceTests+Bar", result);
        }
    }
}
