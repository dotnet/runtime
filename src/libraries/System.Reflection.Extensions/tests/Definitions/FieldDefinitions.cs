// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 3026

namespace System.Reflection.Tests
{
    public class FieldTestBaseClass
    {
        public static int Members = 35;
        public static int MembersEverything = 41;

        public static string[] DeclaredFieldNames = new string[] { "SubPubfld1", "Pubfld1", "Pubfld2", "Pubfld3", "SubIntfld1", "Intfld1", "Intfld2", "Intfld3", "SubProfld1",
                                                                    "Profld1", "Profld2", "Profld3", "SubProIntfld1", "ProIntfld1", "ProIntfld2", "ProIntfld3",
                                                                    "Pubfld4", "Pubfld5", "Pubfld6", "Intfld4", "Intfld5", "Intfld6", "Profld4", "Profld5",
                                                                    "Profld6", "ProIntfld4", "ProIntfld5", "ProIntfld6",
                                                                    "Members", "DeclaredFieldNames", "InheritedFieldNames",  "MembersEverything", "PublicFieldNames"};

        public static string[] InheritedFieldNames = new string[] { };

        public static string[] PublicFieldNames = new string[] { "SubPubfld1", "Pubfld1", "Pubfld2", "Pubfld3", "Pubfld4", "Pubfld5", "Pubfld6",
                                                                 "Members", "DeclaredFieldNames", "InheritedFieldNames",  "MembersEverything", "PublicFieldNames"};

        public string SubPubfld1 = "";
        public string Pubfld1 = "";
        public readonly string Pubfld2 = "";
        public volatile string Pubfld3 = "";
        public static string Pubfld4 = "";
        public static readonly string Pubfld5 = "";
        public static volatile string Pubfld6 = "";

        internal string SubIntfld1 = "";
        internal string Intfld1 = "";
        internal readonly string Intfld2 = "";
        internal volatile string Intfld3 = "";
        internal static string Intfld4 = "";
        internal static readonly string Intfld5 = "";
        internal static volatile string Intfld6 = "";

        protected string SubProfld1 = "";
        protected string Profld1 = "";
        protected readonly string Profld2 = "";
        protected volatile string Profld3 = "";
        protected static string Profld4 = "";
        protected static readonly string Profld5 = "";
        protected static volatile string Profld6 = "";

        protected internal string SubProIntfld1 = "";
        protected internal string ProIntfld1 = "";
        protected internal readonly string ProIntfld2 = "";
        protected internal volatile string ProIntfld3 = "";
        protected internal static string ProIntfld4 = "";
        protected internal static readonly string ProIntfld5 = "";
        protected internal static volatile string ProIntfld6 = "";
    }

    public class FieldTestSubClass : FieldTestBaseClass
    {
        public static new int Members = 32;
        public static new int MembersEverything = 54;

        public static new string[] DeclaredFieldNames = new string[] { "Pubfld1", "Pubfld2", "Pubfld3", "Intfld1", "Intfld2", "Intfld3",
                                                                   "Profld1", "Profld2", "Profld3", "ProIntfld1", "ProIntfld2", "ProIntfld3",
                                                                   "Pubfld4", "Pubfld5", "Pubfld6", "Intfld4", "Intfld5", "Intfld6", "Profld4", "Profld5",
                                                                   "Profld6", "ProIntfld4", "ProIntfld5", "ProIntfld6",
                                                                    "Members", "DeclaredFieldNames", "InheritedFieldNames", "NewFieldNames", "MembersEverything", "PublicFieldNames"};


        public static new string[] InheritedFieldNames = new string[] { "SubPubfld1", "SubIntfld1", "SubProfld1", "SubProIntfld1" };

        public static string[] NewFieldNames = new string[] { "Pubfld1", "Pubfld2", "Pubfld3", "Intfld1", "Intfld2", "Intfld3",
                                                              "Profld1", "Profld2", "Profld3", "ProIntfld1", "ProIntfld2", "ProIntfld3"};

        public static new string[] PublicFieldNames = new string[] { "Pubfld1", "Pubfld2", "Pubfld3", "Pubfld4", "Pubfld5", "Pubfld6",
                                                                 "Members", "DeclaredFieldNames", "InheritedFieldNames", "NewFieldNames", "MembersEverything", "PublicFieldNames"};
        public new string Pubfld1 = "";
        public new readonly string Pubfld2 = "";
        public new volatile string Pubfld3 = "";
        public static new string Pubfld4 = "";
        public static new readonly string Pubfld5 = "";
        public static new volatile string Pubfld6 = "";

        internal new string Intfld1 = "";
        internal new readonly string Intfld2 = "";
        internal new volatile string Intfld3 = "";
        internal static new string Intfld4 = "";
        internal static new readonly string Intfld5 = "";
        internal static new volatile string Intfld6 = "";

        protected new string Profld1 = "";
        protected new readonly string Profld2 = "";
        protected new volatile string Profld3 = "";
        protected static new string Profld4 = "";
        protected static new readonly string Profld5 = "";
        protected static new volatile string Profld6 = "";

        protected internal new string ProIntfld1 = "";
        protected internal new readonly string ProIntfld2 = "";
        protected internal new volatile string ProIntfld3 = "";
        protected internal static new string ProIntfld4 = "";
        protected internal static new readonly string ProIntfld5 = "";
        protected internal static new volatile string ProIntfld6 = "";
    }
}
