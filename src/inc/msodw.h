// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

/****************************************************************************
    This files contains the handshake structure with which apps will launch
    Watson.
****************************************************************************/

#ifndef MSODW_H
#define MSODW_H
#pragma pack(push, msodw_h)
#pragma pack(4)

#define DW_TIMEOUT_VALUE    20000
#define DW_MUTEX_TIMEOUT    DW_TIMEOUT_VALUE / 2
#define DW_NOTIFY_TIMEOUT   120000 // 2 minutes

#define DW_CURRENT_VERSION  0x00020000

#define DW_MAX_ASSERT_CCH   1024
#define DW_MAX_PATH         260
#define DW_APPNAME_LENGTH   56
#define DW_MAX_ERROR_CWC    260 // must be at least max_path
#define DW_MAX_REGSUBPATH   200
#define DW_MAX_CALLSTACK    16
#define DW_MAX_EVENTSOURCE  DW_MAX_PATH
#define DW_MAX_PIDREGKEY    DW_MAX_PATH
#define DW_MAX_BUCKETPARAM_CWC  255
#define DW_MAX_USERDOCS_CWC 1024

// return values for DW process
#define DW_RETVAL_SUCCESS   0
#define DW_RETVAL_FAILURE   1
#define DW_RETVAL_DEBUG     16

#define DW_ALLMODULES              L"*\0"
#define DW_NOTAG 0

// this is added to the command line of the restarted app if fDweTagCommandLine is set
#define DW_CMDLINE_TAG "Watson=1"

// The following are the fields that can be specified in a manifest file to
// launch DW in a file based reporting mode.
// The following are required UI fields.
#define DW_MANIFEST_GENERAL_APPNAME  L"General_AppName="
#define DW_MANIFEST_MAIN_INTRO_BOLD  L"Main_Intro_Bold="
#define DW_MANIFEST_QUEUED_EVENTDESCRIPTION L"Queued_EventDescription=" // this will only be shown if the report is queued

// The following are required reporting fields.
#define DW_MANIFEST_LCID             L"UI LCID="
#define DW_MANIFEST_VERSION          L"Version=" // set this to DW_CURRENT_VERSION defined in this file

// There are two ways to specify your bucket parameters.
// The first is to fill in as many of these as you need,
// and let DW construct URLs and UNC paths for you.
#define DW_MANIFEST_EVENTTYPE   L"EventType="
#define DW_MANIFEST_P1          L"P1="
#define DW_MANIFEST_P2          L"P2="
#define DW_MANIFEST_P3          L"P3="
#define DW_MANIFEST_P4          L"P4="
#define DW_MANIFEST_P5          L"P5="
#define DW_MANIFEST_P6          L"P6="
#define DW_MANIFEST_P7          L"P7="
#define DW_MANIFEST_P8          L"P8="
#define DW_MANIFEST_P9          L"P9="
#define DW_MANIFEST_P10         L"P10="

// Alternatively, you can spell it all out for us.
#define DW_MANIFEST_URL1         L"Stage1URL="
#define DW_MANIFEST_URL2         L"Stage2URL="
#define DW_MANIFEST_ERRORSUBPATH L"ErrorSubPath="

// The following are optional; DW has default behavior for all of these.
// These are UI fields (see UserInterfaceBlock for documentation if not listed below)
#define DW_MANIFEST_GENERAL_REPORTEE    L"General_Reportee="

#define DW_MANIFEST_MAIN_CAPTION        L"Main_Caption="
#define DW_MANIFEST_MAIN_ICONFILE       L"Main_IconFile=" // otherwise, no icon
#define DW_MANIFEST_MAIN_INTRO_REG      L"Main_Intro_Reg=" // otherwise DAL collapses space
#define DW_MANIFEST_MAIN_CHECKBOX       L"Main_CheckBox="
#define DW_MANIFEST_MAIN_PLEA_BOLD      L"Main_Plea_Bold="
#define DW_MANIFEST_MAIN_PLEA_REG       L"Main_Plea_Reg=" // otherwise DAL collapses space
#define DW_MANIFEST_MAIN_DETAILSLINK    L"Main_DetailsLink="
#define DW_MANIFEST_MAIN_REPORTBTN      L"Main_ReportBtn="
#define DW_MANIFEST_MAIN_NOREPORTBTN    L"Main_NoReportBtn="
#define DW_MANIFEST_MAIN_ALWAYSREPORTBTN L"Main_AlwaysReportBtn="
#define DW_MANIFEST_MAIN_QUEUEBTN       L"Main_QueueBtn="
#define DW_MANIFEST_MAIN_NOQUEUEBTN     L"Main_NoQueueBtn="
#define DW_MANIFEST_MAIN_QUEUETEXT      L"Main_QueueText="
#define DW_MANIFEST_MAIN_FEEDBACKLINK   L"Main_FeedbackLink="

