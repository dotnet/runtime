// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;		//StackTrace
using System.Diagnostics;

namespace OLEDB.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // CTestCases
    //
    ////////////////////////////////////////////////////////////////
    public class CTestCase : CTestBase//, ITestCases
    {
        public delegate int dlgtTestVariation();
        //Data
        private CVariation _curvariation;

        //Constructor
        public CTestCase()
            : this(null)
        {
            //Delegate
        }

        public CTestCase(string desc)
            : this(null, desc)
        {
            //Delegate
        }

        public CTestCase(CTestModule parent, string desc)
            : base(desc)
        {
            Parent = parent;
        }

        public virtual void DOTEST()
        {
            if (this.Attribute != null)
            {
                Console.WriteLine("TestCase:{0} - {1}", this.Attribute.Name, this.Attribute.Desc);
            }
        }

        public override tagVARIATION_STATUS Execute()
        {
            List<object> children = Children;
            if (children != null && children.Count > 0)
            {
                // this is not a leaf node, just invoke all the children's execute
                foreach (object child in children)
                {
                    CTestCase childTc = child as CTestCase;
                    if (childTc != null)    //nested test case class will be child of a test case
                    {
                        childTc.Init();
                        childTc.Execute();
                        continue;
                    }
                    CVariation var = child as CVariation;
                    if (var != null && CModInfo.IsVariationSelected(var.Desc))
                    {
                        const string indent = "\t";
                        try
                        {
                            CurVariation = var;
                            tagVARIATION_STATUS ret = var.Execute();
                            if (tagVARIATION_STATUS.eVariationStatusPassed == ret)
                            {
                                TestModule.PassCount++;
                            }
                            else if (tagVARIATION_STATUS.eVariationStatusFailed == ret)
                            {
                                System.Console.WriteLine(indent + var.Desc);
                                System.Console.WriteLine(indent + " FAILED");
                                TestModule.FailCount++;
                            }
                            else
                            {
                                TestModule.SkipCount++;
                            }
                        }
                        catch (CTestSkippedException)
                        {
                            TestModule.SkipCount++;
                        }
                        catch (Exception e)
                        {
                            System.Console.WriteLine(indent + var.Desc);
                            System.Console.WriteLine("unexpected exception happened:{0}", e.Message);
                            System.Console.WriteLine(e.StackTrace);
                            System.Console.WriteLine(indent + " FAILED");
                            TestModule.FailCount++;
                        }
                    }
                }
            }
            return tagVARIATION_STATUS.eVariationStatusPassed;
        }

        public override IEnumerable<XunitTestCase> TestCases()
        {
            List<object> children = Children;
            if (children != null && children.Count > 0)
            {
                foreach (object child in children)
                {
                    CTestCase childTc = child as CTestCase;
                    if (childTc != null) 
                    {
                        childTc.Init();

                        foreach (XunitTestCase testCase in childTc.TestCases())
                        {
                            yield return testCase;
                        }

                        continue;
                    }

                    CVariation var = child as CVariation;
                    if (var != null && CModInfo.IsVariationSelected(var.Desc))
                    {
                        foreach (var testCase in var.TestCases())
                        {
                            Func<tagVARIATION_STATUS> test = testCase.Test;
                            testCase.Test = () => {
                                CurVariation = var;
                                return test();
                            };

                            yield return testCase;
                        }
                    }
                }
            }
        }

        public void RunVariation(dlgtTestVariation testmethod, Variation curVar)
        {
            if (!CModInfo.IsVariationSelected(curVar.Desc))
                return;
            const string indent = "\t";
            try
            {
                CurVariation.Attribute = curVar;

                int ret = testmethod();
                if (TEST_PASS == ret)
                {
                    TestModule.PassCount++;
                }
                else if (TEST_FAIL == ret)
                {
                    System.Console.WriteLine(indent + curVar.Desc);
                    System.Console.WriteLine(indent + " FAILED");
                    TestModule.FailCount++;
                }
                else
                {
                    System.Console.WriteLine(indent + curVar.Desc);
                    System.Console.WriteLine(indent + " SKIPPED");
                    TestModule.SkipCount++;
                }
            }
            catch (CTestSkippedException tse)
            {
                System.Console.WriteLine(indent + curVar.Desc);
                System.Console.WriteLine(indent + " SKIPPED" + ", Msg:" + tse.Message);
                TestModule.SkipCount++;
            }
            catch (Exception e)
            {
                System.Console.WriteLine(indent + curVar.Desc);
                System.Console.WriteLine("unexpected exception happened:{0}", e.Message);
                System.Console.WriteLine(e.StackTrace);
                System.Console.WriteLine(indent + " FAILED");
                TestModule.FailCount++;
            }
        }
        //Accessor
        public CTestModule TestModule
        {
            get
            {
                if (Parent is CTestModule)
                    return (CTestModule)Parent;
                else return ((CTestCase)Parent).TestModule;
            }
            set { Parent = value; }
        }

        //Accessors
        protected override CAttrBase CreateAttribute()
        {
            return new TestCase();
        }

        public new virtual TestCase Attribute
        {
            get { return (TestCase)base.Attribute; }
            set { base.Attribute = value; }
        }

        //Helpers
        public void AddVariation(CVariation variation)
        {
            //Delegate
            this.AddChild(variation);
        }

        public CVariation CurVariation
        {
            //Return the current variation:
            //Note: We do this so that within the variation the user can have access to all the 
            //attributes of that particular method.  Unlike the TestModule/TestCase which are objects 
            //and have properties to reference, the variations are function and don't.  Each variation
            //could also have multiple attributes (repeats), so we can't simply use the StackFrame
            //to determine this info...
            get { return _curvariation; }
            set { _curvariation = value; }
        }
        public int GetVariationCount()
        {
            return Children.Count;
        }

        public virtual int ExecuteVariation(int index, object param)
        {
            //Execute the Variation
            return (int)_curvariation.Execute();
        }

        public virtual tagVARIATION_STATUS ExecuteVariation(int index)
        {
            //Track the test case we're in
            CTestModule testmodule = this.TestModule;
            if (testmodule != null)
                testmodule.CurTestCase = this;

            //Track which variation were executing
            _curvariation = (CVariation)Children[index];

            int result = TEST_FAIL;
            if (_curvariation.Skipped || !_curvariation.Implemented)
            {
                //Skipped
                result = TEST_SKIPPED;
            }
            else
            {
                //Execute
                result = ExecuteVariation(index, _curvariation.Param);
            }

            //Before exiting make sure we reset our CurVariation to null, to prevent 
            //incorrect uses of CurVariation within the TestCase, but not actually a running
            //variation.  This will only be valid within a function with a //[Variation] attribute...
            _curvariation = null;
            return (tagVARIATION_STATUS)result;
        }

        public int GetVariationID(int index)
        {
            CVariation variation = (CVariation)Children[index];
            return variation.id;
        }

        public string GetVariationDesc(int index)
        {
            CVariation variation = (CVariation)Children[index];
            return variation.GetDescription();
        }

        protected override void DetermineChildren()
        {
            AddChildren();
            DetermineVariations();
        }

        public virtual void DetermineVariations()
        {
            //Default - no sort
            bool bSort = false;
            //Normally the reflection Type.GetMethods() api returns the methods in order 
            //of how they appear in the code.  But it will change that order depending 
            //upon if there are virtual functions, which are returned first before other
            //non-virtual functions.  Then there are also inherited classes where the 
            //derived classes methods are returned before the inherited class.  So we have
            //added the support of specifying an id=x, as an attribute so you can have
            //then sorted and displayed however your see fit.
            if (bSort)
                Children.Sort(/*Default sort is based upon IComparable of each item*/);
        }

    }
}
