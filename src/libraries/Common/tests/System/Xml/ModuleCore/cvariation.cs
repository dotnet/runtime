// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OLEDB.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // CVariation
    //
    ////////////////////////////////////////////////////////////////
    public delegate int TestFunc();
    public class CVariation : CTestBase
    {
        //Data
        private TestFunc _pfunc = null;
        //Constructor
        public CVariation(CTestCase testcase)
            : this(testcase, null)
        {
        }

        public CVariation(CTestCase testcase, string desc)
            : this(testcase, desc, "Variation_" + (testcase.GetVariationCount() + 1))
        {
            //Delegate
            //By default - if you don't specify the function name to run for this variation
            //it creates Variation_X, where X is the next variation for this test case...
        }

        public CVariation(CTestCase testcase, string desc, string function)
            : base(function, desc)
        {
            //Note: The parent automatically gets setup on AddVariation so we don't 
            //really need to pass in the test case, left here for backward compatibility 
            //of inherited drivers.
        }

        public CVariation(CTestCase testcase, string desc, TestFunc function)
            : base(null, desc)
        {
            //Note: The parent automatically gets setup on AddVariation so we don't 
            //really need to pass in the test case, left here for backward compatibility 
            //of inherited drivers.
            _pfunc = function;
        }
        public CVariation(TestFunc func)
            : base(null, null)
        {
            _pfunc = func;
        }

        public CTestCase TestCase
        {
            get { return (CTestCase)Parent; }
            set { Parent = value; }
        }

        protected override CAttrBase CreateAttribute()
        {
            return new Variation();
        }

        public override tagVARIATION_STATUS Execute()
        {
            if (TestCase != null)
            {
                TestCase.CurVariation = this;
            }

            if (_pfunc != null)
            {
                int ret = _pfunc();
                return (tagVARIATION_STATUS)ret;
            }

            throw new Exception("test method is null in CVariation");
        }

        public override IEnumerable<XunitTestCase> TestCases()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(Name))
                sb.Append(Name);

            sb.Append(Desc);

            yield return new XunitTestCase(sb.ToString(), Execute);
        }
    }
}