#define DW_MANIFEST_DETAILS_CAPTION     L"Details_Caption="
#define DW_MANIFEST_DETAILS_PRE_HEADER  L"Details_Pre_Header="
#define DW_MANIFEST_DETAILS_PRE_BODY    L"Details_Pre_Body="
#define DW_MANIFEST_DETAILS_SIG_HEADER  L"Details_Sig_Header="
#define DW_MANIFEST_DETAILS_SIG_BODY    L"Details_Sig_Body="
#define DW_MANIFEST_DETAILS_POST_HEADER L"Details_Post_Header="
#define DW_MANIFEST_DETAILS_POST_BODY   L"Details_Post_Body=" // pretty similar to exception mode, but calls out DigPid too.
#define DW_MANIFEST_DETAILS_TECHLINK    L"Details_TechLink="
#define DW_MANIFEST_DETAILS_DCPLINK     L"Details_DCPLink="

#define DW_MANIFEST_TECH_CAPTION        L"Tech_Caption="
#define DW_MANIFEST_TECH_FILES_HEADER   L"Tech_Files_Header="

#define DW_MANIFEST_TRANSFER_CAPTION    L"Transfer_Caption="
#define DW_MANIFEST_TRANSFER_1CHECK     L"Transfer_1check="
#define DW_MANIFEST_TRANSFER_2CHECK     L"Transfer_2check="
#define DW_MANIFEST_TRANSFER_3CHECK     L"Transfer_3check="
#define DW_MANIFEST_TRANSFER_STATUS_INPROGRESS      L"Transfer_Status_InProgress="
#define DW_MANIFEST_TRANSFER_STATUS_DONE            L"Transfer_Status_Done="
#define DW_MANIFEST_TRANSFER_CHECKBOX   L"Transfer_Checkbox="

#define DW_MANIFEST_SECONDLEVEL_CAPTION     L"SecondLevel_Caption="
#define DW_MANIFEST_SECONDLEVEL_PRE         L"SecondLevel_Pre="
#define DW_MANIFEST_SECONDLEVEL_POST        L"SecondLevel_Post="

#define DW_MANIFEST_FINAL_CAPTION           L"Final_Caption="
#define DW_MANIFEST_FINAL_TEXT              L"Final_Text="
#define DW_MANIFEST_FINAL_TEXT_USERCANCEL   L"Final_Text_UserCancel="
#define DW_MANIFEST_FINAL_LINK              L"Final_Link="
#define DW_MANIFEST_FINAL_LINK_SURVEY       L"Final_Link_Survey="

#define DW_MANIFEST_STANDBY_CAPTION     L"Standby_Caption="
#define DW_MANIFEST_STANDBY_BODY        L"Standby_Body="

// These are reporting fields.
#define DW_MANIFEST_RFLAGS       L"ReportingFlags="
#define DW_MANIFEST_UFLAGS       L"UIFlags="
#define DW_MANIFEST_LFLAGS       L"LoggingFlags="
#define DW_MANIFEST_MFLAGS       L"MiscFlags="
#define DW_MANIFEST_BRAND        L"Brand="
#define DW_MANIFEST_EVENTSOURCE  L"EventLogSource="
#define DW_MANIFEST_EVENTID      L"EventID="
#define DW_MANIFEST_DIGPIDPATH   L"DigPidRegPath="
#define DW_MANIFEST_CHECKBOX_REGKEY L"CheckBoxRegKey="
#define DW_MANIFEST_CUSTOM_QUERY_STRING_ELEMENTS L"CustomQueryStringElements="

