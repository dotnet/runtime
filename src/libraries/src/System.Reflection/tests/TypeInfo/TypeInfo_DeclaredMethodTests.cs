// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414
#pragma warning disable 0067
#pragma warning disable 3026

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredMethodTests
    {
        // Verify Declared Methods for a Base class
        [Fact]
        public static void TestBaseClassMethods()
        {
            VerifyMethods(typeof(TypeInfoMethodBaseClass), TypeInfoMethodBaseClass.PublicMethodNames);
        }

        // Verify Declared Methods for a Base class
        [Fact]
        public static void TestDerivedClassMethods()
        {
            VerifyMethods(typeof(TypeInfoMethodSubClass), TypeInfoMethodSubClass.PublicMethodNames);
        }

        //private helper methods
        private static void VerifyMethods(Type t, String[] expectedmethods)
        {
            List<MethodInfo> methods = getMethods(t);
            Assert.NotNull(methods);

            bool found = false;

            for (int i = 0; i < expectedmethods.Length; i++)
            {
                found = false;
                foreach (MethodInfo mi in methods)
                {
                    if (expectedmethods[i].Equals(mi.Name))
                    {
                        found = true;
                        break;
                    }
                }
                Assert.True(found, "Failed!!. Did not find expected Method: " + expectedmethods[i]);
            }
        }

        //Gets MethodInfo object from a Type
        public static List<MethodInfo> getMethods(Type t)
        {
            //Fix to initialize Reflection
            String name = typeof(Object).Name;

            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<MethodInfo> allmethods = ti.DeclaredMethods.GetEnumerator();
            List<MethodInfo> list = new List<MethodInfo>();

            while (allmethods.MoveNext())
            {
                list.Add(allmethods.Current);
            }
            return list;
        }
    }

    //Metadata for Reflection
    public class TypeInfoMethodBaseClass
    {
        public static String[] PublicMethodNames = new String[] {   "PubBaseMeth1",
                                                                    "PubBaseMeth1",
                                                                    "PubMeth1",
                                                                    "PubMeth2",
                                                                    "PubMeth3",
                                                                    "PubMeth2ToOverride"};

        public void PubBaseMeth1() { }
        public void PubBaseMeth1(String str) { }
        public void PubMeth1() { }
        public virtual void PubMeth2() { }
        public virtual void PubMeth2ToOverride() { }
        public static void PubMeth3() { }
    }

    public class TypeInfoMethodSubClass : TypeInfoMethodBaseClass
    {
        public new static String[] PublicMethodNames = new String[]{ "PubMeth1",
                                                                 "PubMeth2",
                                                                 "PubMeth3",
                                                                 "PubMeth2ToOverride"};
        public new void PubMeth1() { }
        public new virtual void PubMeth2() { }
        public override void PubMeth2ToOverride() { }
        public new static void PubMeth3() { }
    }
}
