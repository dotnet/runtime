// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace OLEDB.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // CTestBase
    //
    ////////////////////////////////////////////////////////////////
    public abstract class CTestBase : IComparable
    {
        //Defines
        public const int TEST_FAIL = (int)tagVARIATION_STATUS.eVariationStatusFailed;
        public const int TEST_PASS = (int)tagVARIATION_STATUS.eVariationStatusPassed;
        public const int TEST_SKIPPED = (int)tagVARIATION_STATUS.eVariationStatusNotRun;
        public const int TEST_NOTFOUND = (int)tagVARIATION_STATUS.eVariationStatusNonExistent;
        public const int TEST_UNKNOWN = (int)tagVARIATION_STATUS.eVariationStatusUnknown;
        public const int TEST_TIMEOUT = (int)tagVARIATION_STATUS.eVariationStatusTimedOut;
        public const int TEST_WARNING = (int)tagVARIATION_STATUS.eVariationStatusConformanceWarning;
        public const int TEST_EXCEPTION = (int)tagVARIATION_STATUS.eVariationStatusException;
        public const int TEST_ABORTED = (int)tagVARIATION_STATUS.eVariationStatusAborted;

        //Data
        private CTestBase _parent;
        private List<object> _children;
        private CAttrBase _attribute;

        //Constructor
        public CTestBase(string desc)
            : this(null, desc)
        {
        }

        //Constructor
        public CTestBase(string name, string desc)
        {
            //Now set the passed in names.
            if (name != null)
                this.Name = name;
            if (this.Name == null) this.Name = "No Name Provided";
            if (desc != null)
                this.Desc = desc;
        }

        //Accessors
        public virtual CAttrBase Attribute
        {
            get
            {
                if (_attribute == null)
                    _attribute = CreateAttribute();
                return _attribute;
            }
            set { _attribute = value; }
        }

        protected abstract CAttrBase CreateAttribute();

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

        public int id
        {
            get { return Attribute.id; }
            set { Attribute.id = value; }
        }

        public int Pri
        {
            get { return Attribute.Pri; }
            set { Attribute.Pri = value; }
        }

        public int Priority //Alias for Pri
        {
            get { return Attribute.Pri; }
            set { Attribute.Pri = value; }
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

        public bool Implemented
        {
            get { return Attribute.Implemented; }
            set { Attribute.Implemented = value; }
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

        protected CTestBase Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public string Filter
        {
            get { return Attribute.Filter; }
            set { Attribute.Filter = value; }
        }

        public List<object> Children
        {
            get
            {
                //Deferred Creation of the children.
                if (_children == null)
                    _children = new List<object>();
                return _children;
            }
            set { _children = value; }
        }

        protected virtual void UpdateAttributes()
        {
        }

        protected virtual void DetermineChildren()
        {
            //Override this method if this node has children
        }

        protected void AddChild(CTestBase child)
        {
            //Setup the parent
            child.Parent = this;

            //Inheritance of attributes
            child.Attribute.Parent = this.Attribute;

            //Adjust the Id (if not set)
            if (child.id <= 0)
                child.id = Children.Count + 1;

            //Only add implemented items
            //Note: we still increment id counts, to save the 'spot' once they are implemented
            if (child.Implemented || CModInfo.IncludeNotImplemented)
            {
                //Determine any children of this node (before adding it)
                //Note: We don't call 'determinechildren' from within the (test case) constructor
                //since none of the attributes/properties are setup until now.  So as soon as we
                //setup that information then we call determinechildren which when implemented
                //for dynamic tests can now look at those properties (otherwise they wouldn't be setup
                //until after the constructor returns).
                child.DetermineChildren();

                //Add it to our list...
                Children.Add(child);
            }
        }

        public virtual void AddChildren()
        {
        }

        //ITestCase implementation
        public string GetName()
        {
            return Name;
        }

        public string GetDescription()
        {
            return Desc;
        }

        public virtual int Init(object o)
        {
            //Note: This version is the only override-able Init, since the other one 
            //automatically handles the exception you might throw from this function
            //Note: If you override this function, (as with most overrides) make sure you call the base.
            return TEST_PASS;
        }

        public int Init()
        {
            try
            {
                //Skipped
                if (this.Skipped || !this.Implemented)
                    return TEST_SKIPPED;

                return Init(Param);
            }
            catch (Exception e)
            {
                return HandleException(e);
            }

        }

        public virtual int Terminate(object o)
        {
            //Note: This version is the only override-able Terminate, since the other one 
            //automatically handles the exception you might throw from this function
            //Note: If you override this function, (as with most overrides) make sure you call the base.
            return TEST_PASS;
        }

        public bool Terminate()
        {
            bool bResult = false;
            try
            {
                //Skipped
                if (this.Skipped || !this.Implemented)
                    return true;

                if (Terminate(Param) == TEST_PASS)
                    bResult = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                HandleException(e);
            }

            try
            {
                //Clear any previous existing info (in the static class).
                if (this is CTestModule)
                {
                    //Note: We actually have to clear these "statics" since they are not cleaned up
                    //until the process exits.  Which means that if you run again, in an apartment model
                    //thread it will try to release these when setting them which is not allowed to 
                    //call a method on an object created in another apartment
                    CModInfo.Dispose();
                }
            }
            catch (Exception e)
            {
                HandleException(e);
            }

            //This is also a good point to hint to the GC to free up any unused memory
            //at the end of each TestCase and the end of each module.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return bResult;
        }

        public virtual tagVARIATION_STATUS Execute()
        {
            return tagVARIATION_STATUS.eVariationStatusPassed;
        }

        public abstract IEnumerable<XunitTestCase> TestCases();

        public int CompareTo(object o)
        {
            return this.id.CompareTo(((CTestBase)o).id);
        }

        public static int HandleException(Exception e)
        {
            //TargetInvocationException is almost always the outer 
            //since we call the variation through late binding
            if (e.InnerException != null)
                e = e.InnerException;

            int eResult = TEST_FAIL;
            object actual = e.GetType();
            object expected = null;
            string message = e.Message;
            tagERRORLEVEL eErrorLevel = tagERRORLEVEL.HR_FAIL;

            if (e is CTestException)
            {
                CTestException eTest = (CTestException)e;

                //Setup more meaningful info
                actual = eTest.Actual;
                expected = eTest.Expected;
                eResult = eTest.Result;
                switch (eResult)
                {
                    case TEST_PASS:
                    case TEST_SKIPPED:
                        return eResult; //were done

                    case TEST_WARNING:
                        eErrorLevel = tagERRORLEVEL.HR_WARNING;
                        break;
                };
            }

            //Note: We don't use Exception.ToString as the details for the log since that also includes
            //the message text (again).  Normally this isn't a problem but if you throw a good message
            //(multiple lines) it show up twice and is confusing.  So we will strictly use the 
            //StackTrace as the details and roll our own message (which also include inner exception
            //messages).
            Exception inner = e.InnerException;
            CError.Log(actual, expected, e.Source, message, e.StackTrace, eErrorLevel);

            while (inner != null)
            {
                CError.WriteLine("\n INNER EXCEPTION :");
                CError.Log(actual, expected, inner.Source, inner.Message, inner.StackTrace, eErrorLevel);
                inner = inner.InnerException;
            }

            return eResult;
        }
    }
}