// DW expects at least one of these to be set.
#define DW_MANIFEST_DELETABLEFILES      L"FilesToDelete="
#define DW_MANIFEST_NONDELETABLEFILES   L"FilesToKeep="

// These may be optionally set, and will be used on second-level data requests.
#define DW_MANIFEST_USERFILES           L"UserDocs="
#define DW_MANIFEST_HEAP                L"Heap="


#define DW_X(X)                  L##X
#define DW_Y(X)                  DW_X(X)

// Seperator for file lists (Manifest DataFiles and Exception Additional Files)
#define DW_FILESEPA              '|'
#define DW_FILESEP               DW_Y(DW_FILESEPA)

#define DW_OMIT_SECTION          L"NIL"
#define DW_APPNAME_TOKEN         L"%General_AppName%"
#define DW_REPORTEE_TOKEN        L"%General_Reportee%"

// the following is required for queued information file only
#define DW_QR_VERSION              L"QueueVer="
#define DW_QR_DATE                 L"Date="
#define DW_QR_TIME                 L"Time="
#define DW_QR_REPORTSIZE           L"ReportSize="
#define DW_QR_BYTES                L"Bytes="
#define DW_QR_KILOBYTES            L"Kilobytes="
#define DW_QR_MEGABYTES            L"Megabytes="
#define DW_QR_MOREINFO             L"MoreInfo="
#define DW_QR_BP0                  L"BP0="
#define DW_QR_BP1                  L"BP1="
#define DW_QR_BP2                  L"BP2="
#define DW_QR_BP3                  L"BP3="
#define DW_QR_BP4                  L"BP4="
#define DW_QR_BP5                  L"BP5="
#define DW_QR_BP6                  L"BP6="
#define DW_QR_BP7                  L"BP7="
#define DW_QR_BP8                  L"BP8="
#define DW_QR_BP9                  L"BP9="
#define DW_QR_BP10                 L"BP10="
#define DW_QR_CBP                  L"CBP="
#define DW_QR_DWVER0               L"DWVer0="
#define DW_QR_DWVER1               L"DWVer1="
#define DW_QR_DWVER2               L"DWVer2="
#define DW_QR_DWVER3               L"DWVer3="
#define DW_QR_MODE                 L"QueueMode="


// shared reg values between DW and DW COM EXE
#define DEFAULT_SUBPATH L"Microsoft\\PCHealth\\ErrorReporting\\DW"
#define QUEUE_REG_SUBPATH  L"Software\\" DEFAULT_SUBPATH
#define QUEUE_REG_OKTOREPORT_VALUE L"OkToReportFromTheseQueues"
#define WATSON_INSTALLED_REG_SUBPATH QUEUE_REG_SUBPATH L"\\Installed"
#define WATSON_INSTALLED_REG_SUBPATH_IA64 L"Software\\Wow6432Node\\"DEFAULT_SUBPATH L"\\Installed"
#define WATSON_INSTALLED_REG_VAL L"DW0200" // keep in sync with %MSI%\src\sdl\shared\watson.sreg
#define WATSON_INSTALLED_REG_VAL_IA64 L"DW0201" // keep in sync with %MSI%\src\sdl\shared\watson.sreg

// names for the Watson exes
#define DW_EXEA            "dw20.exe"
#define DW_EXE             DW_Y(DW_EXEA)
#define DW_COM_EXEA        "dwtrig20.exe"
#define DW_COM_EXE         DW_Y(DW_COM_EXEA)

// the following option is used to exec DW to set the trigger for queued reporting
// ie Run 'dw20 -k <queue number>'
#define OPTSQRTA 'k' // queued reporting trigger
#define OPTSQRT DW_Y(OPTSQRTA)

// the following option is used to exec DW in queued reporting mode
// ie Run 'dw20 -q <queue types to report from>'
#define OPTQRMA 'q' // queued reporting mode
#define OPTQRM DW_Y(OPTQRMA)

// the following option is used to exec the DW COM EXE to trigger queued reporting after a delay time
// ie Run 'dwtrig20 -t'
#define OPTQRTA 't' // queued reporting trigger
#define OPTQRT DW_Y(OPTQRTA)

