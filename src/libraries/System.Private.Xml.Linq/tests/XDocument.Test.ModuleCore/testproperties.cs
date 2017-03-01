// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;			//IEnumerator
using System.Collections.Generic;   //List<T>

namespace Microsoft.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // TestInput
    //
    ////////////////////////////////////////////////////////////////
    public class TestInput
    {
        //Data
        private static TestProps s_pproperties;
        private static String s_pinitstring;
        private static String s_pcommandline;

        //Constructor
        public static TestProps Properties
        {
            get
            {
                if (s_pproperties == null)
                {
                    s_pproperties = new TestProps(new TestProperties());

                    //CommandLine
                    String commandline = s_pcommandline;
                    s_pproperties["CommandLine"] = commandline;

                    //CommandLine Options
                    KeywordParser.Tokens tokens = new KeywordParser.Tokens();
                    tokens.Equal = " ";
                    tokens.Seperator = "/";
                    Dictionary<string, string> options = KeywordParser.ParseKeywords(commandline, tokens);
                    foreach (String key in options.Keys)
                        s_pproperties["CommandLine/" + key] = options[key];
                }
                return s_pproperties;
            }
            set
            {
                if (value != null)
                {
                    //Clear between each run
                    s_pproperties = null;

                    //InitString keywords
                    String initstring = value["Alias/InitString"];
                    TestInput.Properties["Alias/InitString"] = initstring;

                    Dictionary<string, string> keywords = KeywordParser.ParseKeywords(initstring);
                    foreach (String key in keywords.Keys)
                        TestInput.Properties["Alias/InitString/" + key] = keywords[key] as String;

                    // Command line keywords (if any)
                    TestProp commandLineProp = value.Get("CommandLine");
                    if (commandLineProp != null)
                    {
                        string commandLine = value["CommandLine"];
                        TestInput.Properties["CommandLine"] = commandLine;

                        //CommandLine Options
                        KeywordParser.Tokens tokens = new KeywordParser.Tokens();
                        tokens.Equal = " ";
                        tokens.Seperator = "/";
                        Dictionary<string, string> options = KeywordParser.ParseKeywords(commandLine, tokens);
                        foreach (String key in options.Keys)
                            TestInput.Properties["CommandLine/" + key] = options[key];
                    }
                }
            }
        }

        public static bool IsTestCaseSelected(string testcasename)
        {
            bool ret = true;
            string testcasefilter = Properties["CommandLine/testcase"];
            if (testcasefilter != null
                && testcasefilter != "*"
                && testcasefilter != testcasename)
            {
                ret = false;
            }

            return ret;
        }

        public static bool IsVariationSelected(string variationname)
        {
            bool ret = true;
            string variationfilter = s_pproperties["variation"];
            if (variationfilter != null
                && variationfilter != "*"
                && variationfilter != variationname)
            {
                ret = false;
            }

            return ret;
        }

        internal static void Dispose()
        {
            //Reset the info.  
            //Since this is a static class, (to make it simplier to access from anywhere in your code)
            //we need to reset this info every time a test is run - so if you don't select an alias
            //the next time it doesn't use the previous alias setting - ie: ProviderInfo doesn't 
            //get called when no alias is selected...
            s_pproperties = null;
            s_pinitstring = null;
            s_pcommandline = null;
        }

        public static string InitString
        {
            get
            {
                //Useful typed getter
                if (s_pinitstring == null)
                    s_pinitstring = TestInput.Properties["Alias/InitString"];
                return s_pinitstring;
            }
        }

        public static string CommandLine
        {
            get
            {
                //Useful typed getter
                if (s_pcommandline == null)
                    s_pcommandline = TestInput.Properties["CommandLine"];
                return s_pcommandline;
            }
            set
            {
                s_pcommandline = value;
            }
        }

        public static String Filter
        {
            get
            {
                if (TestInput.Properties != null)
                    return TestInput.Properties["CommandLine/Filter"];
                return null;
            }
        }

        public static String MaxPriority
        {
            get
            {
                if (TestInput.Properties != null)
                    return TestInput.Properties["CommandLine/MaxPriority"];
                return null;
            }
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestProps
    //
    ////////////////////////////////////////////////////////////////
    public class TestProps : ITestProperties, IEnumerable, IEnumerator
    {
        //Data
        protected ITestProperties pinternal;
        protected int penum = -1;

        //Constructor
        public TestProps(ITestProperties properties)
        {
            pinternal = properties;
        }

        //Accessors
        public virtual ITestProperties Internal
        {
            get { return pinternal; }
        }

        public virtual int Count
        {
            get
            {
                if (pinternal != null)
                    return pinternal.Count;
                return 0;
            }
        }

        public virtual TestProp this[int index]
        {
            get
            {
                ITestProperty property = pinternal.GetItem(index);
                if (property != null)
                    return new TestProp(property);
                return null;
            }
        }

        public virtual String this[String name]
        {
            get
            {
                ITestProperty property = pinternal.Get(name);
                if (property != null)
                    return StringEx.ToString(property.Value);
                return null;
            }
            set
            {
                this.Add(name).Value = value;
            }
        }

        public virtual TestProp Get(String name)
        {
            ITestProperty property = pinternal.Get(name);
            if (property != null)
                return new TestProp(property);
            return null;
        }

        public virtual TestProp Add(String name)
        {
            return new TestProp(pinternal.Add(name));
        }

        public virtual void Remove(String name)
        {
            pinternal.Remove(name);
        }

        public virtual IEnumerator GetEnumerator()
        {
            return this;
        }

        public virtual bool MoveNext()
        {
            if (penum + 1 >= this.Count)
                return false;
            penum++;
            return true;
        }

        public virtual Object Current
        {
            get { return this[penum]; }
        }

        public virtual void Reset()
        {
            penum = -1;
        }

        public virtual void Clear()
        {
            if (pinternal != null)
                pinternal.Clear();
        }

        ITestProperty ITestProperties.Add(String name)
        {
            return pinternal.Add(name);
        }

        ITestProperty ITestProperties.Get(String name)
        {
            return pinternal.Get(name);
        }

        ITestProperty ITestProperties.GetItem(int index)
        {
            return pinternal.GetItem(index);
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestProp
    //
    ////////////////////////////////////////////////////////////////
    public class TestProp : ITestProperty, IEnumerable
    {
        //Data
        protected ITestProperty pinternal;

        //Constructor
        public TestProp(ITestProperty property)
        {
            pinternal = property;
        }

        //Accessors
        public virtual ITestProperty Internal
        {
            get { return pinternal; }
        }

        public virtual String Name
        {
            get { return pinternal.Name; }
        }

        public virtual String Desc
        {
            get { return pinternal.Desc; }
        }

        public virtual TestPropertyFlags Flags
        {
            get { return pinternal.Flags; }
            set { pinternal.Flags = value; }
        }

        public virtual Object Value
        {
            get { return pinternal.Value; }
            set { pinternal.set_Value(ref value); }
        }

        public virtual TestProps Children
        {
            get { return new TestProps(pinternal.Children); }
        }

        public virtual IEnumerator GetEnumerator()
        {
            return this.Children;
        }

        void ITestProperty.set_Value(ref object value)
        {
            pinternal.set_Value(ref value);
        }

        ITestProperties ITestProperty.Children
        {
            get { return pinternal.Children; }
        }

        ITestProperties ITestProperty.Metadata
        {
            get { return pinternal.Metadata; }
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestProperty
    //
    ////////////////////////////////////////////////////////////////
    public class TestProperty : ITestProperty
    {
        //Data
        protected String pname = null;
        protected String pdesc = null;
        protected Object pvalue = null;
        protected TestPropertyFlags pflags = 0;
        protected TestProperties pmetadata = null;
        protected TestProperties pchildren = null;

        //Constructor
        public TestProperty(String name, Object value)
        {
            pname = name;
            pvalue = value;
        }

        //Accessors
        public string Name
        {
            get { return pname; }
            set { pname = value; }
        }

        public string Desc
        {
            get { return pdesc; }
            set { pdesc = value; }
        }

        public TestPropertyFlags Flags
        {
            get { return pflags; }
            set { pflags = value; }
        }

        public object Value
        {
            get { return pvalue; }
            set { pvalue = value; }
        }

        public void set_Value(ref object value)
        {
            pvalue = value;
        }

        public ITestProperties Metadata
        {
            get { return pmetadata; }
        }

        public ITestProperties Children
        {
            get { return pchildren; }
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestProperties
    //
    ////////////////////////////////////////////////////////////////
    public class TestProperties : ITestProperties, IEnumerable
    {
        //Data
        protected List<TestProperty> plist = null;

        //Constructor
        public TestProperties()
        {
            plist = new List<TestProperty>();
        }

        //Methods
        public virtual int Count
        {
            get { return plist.Count; }
        }

        public virtual TestProperty this[int index]
        {
            get { return plist[index]; }
        }

        public virtual Object this[String name]
        {
            get
            {
                ITestProperty property = this.Get(name);
                if (property != null)
                    return property.Value;
                return null;
            }
            set { this.Add(name).Value = value; }
        }

        public virtual int IndexOf(string name)
        {
            int count = plist.Count;
            for (int i = 0; i < count; i++)
            {
                if (String.Compare(plist[i].Name, name) == 0)
                    return i;
            }
            return -1;
        }

        public virtual IEnumerator GetEnumerator()
        {
            return plist.GetEnumerator();
        }

        ITestProperty ITestProperties.GetItem(int index)
        {
            return this[index];
        }

        public virtual ITestProperty Get(String name)
        {
            int index = this.IndexOf(name);
            if (index >= 0)
                return plist[index];
            return null;
        }

        ITestProperty ITestProperties.Add(String name)
        {
            return (TestProperty)Add(name);
        }

        public virtual TestProperty Add(String name)
        {
            //Exists
            int index = this.IndexOf(name);
            if (index >= 0)
                return plist[index];

            //Otherwise add
            TestProperty property = new TestProperty(name, null);
            plist.Add(property);
            return property;
        }

        public virtual void Remove(String name)
        {
            int index = this.IndexOf(name);
            if (index >= 0)
                plist.RemoveAt(index);
        }

        public virtual void Clear()
        {
            plist.Clear();
        }
    }
}
