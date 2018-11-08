// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0414
#pragma warning disable 0067
#pragma warning disable 3026

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredMemberTests
    {
        // Verify Declared fields for a Base class
        [Fact]
        public static void TestBaseClassMembers()
        {
            VerifyMembers(typeof(TypeInfoMemberBaseClass).Project(), TypeInfoMemberBaseClass.PublicFieldNames);
        }

        // Verify Declared fields for a Derived class
        [Fact]
        public static void TestDerivedClassMembers()
        {
            VerifyMembers(typeof(TypeInfoMemberSubClass).Project(), TypeInfoMemberSubClass.PublicFieldNames);
        }

        //private helper methods
        private static void VerifyMembers(Type t, string[] expectedfields)
        {
            List<MemberInfo> members = getMembers(t);
            Assert.NotNull(members);

            bool found = false;

            for (int i = 0; i < expectedfields.Length; i++)
            {
                found = false;
                foreach (MemberInfo mi in members)
                {
                    if (expectedfields[i].Equals(mi.Name))
                    {
                        found = true;
                        break;
                    }
                }
                Assert.True(found, "Failed!!. Did not find expected Member: " + expectedfields[i]);
            }
        }

        //Gets MemberInfo object from a Type
        public static List<MemberInfo> getMembers(Type t)
        {
            //Fix to initialize Reflection
            string name = typeof(object).Project().Name;

            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<MemberInfo> allmembers = ti.DeclaredMembers.GetEnumerator();
            List<MemberInfo> list = new List<MemberInfo>();

            while (allmembers.MoveNext())
            {
                list.Add(allmembers.Current);
            }
            return list;
        }
    }

    //Metadata for Reflection
    public class TypeInfoMemberBaseClass
    {
        public static int Members = 9;

        public static string[] PublicFieldNames = new string[] { "SubPubfld1", "Pubfld1", "Pubfld2", "Pubfld3", "Pubfld4", "Pubfld5", "Pubfld6",
                                                                 "Members",  "PublicFieldNames"};

        public string SubPubfld1 = "";
        public string Pubfld1 = "";
        public readonly string Pubfld2 = "";
        public volatile string Pubfld3 = "";
        public static string Pubfld4 = "";
        public static readonly string Pubfld5 = "";
        public static volatile string Pubfld6 = "";
    }

    public class TypeInfoMemberSubClass : TypeInfoMemberBaseClass
    {
        public new static int Members = 10;

        public static string[] InheritedFieldNames = new string[] { "SubPubfld1" };

        public static string[] NewFieldNames = new string[] { "Pubfld1", "Pubfld2", "Pubfld3" };

        public new static string[] PublicFieldNames = new string[] { "Pubfld1", "Pubfld2", "Pubfld3", "Pubfld4", "Pubfld5", "Pubfld6",
                                                                 "Members", "InheritedFieldNames", "NewFieldNames", "PublicFieldNames"};
        public new string Pubfld1 = "";
        public new readonly string Pubfld2 = "";
        public new volatile string Pubfld3 = "";
        public new static string Pubfld4 = "";
        public new static readonly string Pubfld5 = "";
        public new static volatile string Pubfld6 = "";
    }
}