// the following option is used to exec the DW COM EXE to trigger queued reporting immediately
// ie Run 'dwtrig20 -f <queue types to report from>'
#define OPTQRFA 'f' // force queued reporting
#define OPTQRF DW_Y(OPTQRFA)

#define C_QUEUE_TYPES      3

enum // EQueueTypes
{
    dwqueueMin              = 0x00000001,

    // all the queues types
    // these must be consecutive
    // (valid range for queue types is 0x00000001 - 0x00008000, 16 total)
    dwqueueRegular          = 0x00000001,
    dwqueueSignOff          = 0x00000002,
    dwqueueHeadless         = 0x00000004,
    // next valid queue type: 0x00000008

    // the maximum dwqueueMax is 0x00008000
    dwqueueMax              = 0x00000004,

    // triggering flags (valid range for triggers is 0x00010000 - 0x08000000, 12 total)
    dwtriggerAtLogon          = 0x00010000,
    dwtriggerAtConnectionMade = 0x00020000,

    // special flags (valid range for special flags is 0x1000000 - 0x80000000, 4 total)
    dwqueueAnyAdmin                = 0x10000000, // Admin queue

    // flag combinations
    dwqueueTypes            = 0x00000007, // Regular | SignOff | Headless
};

enum // EQueuedReportingDialogResults
{
      qrdrCancel           = 0x00000001,
      qrdrLater            = 0x00000002,
      qrdrDone             = 0x00000004,
};


#ifdef DEBUG
enum // AssertActionCodes
{
    DwAssertActionFail = 0,
    DwAssertActionDebug,
    DwAssertActionIgnore,
    DwAssertActionAlwaysIgnore,
    DwAssertActionIgnoreAll,
    DwAssertActionQuit,
};
#endif

//  Caller is the app that has experienced an exception and launches DW

enum // ECrashTimeDialogStates  // msoctds
{
    msoctdsNull          = 0x00000000,
    msoctdsQuit          = 0x00000001,
    msoctdsRestart       = 0x00000002,
    msoctdsRecover       = 0x00000004,
    msoctdsUnused        = 0x00000008,
    msoctdsDebug         = 0x00000010,
};

#define MSODWRECOVERQUIT (msoctdsRecover | msoctdsQuit)
#define MSODWRESTARTQUIT (msoctdsRestart | msoctdsQuit)
#define MSODWRESPONSES (msoctdsQuit | msoctdsRestart | msoctdsRecover)

// THIS IS PHASED OUT -- DON'T USE
enum  // EMsoCrashHandlerFlags  // msochf
{
    msochfNull                = 0x00000000,
    msochfCheckboxOff         = 0x00000001,

    msochfUnused              = msoctdsUnused,  // THESE MUST BE THE SAME 0x8
    msochfCanRecoverDocuments = msoctdsRecover, // 0x4

    msochfObsoleteCanDebug    = 0x00010001,  // not used anymore
    msochfCannotSneakyDebug   = 0x00010002,  // The "hidden" debug feature won't work
    msochfDefaultDontReport   = 0x00010004,
    msochReportingDisabled    = 0x00010008,  // User cannot change default reporting choice
};


//
enum  // EMsoCrashHandlerResults  // msochr
{
    msochrNotHandled        = msoctdsNull,
    msochrUnused            = msoctdsUnused,
    msochrDebug             = msoctdsDebug,
    msochrRecoverDocuments  = msoctdsRecover,
    msochrRestart           = msoctdsRestart,
    msochrQuit              = msoctdsQuit,
};

enum  // EDwReportingFlags
{
    fDwrDeleteFiles          = 0x00000001,   // delete "files to delete" after use (plus heap, minidump, manifest).
    fDwrIgnoreHKCU           = 0x00000002,   // Only look at HKLM. If you do not set this, we will look at both HKCU and HKLM.
    fDwrForceOfflineMode     = 0x00000008,   // DW will force the report to be queued
    fDwrForceToAdminQueue    = 0x00000004 | fDwrForceOfflineMode,   // DW will force the report to be queued on the Admin queue
    fDwrDenyOfflineMode      = 0x00000010,   // DW will not allow report to be queued
    fDwrNoHeapCollection     = 0x00000020,   // DW will not gather the heap.
    fDwrNoSecondLevelCollection = 0x00000040 | fDwrNoHeapCollection, // DW will not send any second level data, including the heap
    fDwrNeverUpload          = 0x00000080,   // don't report
    fDwrDontPromptIfCantReport = 0x00000100, // DW will not show any UI if we're not going to report.
    fDwrNoDefaultCabLimit    = 0x00000200,   // DW under CER won't use 5 as the fallback but unlimited instead (policy still overrides)
};

