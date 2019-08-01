// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Tests
{
    internal class PropertyTestBaseClass
    {
        public static int Members = 67;
        public static int MembersEverything = 73;

        public static string[] DeclaredPropertyNames = new string[] { "SubPubprop1", "Pubprop1", "Pubprop2", "Pubprop3", "SubIntprop1", "Intprop1", "Intprop2", "Intprop3", "SubProprop1",
                                                                    "Proprop1", "Proprop2", "Proprop3", "SubProIntprop1", "ProIntprop1", "ProIntprop2", "ProIntprop3",
                                                                    };

        public static string[] InheritedPropertyNames = new string[] { };

        public static string[] InheritedButHiddenPropertyNames = new string[] { };

        public static string[] PublicPropertyNames = new string[] { "SubPubprop1", "Pubprop1", "Pubprop2", "Pubprop3" };

        public string Pubprop1 { get { return ""; } set { } }
        public string SubPubprop1 { get { return ""; } set { } }
        public virtual string Pubprop2 { get { return ""; } set { } }
        public static string Pubprop3 { get { return ""; } set { } }

        internal string Intprop1 { get { return ""; } set { } }
        internal string SubIntprop1 { get { return ""; } set { } }
        internal virtual string Intprop2 { get { return ""; } set { } }
        internal static string Intprop3 { get { return ""; } set { } }

        protected string Proprop1 { get { return ""; } set { } }
        protected string SubProprop1 { get { return ""; } set { } }
        protected virtual string Proprop2 { get { return ""; } set { } }
        protected static string Proprop3 { get { return ""; } set { } }

        protected internal string ProIntprop1 { get { return ""; } set { } }
        protected string SubProIntprop1 { get { return ""; } set { } }
        protected internal virtual string ProIntprop2 { get { return ""; } set { } }
        protected internal static string ProIntprop3 { get { return ""; } set { } }
    }

    internal class PropertyTestSubClass : PropertyTestBaseClass
    {
        public static new int Members = 55;
        public static new int MembersEverything = 89;

        public static new string[] DeclaredPropertyNames = new string[] { "Pubprop1", "Pubprop2", "Pubprop3", "Intprop1", "Intprop2", "Intprop3",
                                                                   "Proprop1", "Proprop2", "Proprop3", "ProIntprop1", "ProIntprop2", "ProIntprop3",
                                                                   };

        public static new string[] InheritedPropertyNames = new string[] { "SubPubprop1", "SubIntprop1", "SubProprop1", "SubProIntprop1" };

        public static new string[] InheritedButHiddenPropertyNames = new string[] { "Pubprop1", "Pubprop2", "Intprop1", "Intprop2",
                                                                                    "Proprop1", "Proprop2", "ProIntprop1", "ProIntprop2", };

        public static new string[] PublicPropertyNames = new string[] { "Pubprop1", "Pubprop2", "Pubprop3" };

        public new string Pubprop1 { get { return ""; } set { } }
        public new virtual string Pubprop2 { get { return ""; } set { } }
        public static new string Pubprop3 { get { return ""; } set { } }

        internal new string Intprop1 { get { return ""; } set { } }
        internal new virtual string Intprop2 { get { return ""; } set { } }
        internal static new string Intprop3 { get { return ""; } set { } }

        protected new string Proprop1 { get { return ""; } set { } }
        protected new virtual string Proprop2 { get { return ""; } set { } }
        protected static new string Proprop3 { get { return ""; } set { } }

        protected new internal string ProIntprop1 { get { return ""; } set { } }
        protected new internal virtual string ProIntprop2 { get { return ""; } set { } }
        protected new internal static string ProIntprop3 { get { return ""; } set { } }
    }

    public class PropertyTestBaseIndexerClass
    {
        public static int Members = 16;
        public static int MembersEverything = 22;

        public static string[] DeclaredPropertyNames = new string[] { "Item", "Item", "Item" };
        public static string[] InheritedPropertyNames = new string[] { };
        public static string[] InheritedButHiddenPropertyNames = new string[] { };
        public static string[] PublicPropertyNames = new string[] { "Item", "Item", "Item" };

        public int this[int i] { get { return 1; } set { } }
        public int this[string i] { get { return 1; } set { } }
        public int this[DateTime i] { get { return 1; } set { } }
    }

    public class PropertyTestSubIndexerClass : PropertyTestBaseIndexerClass
    {
        public static new int Members = 13;
        public static new int MembersEverything = 26;

        public static new string[] DeclaredPropertyNames = new string[] { "Item", "Item" };
        public static new string[] InheritedPropertyNames = new string[] { "Item" };
        public static new string[] InheritedButHiddenPropertyNames = new string[] { "Item", "Item" };
        public static new string[] PublicPropertyNames = new string[] { "Item", "Item" };

        public new int this[int i] { get { return 1; } set { } }
        public new int this[string i] { get { return 1; } set { } }
    }

    internal abstract class PropertyTestBaseAbsClass
    {
        public static int Members = 19;
        public static int MembersEverything = 25;

        public static string[] DeclaredPropertyNames = new string[] { "prop1", "prop2", "prop3", "prop4" };
        public static string[] InheritedPropertyNames = new string[] { };
        public static string[] InheritedButHiddenPropertyNames = new string[] { };
        public static string[] PublicPropertyNames = new string[] { "prop1" };

        public abstract string prop1 { get; set; }
        internal abstract string prop2 { get; set; }
        protected abstract string prop3 { get; set; }
        protected internal abstract string prop4 { get; set; }
    }

    internal abstract class PropertyTestSubAbsClass : PropertyTestBaseAbsClass
    {
        public static new int Members = 19;
        public static new int MembersEverything = 25;

        public static new string[] DeclaredPropertyNames = new string[] { "prop1", "prop2", "prop3", "prop4" };
        public static new string[] InheritedPropertyNames = new string[] { };
        public static new string[] InheritedButHiddenPropertyNames = new string[] { };
        public static new string[] PublicPropertyNames = new string[] { "prop1" };

        public override abstract string prop1 { get; set; }
        internal abstract override string prop2 { get; set; }
        protected abstract override string prop3 { get; set; }
        protected internal override abstract string prop4 { get; set; }
    }
}
