// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Tests
{
    internal class PropertyTestBaseClass
    {
        public static int Members = 67;
        public static int MembersEverything = 73;

        public static String[] DeclaredPropertyNames = new String[] { "SubPubprop1", "Pubprop1", "Pubprop2", "Pubprop3", "SubIntprop1", "Intprop1", "Intprop2", "Intprop3", "SubProprop1",
                                                                    "Proprop1", "Proprop2", "Proprop3", "SubProIntprop1", "ProIntprop1", "ProIntprop2", "ProIntprop3",
                                                                    };

        public static String[] InheritedPropertyNames = new String[] { };

        public static String[] InheritedButHiddenPropertyNames = new String[] { };

        public static String[] PublicPropertyNames = new String[] { "SubPubprop1", "Pubprop1", "Pubprop2", "Pubprop3" };

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
        public new static int Members = 55;
        public new static int MembersEverything = 89;

        public new static String[] DeclaredPropertyNames = new String[] { "Pubprop1", "Pubprop2", "Pubprop3", "Intprop1", "Intprop2", "Intprop3",
                                                                   "Proprop1", "Proprop2", "Proprop3", "ProIntprop1", "ProIntprop2", "ProIntprop3",
                                                                   };

        public new static String[] InheritedPropertyNames = new String[] { "SubPubprop1", "SubIntprop1", "SubProprop1", "SubProIntprop1" };

        public new static String[] InheritedButHiddenPropertyNames = new String[] { "Pubprop1", "Pubprop2", "Intprop1", "Intprop2",
                                                                                    "Proprop1", "Proprop2", "ProIntprop1", "ProIntprop2", };

        public new static String[] PublicPropertyNames = new String[] { "Pubprop1", "Pubprop2", "Pubprop3" };

        public new string Pubprop1 { get { return ""; } set { } }
        public new virtual string Pubprop2 { get { return ""; } set { } }
        public new static string Pubprop3 { get { return ""; } set { } }

        internal new string Intprop1 { get { return ""; } set { } }
        internal new virtual string Intprop2 { get { return ""; } set { } }
        internal new static string Intprop3 { get { return ""; } set { } }

        protected new string Proprop1 { get { return ""; } set { } }
        protected new virtual string Proprop2 { get { return ""; } set { } }
        protected new static string Proprop3 { get { return ""; } set { } }

        protected new internal string ProIntprop1 { get { return ""; } set { } }
        protected new internal virtual string ProIntprop2 { get { return ""; } set { } }
        protected new internal static string ProIntprop3 { get { return ""; } set { } }
    }

    public class PropertyTestBaseIndexerClass
    {
        public static int Members = 16;
        public static int MembersEverything = 22;

        public static String[] DeclaredPropertyNames = new String[] { "Item", "Item", "Item" };
        public static String[] InheritedPropertyNames = new String[] { };
        public static String[] InheritedButHiddenPropertyNames = new String[] { };
        public static String[] PublicPropertyNames = new String[] { "Item", "Item", "Item" };

        public int this[int i] { get { return 1; } set { } }
        public int this[string i] { get { return 1; } set { } }
        public int this[DateTime i] { get { return 1; } set { } }
    }

    public class PropertyTestSubIndexerClass : PropertyTestBaseIndexerClass
    {
        public new static int Members = 13;
        public new static int MembersEverything = 26;

        public new static String[] DeclaredPropertyNames = new String[] { "Item", "Item" };
        public new static String[] InheritedPropertyNames = new String[] { "Item" };
        public new static String[] InheritedButHiddenPropertyNames = new String[] { "Item", "Item" };
        public new static String[] PublicPropertyNames = new String[] { "Item", "Item" };

        public new int this[int i] { get { return 1; } set { } }
        public new int this[string i] { get { return 1; } set { } }
    }

    internal abstract class PropertyTestBaseAbsClass
    {
        public static int Members = 19;
        public static int MembersEverything = 25;

        public static String[] DeclaredPropertyNames = new String[] { "prop1", "prop2", "prop3", "prop4" };
        public static String[] InheritedPropertyNames = new String[] { };
        public static String[] InheritedButHiddenPropertyNames = new String[] { };
        public static String[] PublicPropertyNames = new String[] { "prop1" };

        public abstract string prop1 { get; set; }
        internal abstract string prop2 { get; set; }
        protected abstract string prop3 { get; set; }
        protected internal abstract string prop4 { get; set; }
    }

    internal abstract class PropertyTestSubAbsClass : PropertyTestBaseAbsClass
    {
        public new static int Members = 19;
        public new static int MembersEverything = 25;

        public new static String[] DeclaredPropertyNames = new String[] { "prop1", "prop2", "prop3", "prop4" };
        public new static String[] InheritedPropertyNames = new String[] { };
        public new static String[] InheritedButHiddenPropertyNames = new String[] { };
        public new static String[] PublicPropertyNames = new String[] { "prop1" };

        public override abstract string prop1 { get; set; }
        internal abstract override string prop2 { get; set; }
        protected abstract override string prop3 { get; set; }
        protected internal override abstract string prop4 { get; set; }
    }
}