enum  // EDwUIFlags
{
    fDwuNoEventUI            = 0x00000001,   // DW will always try to send headless, regardless of DWAllowHeadless
    // Having no UI reporting from the queue means that there is no UI at the time of the event
    fDwuNoQueueUI            = 0x00000002 | fDwuNoEventUI, // DW will put the report in the headless queue with no UI
    fDwuShowFeedbackLink     = 0x00000004,   // Show the "Why should I report" link.
    fDwuUseIE                = 0x00000008,   // always launch w/ IE

    // DO NOT use this flag.  It doesn't work.  Instead customize by using the UserInterfaceBlock.
    fDwuUseLitePlea          = 0x00000010,   // DW won't suggest product change in report plea

    fDwuManifestDebug        = 0x00000020,   // DW will provide a debug button in manifest mode
    fDwuDenySuspend          = 0x00000040,   // DW will keep powersave mode from suspending it, until transfer is complete.
};

enum  // EDwLoggingFlags
{
    fDwlNoParameterLog       = 0x00000001,   // DW won't log the initial parameters
    fDwlNoBucketLog          = 0x00000002,   // DW won't log the bucket ID and the bucket parameters
    fDwlResponseLog          = 0x00000004,   // log the resolved response, including extra args, to the event log with event 1010.
};

enum  // EDwExceptionModeFlags
{
    fDweCheckSig             = 0x00000001,   // checks the signatures of the App/Mod list
    fDweTagCommandLine       = 0x00000002,   // adds DW_CMDLINE_TAG to command line when restarting apps.
    fDweDefaultQuit          = 0x00000004,   // In exception mode, DW will default the restart/recover box to off. Doesn't affect regkey-based checkboxes.
    fDweKeepMinidump         = 0x00000008,   // Don't delete the minidump when we're done using it.
    fDweIgnoreAeDebug        = 0x00000010,   // Don't check AeDebug to determine whether or not to show the Debug button.
    fDweGatherHeapAsMdmp     = 0x00000020,   // Use the minidump API to gather the heap, rather than the minimal version we build directly
    fDweReleaseBeforeCabbing = 0x00000040,   // Release the thread doing the dump and cab the files in the background.
};

enum  // EDwMiscFlags
{
    fDwmOfficeDigPID         = 0x00000001,   // use custom internal code to figure out what SKU of Office is installed
    fDwmOfficeSQMReporting   = 0x00000002,   // DW should collect SQM data and save it for Office to upload (if QMEnabled reg key is set).
    fDwmContainsOnlyAnonymousData = 0x00000003, // DW does nothing with this flag, but the LH shim will recognize it. It should be used in rare
                                                // circumstances when the silent report is known to contain NO PII.
};


typedef struct _AssertBlock
{
    // for Assert communication
    DWORD dwTag;                       // [in] AssertTag
    char szAssert[DW_MAX_ASSERT_CCH];  // [in] Sz from the assert
    int AssertActionCode;              // [out] action code to take

    DWORD cdwCallstack; // The number of actual callstack entries
    DWORD rgdwCallstack[DW_MAX_CALLSTACK]; // Callstack

} AssertBlock;

