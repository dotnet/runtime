// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;      //BindingFlags

namespace Microsoft.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // Delegate
    //
    ////////////////////////////////////////////////////////////////
    public delegate void TestFunc();

    ////////////////////////////////////////////////////////////////
    // TestVariation
    //
    ////////////////////////////////////////////////////////////////
    public class TestVariation : TestItem
    {
        //Data
        protected TestFunc pfunc = null;

        //Constructor
        public TestVariation()
            : base(null, null, TestType.TestVariation)
        {
        }

        public TestVariation(TestFunc func)
            : this(func, null)
        {
        }

        public TestVariation(TestFunc func, string desc)
            : base(null, desc, TestType.TestVariation)
        {
            pfunc = func;
        }

        // MemberwiseClone Shallow Copy
        public TestVariation Clone()
        {
            return (TestVariation)MemberwiseClone();
        }

        protected override void DetermineChildren()
        {
            //No children
        }

        protected override TestAttribute CreateAttribute()
        {
            return new VariationAttribute();
        }

        public override TestResult Execute()
        {
            if (Parent != null)
                Parent.CurrentChild = this;

            //Delegate function
            //Note: Override, if you have some other approach to executing your code (reflection, xml file, etc)
            pfunc();
            return TestResult.Passed;
        }

        public override int CompareTo(object o)
        {
            //Default comparison, Id based.
            return this.Id.CompareTo(((TestItem)o).Id);
        }
    }
}
