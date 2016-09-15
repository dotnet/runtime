// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public static class TestInfo
    {
        public static string ControlFile = string.Empty;
        public static string Path = string.Empty;
    }

    ///////////////////////////////////////////////////////////////////////////////////
    // CXsltScenarios (test module)
    //
    ///////////////////////////////////////////////////////////////////////////////////
    //[TestModule(Desc = "This test module tests different XSLT Scenarios listed in XML Scenarios doc")]
    public class CXsltScenarios : CTestModule
    {
        //Contstructor
        public CXsltScenarios()
        {
        }

        public override void DetermineTestCases()
        {
            base.DetermineTestCases();
        }

        public String InitStringValue(String str)
        {
            throw new NotImplementedException();
            //object obj = CModInfo.GetOption(str);

            //if (obj == null)
            //{
            //    return String.Empty;
            //}
            //return obj.ToString();
        }
    }
}