typedef struct _UserInterfaceBlock
{
    // THIS ITEM IS REQUIRED. We do not go find your executable name to use instead.
    WCHAR wzGeneral_AppName[DW_APPNAME_LENGTH];

    // everything below this point is optional
    WCHAR wzGeneral_Reportee[DW_APPNAME_LENGTH];    // on whose behalf we request the report; otherwise "Microsoft"

    WCHAR wzMain_Caption[DW_MAX_ERROR_CWC];         // otherwise <General_AppName>
    WCHAR wzMain_IconFile[DW_MAX_PATH];             // otherwise pulled from executable
    WCHAR wzMain_Intro_Bold[DW_MAX_ERROR_CWC];      // otherwise "<General_AppName> has encountered a problem and needs to close. We are sorry for the inconvenience."
    WCHAR wzMain_Intro_Reg[DW_MAX_ERROR_CWC];       // various defaults, usually "Please tell <General_Reportee> about this problem"
    WCHAR wzMain_Checkbox[DW_MAX_ERROR_CWC];        // otherwise "Don't show me this again"
    WCHAR wzMain_Plea_Bold[DW_MAX_ERROR_CWC];       // otherwise "Please tell <General_Reportee> about this problem" ("NIL" means skip whole string)
    WCHAR wzMain_Plea_Reg[DW_MAX_ERROR_CWC];        // otherwise "We have created an error report that you can send to help us improve <General_AppName>. We will treat this report as confidential and anonymous."
    WCHAR wzMain_DetailsLink[DW_MAX_ERROR_CWC];     // otherwise "See what data this error report contains."
    WCHAR wzMain_ReportBtn[DW_APPNAME_LENGTH];      // otherwise "&Send Error Report"
    WCHAR wzMain_NoReportBtn[DW_APPNAME_LENGTH];    // otherwise "&Don't Send"
    WCHAR wzMain_AlwaysReportBtn[DW_APPNAME_LENGTH];// otherwise "&Send Error Report"
    WCHAR wzMain_QueueBtn[DW_APPNAME_LENGTH];       // otherwise "&Send Report Later"
    WCHAR wzMain_NoQueueBtn[DW_APPNAME_LENGTH];     // otherwise "&Don't Report"
    WCHAR wzMain_QueueText[DW_MAX_ERROR_CWC];       // otherwise "You cannot send this error report now because you are not connected...."
    WCHAR wzMain_FeedbackLink[DW_MAX_ERROR_CWC];    // otherwise "Why should I report to %General_Reportee%?"

    WCHAR wzDetails_Caption[DW_MAX_ERROR_CWC];      // otherwise <General_AppName>
    WCHAR wzDetails_Pre_Header[DW_MAX_ERROR_CWC];   // otherwise "Error Details"
    WCHAR wzDetails_Pre_Body[DW_MAX_ERROR_CWC];     // otherwise "foo" ("NIL" means delete whole Pre section)
    WCHAR wzDetails_Sig_Header[DW_MAX_ERROR_CWC];   // otherwise "Error Signature"
    WCHAR wzDetails_Sig_Body[DW_MAX_ERROR_CWC];     // otherwise constructed by DW from bucket params from stage 2 URL ("NIL" means delete whole Sig section)
    WCHAR wzDetails_Post_Header[DW_MAX_ERROR_CWC];  // otherwise "Reporting Details"
    WCHAR wzDetails_Post_Body[DW_MAX_ERROR_CWC];    // otherwise "This error report includes:..." ("NIL" means delete whole Post section)
    WCHAR wzDetails_TechLink[DW_MAX_ERROR_CWC];     // otherwise "View the contents of this error report"
    WCHAR wzDetails_DCPLink[DW_MAX_ERROR_CWC];      // otherwise "Read our Data Collection Policy"

    WCHAR wzTech_Caption[DW_MAX_ERROR_CWC];         // otherwise "Error Report Contents"
    WCHAR wzTech_MDump_Header[DW_MAX_ERROR_CWC];    // otherwise "The following information about your process will be reported...."
    WCHAR wzTech_Files_Header[DW_MAX_ERROR_CWC];    // otherwise "The following files will be included in this error report."

    WCHAR wzTransfer_Caption[DW_MAX_ERROR_CWC];     // otherwise "Error Reporting"
    WCHAR wzTransfer_1Check[DW_MAX_ERROR_CWC];      // otherwise "Preparing error report"
    WCHAR wzTransfer_2Check[DW_MAX_ERROR_CWC];      // otherwise "Connecting to server"
    WCHAR wzTransfer_3Check[DW_MAX_ERROR_CWC];      // otherwise "Checking for the status of this problem"
    WCHAR wzTransfer_Status_InProgress[DW_MAX_ERROR_CWC];   // otherwise "Transferring report..."
    WCHAR wzTransfer_Status_Done[DW_MAX_ERROR_CWC]; // otherwise "Reporting Completed.  Thank you!"
    WCHAR wzTransfer_Checkbox[DW_MAX_ERROR_CWC];    // otherwise "Close When Done"

    WCHAR wzSecondLevel_Caption[DW_MAX_ERROR_CWC];  // otherwise "More Information"
    WCHAR wzSecondLevel_Pre[DW_MAX_ERROR_CWC];      // otherwise "In order to correctly diagnose this problem the following information...."
    WCHAR wzSecondLevel_Post[DW_MAX_ERROR_CWC];     // otherwise "Please click "Cancel" if you do not wish to share this information" (May also contain "Your files may contain sensitive information")

    WCHAR wzFinal_Caption[DW_MAX_ERROR_CWC];        // otherwise "Reporting"
    WCHAR wzFinal_Text[DW_MAX_ERROR_CWC];           // otherwise "Thank you for taking the time to report this problem."
    WCHAR wzFinal_Text_UserCancel[DW_MAX_ERROR_CWC]; // otherwise "Reporting stopped on user cancel."
    WCHAR wzFinal_Link[DW_MAX_ERROR_CWC];           // otherwise "Get more information about preventing this problem in the future."
    WCHAR wzFinal_Link_Survey[DW_MAX_ERROR_CWC];    // otherwise "Provide additional information about this problem via a short questionnaire."

    WCHAR wzStandby_Caption[DW_MAX_ERROR_CWC];      // otherwise "Power Management"
    WCHAR wzStandby_Body[DW_MAX_ERROR_CWC];         // otherwise "Windows cannot go on standby because <General_AppName> has experienced..."

    WCHAR wzDialup_Caption[DW_MAX_ERROR_CWC];       // otherwise "Active Internet Connection Required"
    WCHAR wzDialup_Body[DW_MAX_ERROR_CWC];          // otherwise "In order to report this problem, you must connect to the Internet. Please start your connection before continuing."

    WCHAR wzHangup_Caption[DW_MAX_ERROR_CWC];       // otherwise "Finished Reporting"
    WCHAR wzHangup_Body[DW_MAX_ERROR_CWC];          // otherwise "Reporting has finished. You may close your Internet connection now."
    WCHAR wzHangup_Body_UserCancel[DW_MAX_ERROR_CWC];   // otherwise "Reporting has been cancelled. You may close your Internet connection now."

    WCHAR wzQueued_EventDescription[DW_MAX_ERROR_CWC];  // otherwise "Application Error"

} UserInterfaceBlock;


