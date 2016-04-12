// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace OLEDB.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////////////
    // VARIATION_STATUS
    //
    ////////////////////////////////////////////////////////////////////////
    public enum tagVARIATION_STATUS
    {
        eVariationStatusFailed = 0,
        eVariationStatusPassed,
        eVariationStatusNotRun,
        eVariationStatusNonExistent,
        eVariationStatusUnknown,
        eVariationStatusTimedOut,
        eVariationStatusConformanceWarning,
        eVariationStatusException,
        eVariationStatusAborted
    }

    ////////////////////////////////////////////////////////////////////////
    // ERRORLEVEL
    //
    ////////////////////////////////////////////////////////////////////////
    public enum tagERRORLEVEL
    {
        HR_STRICT,
        HR_OPTIONAL,
        HR_SUCCEED,
        HR_FAIL,
        HR_WARNING
    }

    ////////////////////////////////////////////////////////////////////////
    // CONSOLEFLAGS
    //
    ////////////////////////////////////////////////////////////////////////
    public enum tagCONSOLEFLAGS
    {
        CONSOLE_RAW = 0x00000000,   //No fixup - Don't use, unless you know the text contains no CR/LF, no Xml reserved tokens, or no other non-representable characters
        CONSOLE_TEXT = 0x00000001,  //Default  - Automatically fixup CR/LF correctly for log files, fixup xml tokens, etc
        CONSOLE_XML = 0x00000002,   //For Xml  - User text is placed into a CDATA section (with no xml fixups)
        CONSOLE_IGNORE = 0x00000004,    //Ignore   - User text is placed into ignore tags (can combine this with console_xml as well)
    }


    ////////////////////////////////////////////////////////////////////////
    // IError
    //
    ////////////////////////////////////////////////////////////////////////
    public interface IError
    {
        // These two methods get/set the ErrorLevel property. This property affects the
        // behavior of the 'Validate' method. The enum values have the following 
        // effects on that method:
        //	HR_STRICT:	The 'hrActual' parameter MUST match the 'hrExpected' parameter,
        //				or else the error count in incremented.
        //  HR_SUCCEED: The 'hrActual' MUST be a success code, or else the error count
        //				is incremented.
        //	HR_FAIL:	The 'hrActual' MUST be an error code, or else the error count
        //				is incremented.
        //	HR_OPTIONAL:The error count will not be incremented, regardless of the
        //				parameters passed in to 'Validate.'
        // 
        tagERRORLEVEL GetErrorLevel();
        void SetErrorLevel(tagERRORLEVEL ErrorLevel);

        // 'GetActualHr' returns the HRESULT recorded from the last call to 'Validate'.
        //
        int GetActualHr();

        // 'Validate' compares the two HRESULT values passed in and, depending on
        // the current ErrorLevel, possibly increment the error count and output
        // an error message to the LTM window. In the cases where validation fails,
        // the pfResult parameter will be returned as FALSE -- otherwise TRUE.
        // When an error message is output to LTM, the file name and line numbers
        // are recorded there as well.
        //
        bool Validate(int hrActual,
                             string bstrFileName,
                             int lLineNo,
                             int hrExpected);

        // 'Compare' will output an error message (similar to that output by 'Validate')
        // if the fWereEqual parameter passed in is FALSE.
        //
        bool Compare(bool fWereEqual,
                             string bstrFileName,
                             int lLineNo);

        // The 'LogxxxxxxxxHr' methods output error or status message to the LTM
        // window. The HRESULT parameters passed in are converted to string
        // messages for this purpose.
        //
        void LogExpectedHr(int hrExpected);
        void LogReceivedHr(int hrReceived,
                             string bstrFileName,
                             int lLineNo);

        // The 'ResetxxxxErrors' methods simply reset the internal error count of 
        // a Module, Case, or Variation (respectively.) 
        //
        void ResetModErrors();
        void ResetCaseErrors();
        void ResetVarErrors();

        void ResetModWarnings();
        void ResetCaseWarnings();
        void ResetVarWarnings();

        // 'GetxxxxErrors' retrieve the current number of errors at the Module,
        // Test Case, or Variation level.
        //
        int GetModErrors();
        int GetCaseErrors();
        int GetVarErrors();

        int GetModWarnings();
        int GetCaseWarnings();
        int GetVarWarnings();

        // 'Increment' will increment the error count of the currently running 
        // Test Module, the currently running Test Case, as well as the currently
        // running Variation.
        //
        void Increment();

        // 'Transmit' is the way a Test Module can send text messages to LTM. These will
        // be displayed in LTM's main window, and can be used to inform the user of
        // any pertinent information at run-time.
        //
        void Transmit(string bstrTextString);

        // 'Initialize'
        //
        void Initialize();
    }

    ////////////////////////////////////////////////////////////////////////
    // ITestConsole
    //
    ////////////////////////////////////////////////////////////////////////
    public interface ITestConsole
    {
        //(Error) Logging routines
        void Log(string bstrActual,
                        string bstrExpected,
                        string bstrSource,
                        string bstrMessage,
                        string bstrDetails,
                        tagCONSOLEFLAGS flags,
                        string bstrFilename,
                        int iline);

        void Write(tagCONSOLEFLAGS flags,
                        string bstrString);
        void WriteLine();
    }

    ////////////////////////////////////////////////////////////////////////
    // IProviderInfo
    //
    ////////////////////////////////////////////////////////////////////////
    public interface IProviderInfo
    {
        // The IProviderInfo interface is just a wrapper around a structure which
        // contains all of the information LTM & Test Modules needs to know about
        // the provider they are running against (if any.)
        //
        // The properties are described as follows:
        //	>Name:			The name the provider gives itself. For Kagera this is MSDASQL.
        //	>FriendlyName:	A special name the user has given this provider configuration.
        //	>InitString:	A string which contains initialization data. The interpretation
        //					of this string is Test Module-specific -- so you simply must
        //					make users of LTM aware of what your particular Test Module
        //					wants to see in this string.
        //	>MachineName:	If you will be testing a remote provider, the user will pass
        //					the machine name the provider is located on to you through
        //					this property.
        //	>CLSID:			This is the CLSID of the provider you are to run against.
        //	>CLSCTX:		This tells you whether the provider will be InProc, Local, or
        //					Remote (values of this property are identical to the values
        //					of the CLSCTX enumeration in wtypes.h)
        string GetName();
        void SetName(string bstrProviderName);
        string GetFriendlyName();
        void SetFriendlyName(string bstrFriendlyName);
        string GetInitString();
        void SetInitString(string bstrInitString);
        string GetMachineName();
        void SetMachineName(string bstrMachineName);
        string GetCLSID();
        void SetCLSID(string bstrCLSID);
        int GetCLSCTX();
        void SetCLSCTX(int ClsCtx);
    }

    ////////////////////////////////////////////////////////////////////////
    // IAliasInfo
    //
    ////////////////////////////////////////////////////////////////////////
    public interface IAliasInfo //: IProviderInfo
    {
        //IProviderInfo (as defined above)
        //TODO: Why do I have to repeat them here.  Inheriting from IProviderInfo succeeds,
        //but the memory layout is incorrect (GetCommandLine actually calls GetName).
        string GetName();
        void SetName(string bstrProviderName);
        string GetFriendlyName();
        void SetFriendlyName(string bstrFriendlyName);
        string GetInitString();
        void SetInitString(string bstrInitString);
        string GetMachineName();
        void SetMachineName(string bstrMachineName);
        string GetCLSID();
        void SetCLSID(string bstrCLSID);
        int GetCLSCTX();
        void SetCLSCTX(int ClsCtx);

        //IAliasInfo
        string GetCommandLine();
    }

    ////////////////////////////////////////////////////////////////////////
    // ITestCases
    //
    ////////////////////////////////////////////////////////////////////////
    public interface ITestCases
    {
        // LTM will use these methods to get information about this test case.
        //
        string GetName();
        string GetDescription();

        // SyncProviderInterface() should cause this TestCase to release its current
        // IProviderInterface (if any) and retrieve a new IProviderInterface 
        // from its owning ITestModule (via the 'GetProviderInterface' call)
        //
        // GetProviderInterface() returns the current IProviderInterface for 
        // this test case.
        //
        void SyncProviderInterface();
        IProviderInfo GetProviderInterface();

        // 'GetOwningITestModule' should retrieve the back-pointer 
        // to this test case's owning ITestModule object.
        //
        ITestModule GetOwningITestModule();

        // LTM will call 'Init' before it runs any variations in this Test Case.
        // Any code that sets up objects for use by the Variations should go here.
        // 
        int Init();

        // LTM will call 'Terminate' after it runs any variations on this
        // object -- regardless of the outcome of those calls (even regardless
        // of whether ITestCases::Init() fails or not.)
        //
        bool Terminate();

        // 'GetVariationCount' should return the number of variations
        // this Test Case contains.
        //
        int GetVariationCount();

        // For these next three, the first parameter is the variation index. 'Index' in
        // this case means the 0-based index into the complete list of variations
        // for this test case (if 'GetVariationCount' returns the value 'n', these
        // functions will always be passed values between 0 and (n - 1) as the first
        // parameter).
        // Do not confuse 'index' with 'Variation ID'. The latter is a non-sequential
        // unique identifier for the variation, which can be any 32-bit number. This
        // exists so that even when variations are added or removed during test
        // development, the ID is ALWAYS the same for the same variation (though 
        // the Index will surely change.)
        //
        tagVARIATION_STATUS ExecuteVariation(int lIndex);
        int GetVariationID(int lIndex);
        string GetVariationDesc(int lIndex);
    }

    ////////////////////////////////////////////////////////////////////////
    // ITestModule
    //
    ////////////////////////////////////////////////////////////////////////
    public interface ITestModule
    {
        string GetName();
        string GetDescription();
        string GetOwnerName();
        string GetCLSID();
        int GetVersion();

        // LTM will call 'SetProviderInterface' after the ITestModule object is instantiated 
        // (if appropriate).
        // 'GetProviderInterface' is added for the implementor's convenience, but is not
        // called by LTM.
        //
        void SetProviderInterface(IProviderInfo pProvInfo);
        IProviderInfo GetProviderInterface();

        // LTM will call 'SetErrorInterface' after the ITestModule object is instantiated.
        // 'GetErrorInterface' is added for the implementor's convenience, but is not
        // called by LTM.
        //
        void SetErrorInterface(IError pIError);
        IError GetErrorInterface();

        // 'Init' is called at the beginning of each test run. Any global
        // data that all Test Cases in this module will use should be setup
        // here.
        //
        int Init();

        // 'Terminate' is called at the end of each test run, regardless of the
        // outcome of the tests (even regardless of whether ITestModule::Init()
        // succeeds or fails.)
        //
        bool Terminate();

        // 'GetCaseCount' returns the number of test cases this test module
        // contains. This value must not be zero.
        //
        int GetCaseCount();

        // 'GetCase' should create an ITestCases instance for LTM to use.
        // The first parameter is the 0-based index of the test case. If
        // 'GetCaseCount' returns 'n' cases, this parameter will always
        // be between 0 and (n - 1), inclusive.
        //
        ITestCases GetCase(int lIndex);
    }
}
