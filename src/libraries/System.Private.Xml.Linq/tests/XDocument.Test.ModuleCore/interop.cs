// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;

namespace Microsoft.Test.ModuleCore
{
    public class LtmContext
    {
    }


    ////////////////////////////////////////////////////////////////////////
    // TestResult
    //
    ////////////////////////////////////////////////////////////////////////
    public enum TestResult
    {
        Failed = 0,
        Passed,
        Skipped,
        NonExistent,
        Unknown,
        Timeout,
        Warning,
        Exception,
        Aborted,
        Assert
    }

    ////////////////////////////////////////////////////////////////////////
    // TestPropertyFlags
    //
    ////////////////////////////////////////////////////////////////////////
    public enum TestPropertyFlags
    {
        Read = 0x00000000,
        Write = 0x00000001,
        Required = 0x00000010,
        Inheritance = 0x00000100,
        MultipleValues = 0x00001000,
        DefaultValue = 0x00002000,
        Visible = 0x00010000,
    }

    ////////////////////////////////////////////////////////////////////////
    // TestType
    //
    ////////////////////////////////////////////////////////////////////////
    public enum TestType
    {
        TestSuite = 0,
        TestModule = 1,
        TestCase = 2,
        TestVariation = 3,
    }

    ////////////////////////////////////////////////////////////////////////
    // TestFlags
    //
    ////////////////////////////////////////////////////////////////////////
    public enum TestFlags
    {
    }

    ////////////////////////////////////////////////////////////////////////
    // TestMethod
    //
    ////////////////////////////////////////////////////////////////////////
    public enum TestMethod
    {
        Init = 0,
        Terminate = 1,
        Execute = 2,
    }

    ////////////////////////////////////////////////////////////////////////
    // TestLogFlags
    //
    ////////////////////////////////////////////////////////////////////////
    public enum TestLogFlags
    {
        Raw = 0x00000000,   //No fixup - Don't use, unless you know the text contains no CR/LF, no Xml reserverd tokens, or no other non-respresentable characters
        Text = 0x00000001,  //Default  - Automatically fixup CR/LF correctly for log files, fixup xml tokens, etc
        Xml = 0x00000002,   //For Xml  - User text is placed into a CDATA section (with no xml fixups)
        Ignore = 0x00000004,    //Ignore   - User text is placed into ignore tags (can combine this with console_xml as well)
        Trace = 0x00000010, //Trace    - User text is not displayed unless epxlicitly enabled
    }

    ////////////////////////////////////////////////////////////////////////
    // ITestItem
    //
    ////////////////////////////////////////////////////////////////////////
    public interface ITestItem
    {
        // Simple Meta-data about the item
        int Id { get; }
        string Guid { get; }
        string Name { get; }
        string Desc { get; }
        string Owner { get; }
        string Version { get; }
        int Priority { get; }
        TestType Type { get; }
        TestFlags Flags { get; }

        // Extensible Meta-data about the item
        ITestProperties Metadata { get; }

        // Children (testcases, variations, etc)
        ITestItems Children { get; }

        // Execution
        //		Control Flow:	Init->Execute->(recurse into children)->Terminate
        TestResult Init();
        TestResult Execute();
        TestResult Terminate();
    }

    ////////////////////////////////////////////////////////////////////////
    // ITestItems
    //
    ////////////////////////////////////////////////////////////////////////
    public interface ITestItems
    {
        // Enumeration
        int Count { get; }
        ITestItem GetItem(int index);
    }

    ////////////////////////////////////////////////////////////////////////
    // ITestProperty
    //
    ////////////////////////////////////////////////////////////////////////
    public interface ITestProperty
    {
        // Simple Meta-data about the property
        string Name { get; }
        string Desc { get; }
        TestPropertyFlags Flags { get; set; }
        object Value { get; }
        void set_Value(ref object value);

        // Extensible Meta-data about the property
        ITestProperties Metadata { get; }

        // Children - Heiarchical properties
        ITestProperties Children { get; }
    }

    ////////////////////////////////////////////////////////////////////////
    // ITestProperties
    //
    ////////////////////////////////////////////////////////////////////////
    public interface ITestProperties
    {
        // Enumeration
        int Count { get; }
        ITestProperty GetItem(int index);

        //Access methods
        ITestProperty Get(string name);
        ITestProperty Add(string name);
        void Remove(string name);
        void Clear();
    }

    ////////////////////////////////////////////////////////////////////////
    // ITestLoader
    //
    ////////////////////////////////////////////////////////////////////////
    public interface ITestLoader
    {
        //Simple Metadata
        string Guid { get; }
        string Name { get; }
        string Desc { get; }

        // Extensible Meta-data about the item
        ITestProperties Metadata { get; }

        //Execution 
        void Init();
        ITestItem CreateTest( string assembly, string test);
        void Terminate();

        //Enumeration
        string[] Enumerate( string assembly);

        //Input (get/set)
        ITestProperties Properties {  set; get; }

        //Logging (get/set)
        ITestLog Log {  set; get; }
    }

    ////////////////////////////////////////////////////////////////////////
    // ITestLog
    //
    ////////////////////////////////////////////////////////////////////////
    public interface ITestLog
    {
        string Name { get; }
        string Desc { get; }

        // Extensible Meta-data about the item
        ITestProperties Metadata { get; }

        //Construction
        void Init();
        void Terminate();

        //Console
        void Write(TestLogFlags flags,
                                                string text);
        void WriteLine(TestLogFlags flags,
                                                string text);

        //Scoping
        void Enter(ITestItem item, TestMethod method);
        void Leave(ITestItem item, TestMethod method, TestResult result);

        //(Error) Logging routines
        void Error(TestResult result,
            TestLogFlags flags,
            string actual,
            string expected,
            string source,
            string message,
            string stack,
            string filename,
            int lineno);
    }
}
