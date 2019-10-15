// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace DPStressHarness
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class TestVariationAttribute : Attribute
    {
        private string _variationName;
        private object _variationValue;

        public TestVariationAttribute(string variationName, object variationValue)
        {
            _variationName = variationName;
            _variationValue = variationValue;
        }

        public string VariationName
        {
            get { return _variationName; }
            set { _variationName = value; }
        }

        public object VariationValue
        {
            get { return _variationValue; }
            set { _variationValue = value; }
        }
    }
}
