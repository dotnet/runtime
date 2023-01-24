// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

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
        private static string s_pinitstring;
        private static string s_pcommandline;

        //Constructor
        public static TestProps Properties
        {
            get
            {
                if (s_pproperties == null)
                {
                    s_pproperties = new TestProps(new TestProperties());

                    //CommandLine
                    string commandline = s_pcommandline;
                    s_pproperties["CommandLine"] = commandline;

                    //CommandLine Options
                    KeywordParser.Tokens tokens = new KeywordParser.Tokens();
                    tokens.Equal = " ";
                    tokens.Separator = "/";
                    Dictionary<string, string> options = KeywordParser.ParseKeywords(commandline, tokens);
                    foreach (string key in options.Keys)
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
                    string initstring = value["Alias/InitString"];
                    TestInput.Properties["Alias/InitString"] = initstring;

                    Dictionary<string, string> keywords = KeywordParser.ParseKeywords(initstring);
                    foreach (string key in keywords.Keys)
                        TestInput.Properties["Alias/InitString/" + key] = keywords[key] as string;

                    // Command line keywords (if any)
                    TestProp commandLineProp = value.Get("CommandLine");
                    if (commandLineProp != null)
                    {
                        string commandLine = value["CommandLine"];
                        TestInput.Properties["CommandLine"] = commandLine;

                        //CommandLine Options
                        KeywordParser.Tokens tokens = new KeywordParser.Tokens();
                        tokens.Equal = " ";
                        tokens.Separator = "/";
                        Dictionary<string, string> options = KeywordParser.ParseKeywords(commandLine, tokens);
                        foreach (string key in options.Keys)
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

        public static string Filter
        {
            get
            {
                if (TestInput.Properties != null)
                    return TestInput.Properties["CommandLine/Filter"];
                return null;
            }
        }

        public static string MaxPriority
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

        public virtual string this[string name]
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

        public virtual TestProp Get(string name)
        {
            ITestProperty property = pinternal.Get(name);
            if (property != null)
                return new TestProp(property);
            return null;
        }

        public virtual TestProp Add(string name)
        {
            return new TestProp(pinternal.Add(name));
        }

        public virtual void Remove(string name)
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

        public virtual object Current
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

        ITestProperty ITestProperties.Add(string name)
        {
            return pinternal.Add(name);
        }

        ITestProperty ITestProperties.Get(string name)
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

        public virtual string Name
        {
            get { return pinternal.Name; }
        }

        public virtual string Desc
        {
            get { return pinternal.Desc; }
        }

        public virtual TestPropertyFlags Flags
        {
            get { return pinternal.Flags; }
            set { pinternal.Flags = value; }
        }

        public virtual object Value
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
        protected string pname = null;
        protected string pdesc = null;
        protected object pvalue = null;
        protected TestPropertyFlags pflags = 0;
        protected TestProperties pmetadata = null;
        protected TestProperties pchildren = null;

        //Constructor
        public TestProperty(string name, object value)
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

        public virtual object this[string name]
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
                if (string.Compare(plist[i].Name, name) == 0)
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

        public virtual ITestProperty Get(string name)
        {
            int index = this.IndexOf(name);
            if (index >= 0)
                return plist[index];
            return null;
        }

        ITestProperty ITestProperties.Add(string name)
        {
            return (TestProperty)Add(name);
        }

        public virtual TestProperty Add(string name)
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

        public virtual void Remove(string name)
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
