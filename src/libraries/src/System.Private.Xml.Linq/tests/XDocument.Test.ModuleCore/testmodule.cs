// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // TestModule
    //
    ////////////////////////////////////////////////////////////////
    public abstract class TestModule : TestItem
    {
        //Data
        protected TestSpec ptestspec;
        protected List<TestModule> pincludes = new List<TestModule>();

        private int _ppass = 0;
        private int _pfail = 0;
        private int _pskip = 0;
        public int PassCount
        {
            get { return _ppass; }
            set { _ppass = value; }
        }

        public int FailCount
        {
            get { return _pfail; }
            set { _pfail = value; }
        }

        public int SkipCount
        {
            get { return _pskip; }
            set { _pskip = value; }
        }

        //Constructors
        public TestModule()
            : this(null, null)
        {
        }

        public TestModule(string name, string desc)
            : base(name, desc, TestType.TestModule)
        {
            this.Guid = GetType().ToString();

            //Population
            //DetermineChildren();

            try
            {
                DetermineIncludes();
                DetermineFilters();
            }
            catch (Exception e)
            {
                //Don't completely block construction, otherwise it won't be able to be loaded from COM
                TestLog.HandleException(e);
            }
        }

        public TestCase CurTestCase
        {
            //Return the current testcase:
            //Note: We do this so that within global functions (ie: at the module level) the user can 
            //have know which testcase/variation were in, without having to pass this state from
            //execute arround
            get { return (TestCase)pcurrentchild; }
            set { pcurrentchild = value; }
        }

        public override TestResult Execute()
        {
            TestItems children = Children;
            if (children != null && children.Count > 0)
            {
                // this is not a leaf node, just invoke all the children's execute
                foreach (object child in children)
                {
                    TestCase tc = child as TestCase;
                    if (tc != null)
                    {
                        if (TestInput.IsTestCaseSelected(tc.Name))
                        {
                            Console.WriteLine("TestCase:{0} - {1}", tc.Attribute.Name, tc.Attribute.Desc);
                            tc.Init();
                            tc.Execute();
                        }
                    }
                }
            }
            Console.WriteLine("Pass:{0}, Fail:{1}, Skip:{2}", PassCount, FailCount, SkipCount);
            return TestResult.Passed;
        }

        //Accessors
        public new virtual TestModuleAttribute Attribute
        {
            get { return (TestModuleAttribute)base.Attribute; }
            set { base.Attribute = value; }
        }

        public string Created
        {
            get { return Attribute.Created; }
            set { Attribute.Created = value; }
        }

        public string Modified
        {
            get { return Attribute.Modified; }
            set { Attribute.Modified = value; }
        }

        public TestSpec TestSpec
        {
            get
            {
                //Deferred Creation
                if (ptestspec == null)
                    ptestspec = new TestSpec(this);
                return ptestspec;
            }
        }

        public override void Init()
        {
            //Delegate
            base.Init();

            //Includes (aggregate tests)
            foreach (TestModule include in pincludes)
                include.Init();
        }

        public override void Terminate()
        {
            //Includes (aggregate tests)
            foreach (TestModule include in pincludes)
                include.Terminate();

            //Delegate
            base.Terminate();
        }

        //Helpers

        protected override TestAttribute CreateAttribute()
        {
            return new TestModuleAttribute();
        }

        protected override void DetermineChildren()
        {
            base.DetermineChildren();
        }

        protected virtual void DetermineIncludes()
        {
        }

        protected virtual void DetermineFilters()
        {
        }

        protected virtual string FilterScope(string xpath)
        {
            //Basically we want to allow either simply filtering at the variation node (ie: no scope),
            //in which case we'll just add the 'assumed' scope, or allow filtering at any level.  
            //We also want to be consitent with the XmlDriver in which all filters are predicates only.
            string varfilter = "//TestVariation[{0}]";
            if (xpath != null)
            {
                xpath = xpath.Trim();
                if (xpath.Length > 0)
                {
                    //Add the Variation Scope, if no scope was specified
                    if (xpath[0] != '/')
                        xpath = string.Format(varfilter, xpath);
                }
            }

            return xpath;
        }
    }
}
