// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;		//Assert
using System.IO;
using System.Collections.Generic;

namespace OLEDB.Test.ModuleCore
{
    public class CTestModule : CTestBase
    {
        //Data
        private string _clsid;
        private CTestCase _curtestcase;

        private int _pass = 0;
        private int _fail = 0;
        private int _skip = 0;
        public int PassCount
        {
            get { return _pass; }
            set { _pass = value; }
        }

        public int FailCount
        {
            get { return _fail; }
            set { _fail = value; }
        }

        public int SkipCount
        {
            get { return _skip; }
            set { _skip = value; }
        }

        //Constructors
        public CTestModule()
            : this(null, "Microsoft", 1)
        {
        }

        public CTestModule(string desc)
            : this(desc, "Microsoft", 1)
        {
            //Delegate
        }

        public CTestModule(string desc, string owner, int version)
            : base(desc)
        {
            _clsid = GetType().ToString();

            //The attribute should take precedence
            if (this.Owner == null)
                this.Owner = owner;
            if (this.Version <= 0)
                this.Version = version;

            //Population
            DetermineIncludes();
            DetermineChildren();
            DetermineFilters();
        }

        static CTestModule()
        {
        }

        //Accessors
        public new virtual TestModule Attribute
        {
            get { return (TestModule)base.Attribute; }
            set { base.Attribute = value; }
        }

        public string Owner
        {
            get { return Attribute.Owner; }
            set { Attribute.Owner = value; }
        }

        public string[] Owners
        {
            get { return Attribute.Owners; }
            set { Attribute.Owners = value; }
        }

        public int Version
        {
            get { return Attribute.Version; }
            set { Attribute.Version = value; }
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
        public override int Init(object o)
        {
            //Delegate
            int result = base.Init(o);

            return result;
        }

        public override int Terminate(object o)
        {
            //Delegate
            return base.Terminate(o);
        }

        //Helpers
        public void AddTestCase(CTestCase testcase)
        {
            //Delegate
            this.AddChild(testcase);
        }

        public virtual string GetCLSID()
        {
            return _clsid;
        }

        public virtual string GetOwnerName()
        {
            return Attribute.Owner;
        }

        public virtual int GetVersion()
        {
            return Attribute.Version;
        }

        public void SetErrorInterface(IError rIError)
        {
            //Delegate to our global object
            CError.Error = rIError;

            CError.WriteLine();
        }

        public IError GetErrorInterface()
        {
            //Delegate to our global object
            return CError.Error;
        }

        public int GetCaseCount()
        {
            return Children.Count;
        }

        public ITestCases GetCase(int index)
        {
            return (ITestCases)Children[index];
        }

        protected override CAttrBase CreateAttribute()
        {
            return new TestModule();
        }

        protected override void DetermineChildren()
        {
            try
            {
                DetermineTestCases();
            }

            catch (Exception e)
            {
                //Make this easier to debug
                Console.WriteLine(e);
                Common.Assert(false, e.ToString());
                throw e;
            }

        }

        public virtual void DetermineTestCases()
        {

            //Default - no sort
            //int idPrev = Int32.MinValue;
            bool bSort = false;

            //Normally the reflection Type.GetMethods() api returns the methods in order 
            //of how they appear in the code.  But it will change that order depending 
            //upon if there are virtual functions, which are returned first before other
            //non-virtual functions.  Then there are also many times were people want multiple 
            //source files, so its totally up the compiler on how it pulls in the files (*.cs).
            //So we have added the support of specifying an id=x, as an attribute so you can have
            //then sorted and displayed however your see fit.
            if (bSort)
                Children.Sort(/*Default sort is based upon IComparable of each item*/);
        }

        protected virtual void DetermineIncludes()
        {
        }

        protected virtual void DetermineFilters()
        {
        }

        protected virtual string FilterScope(string xpath)
        {
            //Basically we want to allow either simply filtering at the variation node (i.e.: no scope),
            //in which case we'll just add the 'assumed' scope, or allow filtering at any level.  
            //We also want to be consistent with the XmlDriver in which all filters are predicates only.
            string varfilter = "//Variation[{0}]";
            if (xpath != null)
            {
                xpath = xpath.Trim();
                if (xpath.Length > 0)
                {
                    //Add the Variation Scope, if no scope was specified
                    if (xpath[0] != '/')
                        xpath = String.Format(varfilter, xpath);
                }
            }

            return xpath;
        }

        public CTestCase CurTestCase
        {
            //Return the current test case:
            //Note: We do this so that within global functions (i.e.: at the module level) the user can 
            //have know which test case/variation were in, without having to pass this state from
            //execute around
            get { return _curtestcase; }
            set { _curtestcase = value; }
        }

        public override tagVARIATION_STATUS Execute()
        {
            List<object> children = Children;
            if (children != null && children.Count > 0)
            {
                // this is not a leaf node, just invoke all the children's execute
                foreach (object child in children)
                {
                    CTestCase tc = child as CTestCase;
                    if (tc != null)
                    {
                        if (CModInfo.IsTestCaseSelected(tc.Name))
                        {
                            Console.WriteLine("TestCase:{0} - {1}", tc.Attribute.Name, tc.Attribute.Desc);
                            tc.Init();
                            tc.Execute();
                        }
                    }
                }
            }
            Console.WriteLine("Pass:{0}, Fail:{1}, Skip:{2}", PassCount, FailCount, SkipCount);
            return tagVARIATION_STATUS.eVariationStatusPassed;
        }

        public override IEnumerable<XunitTestCase> TestCases()
        {
            List<object> children = Children;
            if (children != null && children.Count > 0)
            {
                foreach (object child in children)
                {
                    CTestCase tc = child as CTestCase;
                    if (tc != null)
                    {
                        if (CModInfo.IsTestCaseSelected(tc.Name))
                        {
                            tc.Init();
                            foreach (XunitTestCase testCase in tc.TestCases())
                            {
                                yield return testCase;
                            }
                        }
                    }
                }
            }
        }
    }
}
