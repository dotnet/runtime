// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // TestItem
    //
    ////////////////////////////////////////////////////////////////
    public abstract class TestItem : ITestItem, IComparable
    {
        //Data
        protected TestItem pparent;
        protected TestItems pchildren;
        protected TestAttribute pattribute;
        protected TestProperties pmetadata;
        protected TestType ptesttype;
        protected TestFlags ptestflags;
        protected TestItem pcurrentchild;

        //Constructor
        public TestItem(string name, string desc, TestType testtype)
        {
            if (name != null)
                this.Name = name;
            if (this.Name == null) this.Name = "No Name Provided";
            if (desc != null)
                this.Desc = desc;
            ptesttype = testtype;
        }

        //Accessors
        public virtual TestAttribute Attribute
        {
            get
            {
                if (pattribute == null)
                    pattribute = CreateAttribute();
                return pattribute;
            }
            set { pattribute = value; }
        }

        protected abstract TestAttribute CreateAttribute();

        //Note: These are just a mere convenience to access the attribute values
        //for this particular object.  Also note that for non-attribute based
        //scenarios (dynamic), the attribute class will be created just to hold
        //the values
        public object Param
        {
            get { return Attribute.Param; }
            set { Attribute.Param = value; }
        }

        public object[] Params
        {
            get { return Attribute.Params; }
            set { Attribute.Params = value; }
        }

        public int Id
        {
            get { return Attribute.Id; }
            set { Attribute.Id = value; }
        }

        public int Order
        {
            get { return Attribute.Id; }
            set { Attribute.Id = value; }
        }

        public int Priority
        {
            get { return Attribute.Priority; }
            set { Attribute.Priority = value; }
        }

        public string Guid
        {
            get { return Attribute.Guid; }
            set { Attribute.Guid = value; }
        }

        public TestType Type
        {
            get { return ptesttype; }
            set { ptesttype = value; }
        }

        public TestFlags Flags
        {
            get { return ptestflags; }
            set { ptestflags = value; }
        }

        public string Name
        {
            get { return Attribute.Name; }
            set { Attribute.Name = value; }
        }

        public string Desc
        {
            get { return Attribute.Desc; }
            set { Attribute.Desc = value; }
        }

        public string Purpose
        {
            get { return Attribute.Purpose; }
            set { Attribute.Purpose = value; }
        }

        public bool Implemented
        {
            get { return Attribute.Implemented; }
            set { Attribute.Implemented = value; }
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

        public string Version
        {
            get { return Attribute.Version; }
            set { Attribute.Version = value; }
        }

        public bool Skipped
        {
            get { return Attribute.Skipped; }
            set { Attribute.Skipped = value; }
        }

        public bool Error
        {
            get { return Attribute.Error; }
            set { Attribute.Error = value; }
        }

        public bool Manual
        {
            get { return Attribute.Manual; }
            set { Manual = value; }
        }

        public SecurityFlags Security
        {
            get { return Attribute.Security; }
            set { Attribute.Security = value; }
        }

        public bool Inheritance
        {
            get { return Attribute.Inheritance; }
            set { Attribute.Inheritance = value; }
        }

        public string FilterCriteria
        {
            get { return Attribute.FilterCriteria; }
            set { Attribute.FilterCriteria = value; }
        }

        public string Language
        {
            get { return Attribute.Language; }
            set { Attribute.Language = value; }
        }

        public string[] Languages
        {
            get { return Attribute.Languages; }
            set { Attribute.Languages = value; }
        }

        public string Xml
        {
            get { return Attribute.Xml; }
            set { Attribute.Xml = value; }
        }

        public bool Stress
        {
            get { return Attribute.Stress; }
            set { Attribute.Stress = value; }
        }

        public int Threads
        {
            get { return Attribute.Threads; }
            set { Attribute.Threads = value; }
        }

        public int Repeat
        {
            get { return Attribute.Repeat; }
            set { Attribute.Repeat = value; }
        }

        public int Timeout
        {
            get { return Attribute.Timeout; }
            set { Attribute.Timeout = value; }
        }

        public string Filter
        {
            get { return Attribute.Filter; }
            set { Attribute.Filter = value; }
        }

        public TestItem Parent
        {
            get { return pparent; }
            set { pparent = value; }
        }

        public TestModule GetModule()
        {
            TestItem it = this;
            while (true)
            {
                TestModule module = it as TestModule;
                if (module != null)
                {
                    return module;
                }

                if (it == it.Parent)
                {
                    throw new Exception("TestItem is its own parent");
                }
                it = it.Parent;
            }
        }

        ITestItems ITestItem.Children
        {
            get { return this.Children; }
        }

        public TestItems Children
        {
            get
            {
                //Deferred Creation of the children.
                if (pchildren == null)
                    pchildren = new TestItems();
                return pchildren;
            }
            set { pchildren = value; }
        }

        public TestItem CurrentChild
        {
            //Currently executing child
            //Note: We do this so that within global functions can know which 
            //testcase/variation were in, without having to pass this state from execute arround
            get { return pcurrentchild; }
            set { pcurrentchild = value; }
        }

        ITestProperties ITestItem.Metadata
        {
            get { return this.Metadata; }
        }

        public TestProperties Metadata
        {
            get
            {
                if (pmetadata == null)
                {
                    pmetadata = new TestProperties();
                }

                return pmetadata;
            }
        }

        protected virtual void UpdateAttributes()
        {
        }

        protected virtual void DetermineChildren()
        {
            AddChildren();
            Children.Sort();
        }

        public virtual void AddChildren()
        {
        }

        public void AddChild(TestItem child)
        {
            //Setup the parent
            child.Parent = this;

            //Inheritance of attributes
            child.Attribute.Parent = this.Attribute;

            //Adjust the Id (if not set)
            if (child.Id <= 0)
                child.Id = Children.Count + 1;

            //Only add implemented items
            //Note: we still increment id counts, to save the 'spot' once they are implemented
            if (child.Implemented)
            {
                //Determine any children of this node (before adding it)
                //Note: We don't call 'determinechildren' from within the (testcase) constructor
                //since none of the attributes/properties are setup until now.  So as soon as we
                //setup that information then we call determinechildren which when implemented
                //for dynamic tests can now look at those properties (otherwise they wouldn't be setup
                //until after the constructor returns).
                child.DetermineChildren();

                //Add it to our list...
                Children.Add(child);
            }
        }

        protected TestItem FindChild(Type type)
        {
            //Compare
            if (type == this.GetType())
                return this;

            //Otherwise recursive
            foreach (TestItem item in this.Children)
            {
                TestItem found = item.FindChild(type);
                if (found != null)
                    return found;
            }

            return null;
        }

        public virtual void Init()
        {
            //Note: This version is the only override-able Init, since the other one 
            //automatically handles the exception you might throw from this function
            //Note: If you override this function, (as with most overrides) make sure you call the base.
        }

        TestResult ITestItem.Init()
        {
            //Enter
            OnEnter(TestMethod.Init);

            TestResult result = TestResult.Passed;
            if (Parent != null)
                Parent.CurrentChild = this;

            //Obtain the error object (to prime it)
            try
            {
                if (ptesttype == TestType.TestModule)
                {
                    //Clear any previous existing info (in the static class).
                    //Note: We actually have to clear these "statics" since they are not cleaned up
                    //until the process exits.  Which means that if you run again, in an apartment model
                    //thread it will try to release these when setting them which is not allowed to 
                    //call a method on an object created in another apartment
                    if (TestLog.Internal == null)
                    {
                        TestLog.Internal = new LtmContext() as ITestLog;
                        TestInput.Properties = new TestProps(new LtmContext() as ITestProperties);
                    }
                }
            }
            catch (Exception e)
            {
                result = HandleException(e);
            }

            //NOTE: Since exceptions are a way-of-life in COOL, and the fact that they are just
            //caught by the runtime before passed back to LTM, we lose the entire stack and just 
            //an unknown error code is returned.

            //To help solve this we will wrap the call in a try catch and actually
            //log the exception to the LTM output window... 
            try
            {
                //Skipped
                if (this.Skipped || !this.Implemented)
                    result = TestResult.Skipped;
                else
                    this.Init();
            }
            catch (Exception e)
            {
                result = HandleException(e);
            }

            //Leave
            OnLeave(TestMethod.Init);
            return result;
        }

        public virtual TestResult Execute()
        {
            TestResult result = TestResult.Passed;
            //Skipped
            if (this.Skipped || !this.Implemented)
                result = TestResult.Skipped;

            return result;
            //Note: This version is the only override-able version, since the other one 
            //automatically handles the exception you might throw from this function
            //Note: If you override this function, (as with most overrides) make sure you call the base.
        }

        public virtual void Terminate()
        {
            //Note: This version is the only override-able Terminate, since the other one 
            //automatically handles the exception you might throw from this function
            //Note: If you override this function, (as with most overrides) make sure you call the base.
        }

        TestResult ITestItem.Terminate()
        {
            //Enter
            OnEnter(TestMethod.Terminate);
            TestResult result = TestResult.Passed;

            try
            {
                //Skipped
                if (this.Skipped || !this.Implemented)
                    result = TestResult.Skipped;
                else
                    this.Terminate();

                //Before exiting make sure we reset our CurChild to null, to prevent incorrect uses 
                if (Parent != null)
                    Parent.CurrentChild = null;
            }
            catch (Exception e)
            {
                HandleException(e);
            }

            try
            {
                //Clear any previous existing info (in the static class).
                if (ptesttype == TestType.TestModule)
                {
                    //Note: We actually have to clear these "statics" since they are not cleaned up
                    //until the process exits.  Which means that if you run again, in an apartment model
                    //thread it will try to release these when setting them which is not allowed to 
                    //call a method on an object created in another apartment
                    TestInput.Dispose();
                    TestLog.Dispose();
                }
            }
            catch (Exception e)
            {
                result = HandleException(e);
            }

            //This is also a good point to hint to the GC to free up any unused memory
            //at the end of each TestCase and the end of each module.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            //Leave
            OnLeave(TestMethod.Terminate);
            return result;
        }

        protected virtual void OnEnter(TestMethod method)
        {
        }

        protected virtual void OnLeave(TestMethod method)
        {
        }

        protected virtual TestResult HandleException(Exception e)
        {
            //Note: override this if your product has specilized exceptions (ie: nesting or collections)
            //that you need to recurse over of print out differently
            return TestLog.HandleException(e);
        }

        public virtual int CompareTo(object o)
        {
            //Default comparison, name based.
            return this.Name.CompareTo(((TestItem)o).Name);
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestItems
    //
    ////////////////////////////////////////////////////////////////
    public class TestItems : List<TestItem>, ITestItems
    {
        //Data

        //Constructor
        public TestItems()
        {
        }

        //ITestItems
        int ITestItems.Count
        {
            get { return this.Count; }
        }

        ITestItem ITestItems.GetItem(int index)
        {
            return (ITestItem)this[index];
        }
    }
}
