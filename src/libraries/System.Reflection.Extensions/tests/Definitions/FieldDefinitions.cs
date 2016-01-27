// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 3026

namespace System.Reflection.Tests
{
    public class FieldTestBaseClass
    {
        public static int Members = 35;
        public static int MembersEverything = 41;

        public static String[] DeclaredFieldNames = new String[] { "SubPubfld1", "Pubfld1", "Pubfld2", "Pubfld3", "SubIntfld1", "Intfld1", "Intfld2", "Intfld3", "SubProfld1",
                                                                    "Profld1", "Profld2", "Profld3", "SubProIntfld1", "ProIntfld1", "ProIntfld2", "ProIntfld3",
                                                                    "Pubfld4", "Pubfld5", "Pubfld6", "Intfld4", "Intfld5", "Intfld6", "Profld4", "Profld5",
                                                                    "Profld6", "ProIntfld4", "ProIntfld5", "ProIntfld6",
                                                                    "Members", "DeclaredFieldNames", "InheritedFieldNames",  "MembersEverything", "PublicFieldNames"};

        public static String[] InheritedFieldNames = new String[] { };

        public static String[] PublicFieldNames = new String[] { "SubPubfld1", "Pubfld1", "Pubfld2", "Pubfld3", "Pubfld4", "Pubfld5", "Pubfld6",
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
        public new static int Members = 32;
        public new static int MembersEverything = 54;

        public new static String[] DeclaredFieldNames = new String[] { "Pubfld1", "Pubfld2", "Pubfld3", "Intfld1", "Intfld2", "Intfld3",
                                                                   "Profld1", "Profld2", "Profld3", "ProIntfld1", "ProIntfld2", "ProIntfld3",
                                                                   "Pubfld4", "Pubfld5", "Pubfld6", "Intfld4", "Intfld5", "Intfld6", "Profld4", "Profld5",
                                                                   "Profld6", "ProIntfld4", "ProIntfld5", "ProIntfld6",
                                                                    "Members", "DeclaredFieldNames", "InheritedFieldNames", "NewFieldNames", "MembersEverything", "PublicFieldNames"};


        public new static String[] InheritedFieldNames = new String[] { "SubPubfld1", "SubIntfld1", "SubProfld1", "SubProIntfld1" };

        public static String[] NewFieldNames = new String[] { "Pubfld1", "Pubfld2", "Pubfld3", "Intfld1", "Intfld2", "Intfld3",
                                                              "Profld1", "Profld2", "Profld3", "ProIntfld1", "ProIntfld2", "ProIntfld3"};

        public new static String[] PublicFieldNames = new String[] { "Pubfld1", "Pubfld2", "Pubfld3", "Pubfld4", "Pubfld5", "Pubfld6",
                                                                 "Members", "DeclaredFieldNames", "InheritedFieldNames", "NewFieldNames", "MembersEverything", "PublicFieldNames"};
        public new string Pubfld1 = "";
        public new readonly string Pubfld2 = "";
        public new volatile string Pubfld3 = "";
        public new static string Pubfld4 = "";
        public new static readonly string Pubfld5 = "";
        public new static volatile string Pubfld6 = "";

        internal new string Intfld1 = "";
        internal new readonly string Intfld2 = "";
        internal new volatile string Intfld3 = "";
        internal new static string Intfld4 = "";
        internal new static readonly string Intfld5 = "";
        internal new static volatile string Intfld6 = "";

        protected new string Profld1 = "";
        protected new readonly string Profld2 = "";
        protected new volatile string Profld3 = "";
        protected new static string Profld4 = "";
        protected new static readonly string Profld5 = "";
        protected new static volatile string Profld6 = "";

        protected internal new string ProIntfld1 = "";
        protected internal new readonly string ProIntfld2 = "";
        protected internal new volatile string ProIntfld3 = "";
        protected internal new static string ProIntfld4 = "";
        protected internal new static readonly string ProIntfld5 = "";
        protected internal new static volatile string ProIntfld6 = "";
    }
}