typedef struct _CustomMinidumpBlock
{
    BOOL  fCustomMinidump;
    DWORD dwMinidumpType;
    BOOL  fOnlyThisThread;
    DWORD dwThisThreadFlags;
    DWORD dwOtherThreadFlags;
    DWORD dwThisThreadExFlags;
    DWORD dwOtherThreadExFlags;
    DWORD dwPreferredModuleFlags;
    DWORD dwOtherModuleFlags;
} CustomMinidumpBlock;

typedef struct _GenericModeBlock
{
    BOOL fInited;
    WCHAR wzEventTypeName[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP1[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP2[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP3[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP4[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP5[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP6[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP7[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP8[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP9[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP10[DW_MAX_BUCKETPARAM_CWC];
} GenericModeBlock;


typedef struct _DWSharedMem20
{
    DWORD dwSize;               // should be set to size of DWSharedMem
    DWORD dwVersion;            // callers should set this to DWCurrentVersion as in this header

    DWORD pid;                  // Process Id of caller
    DWORD tid;                  // Id of excepting thread
    DWORD_PTR eip;              // EIP of the excepting instruction
    PEXCEPTION_POINTERS pep;    // Exception pointers given to the callee's
                                // exception handler
    HANDLE hEventDone;          // event DW signals when done
                                // caller will also signal this if it things
                                // DW has hung and restarts itself
    HANDLE hEventNotifyDone;    // App sets when it's done w/ notifcation phase
    HANDLE hEventAlive;         // heartbeat event DW signals per EVENT_TIMEOUT
    HANDLE hMutex;              // to protect the signaling of EventDone
    HANDLE hProc;               // handle to the calling process

    DWORD bfDWRFlags;           // controls caller-specific behaviors wrt REPORTING
    DWORD bfDWUFlags;           // controls caller-specific behaviors wrt UI
    DWORD bfDWLFlags;           // controls caller-specific behaviors wrt LOGGING
    DWORD bfDWEFlags;           // controls caller-specific behaviors wrt EXCEPTION MODE SPECIFICS
    DWORD bfDWMFlags;           // controls caller-specific behaviors wrt MISCELLANEOUS THINGS

    LCID lcidUI;                // will try this UI langauge if non-zero
                                // next DW will use the system LCID,
                                // and if it can't find an intl dll for that,
                                // will fall back on US English (1033)

    DWORD bfmsoctdsOffer;     // bitfield of user choices to offer
                              // note that you must specify two of:
                              // Quit, Restart, Recover, Ignore
                              // The Debug choice is independent
    DWORD bfmsoctdsNotify;    // bitfield of user choices for which the
                              // app wants control back instead of simply being
                              // terminated by DW.  The app will then be
                              // responsible for pinging DW (if desired) with
                              // hEventAlive and for notify DW it's ok to
                              // terminate the app w/ hEventDone

    DWORD bfmsoctdsLetRun;    // bitfield of user choices for which the
                              // app wants control back instead of being
                              // terminated by DW.  DW can then safely ignore
                              // the app and exit.

    WCHAR wzEventLogSource[DW_MAX_EVENTSOURCE];    // Set this as your source if you want to log events.

    WCHAR wzModuleFileName[DW_MAX_PATH];        // The result of GetModuleFileName(NULL)

    char szPIDRegKey[DW_MAX_PIDREGKEY];     // name of the key that holds the PID
                                       // can be used by the Server for
                                       // spoof-detection
    char szBrand[DW_APPNAME_LENGTH];   // passed as a param to Privacy Policy link
    char szCustomQueryStringElements[DW_MAX_PATH]; // passsed as param to Privacy Policy Link and Response Link

    char szCheckBoxRegKey[DW_MAX_PATH];   // If we get a key, we should show the
                                          // "Never send reports for this type of event" checkbox.
                                          // That string can be overridden in the UIB.

    WCHAR wzDotDataDlls[DW_MAX_PATH];  // contains the list of DLLs, terminated
                                       // by '\0' characters, that DW will
                                       // collect the .data sections into the
                                       // full minidump version
                                       // e.g. "mso9.dll\0outllib.dll\0"

    WCHAR wzFilesToDelete[1024];    // File list, seperated by DW_FILESEP
                                    // each of these files gets added to the
                                    // cab at upload time
                                    // These are files that we will delete
                                    // if fDwrDeleteFiles is sent.

    WCHAR wzFilesToKeep[1024];  // File list, seperated by DW_FILESEP
                                // each of these files gets added to the
                                // cab at upload time
                                // These are files that we will NOT delete
                                // if fDwrDeleteFiles is sent.

    WCHAR wzUserDocs[DW_MAX_USERDOCS_CWC]; // File list, seperated by DW_FILESEP
                            // each of these files gets added to the
                            // cab at upload time IFF we get a second-level request for them.
                            // These are files that we will NOT delete
                            // if fDwrDeleteFiles is sent.

    UserInterfaceBlock uib;     // encapsulates UI override data. You must set the appname in here.
    AssertBlock ab;             // encapsulates assert-tag data
    GenericModeBlock gmb;       // encapsulates custom bucket parameters for generic mode
    CustomMinidumpBlock cmb;    // encapsulates customization info for minidump gathering

    // OUTPARAMS
    DWORD msoctdsResult;      // result from crash-time dialog
    BOOL fReportProblem;      // did user approve reporting?
    WCHAR wzMinidumpLocation[DW_MAX_PATH];  // when fDweKeepMinidump is set, we write its name here.
                                            // it is available for access once hEventDone is signaled.

    // DYNAMIC INPARAMS used during recovery conversation for DW's UI
    int iPingCurrent;         // current count for the recovery progress bar
    int iPingEnd;             // index for the end of the recovery progress bar

} DWSharedMem20, DWSharedMem;


#pragma pack(pop, msodw_h)
#endif // MSODW_H
