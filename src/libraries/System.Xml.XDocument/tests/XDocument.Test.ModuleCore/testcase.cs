// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // TestCases
    //
    ////////////////////////////////////////////////////////////////
    public abstract class TestCase : TestItem
    {
        //Data

        //Constructor
        public TestCase()
            : this(null, null)
        {
        }

        public TestCase(string name, string desc)
            : base(name, desc, TestType.TestCase)
        {
        }

        //Accessors
        protected override TestAttribute CreateAttribute()
        {
            return new TestCaseAttribute();
        }

        protected virtual TestVariation CreateVariation(TestFunc func, string desc)
        {
            //Override if you have a specific variation class
            return new TestVariation(func, desc);
        }

        public new virtual TestCaseAttribute Attribute
        {
            get { return (TestCaseAttribute)base.Attribute; }
            set { base.Attribute = value; }
        }

        //Helpers
        protected override void DetermineChildren()
        {
            //Delegate (add any nested testcases)
            base.DetermineChildren();

            //Sort
            //Default sort is based upon IComparable of each item	
            Children.Sort();
        }

        public override TestResult Execute()
        {
            base.Execute();
            TestModule module = GetModule();
            if (module == null)
            {
                throw new Exception("No parent module");
            }
            TestItems children = Children;
            if (children != null && children.Count > 0)
            {
                // this is not a leaf node, just invoke all the children's execute
                foreach (object child in children)
                {
                    if (child is TestCase)
                    {
                        TestCase t = child as TestCase;
                        t.Init();
                        t.Execute();
                        continue;
                    }
                    TestVariation var = child as TestVariation;
                    if (var != null)
                    {
                        const string indent = "\t";
                        try
                        {
                            CurVariation = var;
                            TestResult ret = var.Execute();
                            if (TestResult.Passed == ret)
                            {
                                module.PassCount++;
                            }
                            else if (TestResult.Failed == ret)
                            {
                                System.Console.WriteLine(indent + var.Desc);
                                System.Console.WriteLine(indent + " FAILED");
                                module.FailCount++;
                            }
                            else
                            {
                                System.Console.WriteLine(indent + var.Desc);
                                System.Console.WriteLine(indent + " SKIPPED");
                                module.SkipCount++;
                            }
                        }
                        catch (TestSkippedException tse)
                        {
                            if (!string.IsNullOrWhiteSpace(var.Desc))
                            {
                                System.Console.WriteLine(indent + var.Desc);
                            }
                            if (!string.IsNullOrWhiteSpace(tse.Message))
                            {
                                System.Console.WriteLine(indent + " SKIPPED" + ", Msg:" + tse.Message);
                            }
                            module.SkipCount++;
                        }
                        catch (Exception e)
                        {
                            System.Console.WriteLine(indent + var.Desc);
                            System.Console.WriteLine(e);
                            System.Console.WriteLine(indent + " FAILED");
                            module.FailCount++;
                        }
                    }
                }
            }
            return TestResult.Passed;
        }

        public TestVariation Variation
        {
            //Currently executing child
            get { return base.CurrentChild as TestVariation; }
        }
        public TestVariation CurVariation
        {
            //Currently executing child
            get { return base.CurrentChild as TestVariation; }
            set { base.CurrentChild = value; }
        }
    }
}
