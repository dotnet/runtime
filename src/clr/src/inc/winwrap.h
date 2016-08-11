// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// WinWrap.h
//
// This file contains wrapper functions for Win32 API's that take strings.
//
// The Common Language Runtime internally uses UNICODE as the internal state
// and string format.  This file will undef the mapping macros so that one
// cannot mistakingly call a method that isn't going to work.  Instead, you
// have to call the correct wrapper API.
//
//*****************************************************************************

#ifndef __WIN_WRAP_H__
#define __WIN_WRAP_H__

//********** Macros. **********************************************************
#if !defined(WIN32_LEAN_AND_MEAN)
#define WIN32_LEAN_AND_MEAN
#endif
#if !defined(WIN32_LEAN_AND_MEAN)
#define INC_OLE2
#endif

#ifdef _WIN64
#define HIWORD64(p)     ((ULONG_PTR)(p) >> 16)
#else
#define HIWORD64        HIWORD
#endif

#define SAFEDELARRAY(p) if ((p) != NULL) { delete [] p; (p) = NULL; }

//
// WinCE uniformly uses cdecl calling convention on x86. __stdcall is defined as __cdecl in SDK.
// STDCALL macro is meant to be used where we have hard dependency on __stdcall calling convention
// - the unification with __cdecl does not apply to STDCALL.
//
#define STDCALL _stdcall

//********** Includes. ********************************************************

#include <crtwrap.h>
#include <windows.h>
#include <wincrypt.h>
#include <specstrings.h>

#include "registrywrapper.h"
#include "longfilepathwrappers.h"

#if defined(_PREFAST_) || defined(SOURCE_FORMATTING)
//
// For PREFAST we don't want the C_ASSERT to be expanded since it always
// involves the comparison of two constants which causes PREfast warning 326
//
#undef C_ASSERT
#define C_ASSERT(expr)
#endif

#include "palclr.h"

#if !defined(__TODO_PORT_TO_WRAPPERS__)
//*****************************************************************************
// Undefine all of the windows wrappers so you can't use them.
//*****************************************************************************

// wincrypt.h
#undef CryptAcquireContext
#undef CryptGetDefaultProvider
#undef CryptSignHash
#undef CryptVerifySignature

// winbase.h
#undef GetBinaryType
#undef GetShortPathName
#undef GetLongPathName
#undef GetEnvironmentStrings
#undef FreeEnvironmentStrings
#undef FormatMessage
#undef CreateMailslot
#undef EncryptFile
#undef DecryptFile
#undef OpenRaw
#undef QueryRecoveryAgents
#undef lstrcmp
#undef lstrcmpi
#undef lstrcpyn
#undef lstrcpy
#undef lstrcat
#undef lstrlen
#undef CreateMutex
#undef OpenMutex
#undef CreateEvent
#undef OpenEvent
#undef CreateSemaphore
#undef OpenSemaphore
#undef CreateWaitableTimer
#undef OpenWaitableTimer
#undef CreateFileMapping
#undef OpenFileMapping
#undef GetLogicalDriveStrings
#undef LoadLibrary
#undef LoadLibraryEx
#undef GetModuleFileName
#undef GetModuleHandle
#undef GetModuleHandleEx
#undef CreateProcess
#undef FatalAppExit
#undef GetStartupInfo
#undef GetCommandLine
#undef GetEnvironmentVariable
#undef SetEnvironmentVariable
#undef ExpandEnvironmentStrings
#undef OutputDebugString
#undef FindResource
#undef FindResourceEx
#undef EnumResourceTypes
#undef EnumResourceNames
#undef EnumResourceLanguages
#undef BeginUpdateResource
#undef UpdateResource
#undef EndUpdateResource
#undef GlobalAddAtom
#undef GlobalFindAtom
#undef GlobalGetAtomName
#undef AddAtom
#undef FindAtom
#undef GetAtomName
#undef GetProfileInt
#undef GetProfileString
#undef WriteProfileString
#undef GetProfileSection
#undef WriteProfileSection
#undef GetPrivateProfileInt
#undef GetPrivateProfileString
#undef WritePrivateProfileString
#undef GetPrivateProfileSection
#undef WritePrivateProfileSection
#undef GetPrivateProfileSectionNames
#undef GetPrivateProfileStruct
#undef WritePrivateProfileStruct
#undef GetDriveType
#undef GetSystemDirectory
#undef GetTempPath
#undef GetTempFileName
#undef GetWindowsDirectory
#undef SetCurrentDirectory
#undef GetCurrentDirectory
#undef GetDiskFreeSpace
#undef GetDiskFreeSpaceEx
#undef CreateDirectory
#undef CreateDirectoryEx
#undef RemoveDirectory
#undef GetFullPathName
#undef DefineDosDevice
#undef QueryDosDevice
#undef CreateFile
#undef SetFileAttributes
#undef GetFileAttributes
#undef GetFileAttributesEx
#undef GetCompressedFileSize
#undef DeleteFile
#undef FindFirstFileEx
#undef FindFirstFile
#undef FindNextFile
#undef SearchPath
#undef CopyFile
#undef CopyFileEx
#undef MoveFile
#undef MoveFileEx
#undef MoveFileWithProgress
#undef CreateSymbolicLink
#undef QuerySymbolicLink
#undef CreateHardLink
#undef CreateNamedPipe
#undef GetNamedPipeHandleState
#undef CallNamedPipe
#undef WaitNamedPipe
#undef SetVolumeLabel
#undef GetVolumeInformation
#undef ClearEventLog
#undef BackupEventLog
#undef OpenEventLog
#undef RegisterEventSource
#undef OpenBackupEventLog
#undef ReadEventLog
#undef ReportEvent
#undef AccessCheckAndAuditAlarm
#undef AccessCheckByTypeAndAuditAlarm
#undef AccessCheckByTypeResultListAndAuditAlarm
#undef ObjectOpenAuditAlarm
#undef ObjectPrivilegeAuditAlarm
#undef ObjectCloseAuditAlarm
#undef ObjectDeleteAuditAlarm
#undef PrivilegedServiceAuditAlarm
#undef SetFileSecurity
#undef GetFileSecurity
#undef FindFirstChangeNotification
#undef IsBadStringPtr
#undef LookupAccountSid
#undef LookupAccountName
#undef LookupPrivilegeValue
#undef LookupPrivilegeName
#undef LookupPrivilegeDisplayName
#undef BuildCommDCB
#undef BuildCommDCBAndTimeouts
#undef CommConfigDialog
#undef GetDefaultCommConfig
#undef SetDefaultCommConfig
#undef GetComputerName
#undef SetComputerName
#undef GetUserName
#undef LogonUser
#undef CreateProcessAsUser
#undef GetCurrentHwProfile
#undef GetVersionEx
#undef CreateJobObject
#undef OpenJobObject
#undef SetDllDirectory

// winuser.h
#undef MAKEINTRESOURCE
#undef wvsprintf
#undef wsprintf
#undef LoadKeyboardLayout
#undef GetKeyboardLayoutName
#undef CreateDesktop
#undef OpenDesktop
#undef EnumDesktops
#undef CreateWindowStation
#undef OpenWindowStation
#undef EnumWindowStations
#undef GetUserObjectInformation
#undef SetUserObjectInformation
#undef RegisterWindowMessage
#undef SIZEZOOMSHOW
#undef WS_TILEDWINDOW
#undef GetMessage
#undef DispatchMessage
#undef PeekMessage

#undef SendMessage
#undef SendMessageTimeout
#undef SendNotifyMessage
#undef SendMessageCallback
#undef BroadcastSystemMessage
#undef RegisterDeviceNotification
#undef PostMessage
#undef PostThreadMessage
#undef PostAppMessage
#undef DefWindowProc
#undef CallWindowProc
#undef RegisterClass
#undef UnregisterClass
#undef GetClassInfo
#undef RegisterClassEx
#undef GetClassInfoEx
#undef CreateWindowEx
#undef CreateWindow
#undef CreateDialogParam
#undef CreateDialogIndirectParam
#undef CreateDialog
#undef CreateDialogIndirect
#undef DialogBoxParam
#undef DialogBoxIndirectParam
#undef DialogBox
#undef DialogBoxIndirect
#undef SetDlgItemText
#undef GetDlgItemText
#undef SendDlgItemMessage
#undef DefDlgProc
#undef CallMsgFilter
#undef RegisterClipboardFormat
#undef GetClipboardFormatName
#undef CharToOem
#undef OemToChar
#undef CharToOemBuff
#undef OemToCharBuff
#undef CharUpper
#undef CharUpperBuff
#undef CharLower
#undef CharLowerBuff
#undef CharNext
#undef IsCharAlpha
#undef IsCharAlphaNumeric
#undef IsCharUpper
#undef IsCharLower
#undef GetKeyNameText
#undef VkKeyScan
#undef VkKeyScanEx
#undef MapVirtualKey
#undef MapVirtualKeyEx
#undef LoadAccelerators
#undef CreateAcceleratorTable
#undef CopyAcceleratorTable
#undef TranslateAccelerator
#undef LoadMenu
#undef LoadMenuIndirect
#undef ChangeMenu
#undef GetMenuString
#undef InsertMenu
#undef AppendMenu
#undef ModifyMenu
#undef InsertMenuItem
#undef GetMenuItemInfo
#undef SetMenuItemInfo
#undef DrawText
#undef DrawTextEx
#undef GrayString
#undef DrawState
#undef TabbedTextOut
#undef GetTabbedTextExtent
#undef SetProp
#undef GetProp
#undef RemoveProp
#undef EnumPropsEx
#undef EnumProps
#undef SetWindowText
#undef GetWindowText
#undef GetWindowTextLength
#undef MessageBox
#undef MessageBoxEx
#undef MessageBoxIndirect
#undef COLOR_3DSHADOW
#undef GetWindowLong
#undef SetWindowLong
#undef GetClassLong
#undef SetClassLong
#undef FindWindow
#undef FindWindowEx
#undef GetClassName
#undef SetWindowsHook
#undef SetWindowsHook
#undef SetWindowsHookEx
#undef MFT_OWNERDRAW
#undef LoadBitmap
#undef LoadCursor
#undef LoadCursorFromFile
#undef LoadIcon
#undef LoadImage
#undef LoadString
#undef IsDialogMessage
#undef DlgDirList
#undef DlgDirSelectEx
#undef DlgDirListComboBox
#undef DlgDirSelectComboBoxEx
#undef DefFrameProc
#undef DefMDIChildProc
#undef CreateMDIWindow
#undef WinHelp
#undef ChangeDisplaySettings
#undef ChangeDisplaySettingsEx
#undef EnumDisplaySettings
#undef EnumDisplayDevices
#undef SystemParametersInfo
#undef GetMonitorInfo
#undef GetWindowModuleFileName
#undef RealGetWindowClass
#undef GetAltTabInfo
#undef GetCalendarInfo
#undef GetDateFormat
#undef GetTimeFormat
#undef LCMapString

// winnetwk.h
#undef WNetGetConnection

// Win32 Fusion API's
#undef QueryActCtxW

#endif // !defined(__TODO_PORT_TO_WRAPPERS__)

//
// NT supports the wide entry points.  So we redefine the wrappers right back
// to the *W entry points as macros.  This way no client code needs a wrapper on NT.
//

// wincrypt.h
#define WszCryptAcquireContext CryptAcquireContextW
#define WszCryptGetDefaultProvider CryptGetDefaultProviderW
#define WszCryptSignHash CryptSignHashW
#define WszCryptVerifySignature CryptVerifySignatureW

// winbase.h
#define WszGetEnvironmentStrings   GetEnvironmentStringsW
#define WszFreeEnvironmentStrings   FreeEnvironmentStringsW
#ifndef USE_FORMATMESSAGE_WRAPPER
#define WszFormatMessage   FormatMessageW
#else
#define WszFormatMessage   CCompRC::FormatMessage
#endif
#define WszCreateMailslot   CreateMailslotW
#define WszOpenRaw   OpenRawW
#define WszQueryRecoveryAgents   QueryRecoveryAgentsW
#define Wszlstrcmp   lstrcmpW
#define Wszlstrcmpi   lstrcmpiW
#define Wszlstrcpy lstrcpyW
#define Wszlstrcat lstrcatW
#define WszCreateMutex CreateMutexW
#define WszOpenMutex OpenMutexW
#define WszCreateEvent CreateEventW
#define WszOpenEvent OpenEventW
#define WszCreateWaitableTimer CreateWaitableTimerW
#define WszOpenWaitableTimer OpenWaitableTimerW
#define WszCreateFileMapping CreateFileMappingW
#define WszOpenFileMapping OpenFileMappingW
#define WszGetLogicalDriveStrings GetLogicalDriveStringsW
#define WszGetModuleHandle GetModuleHandleW
#define WszGetModuleHandleEx GetModuleHandleExW
#define WszFatalAppExit FatalAppExitW
#define WszGetStartupInfo GetStartupInfoW
#define WszGetCommandLine GetCommandLineW
#define WszSetEnvironmentVariable SetEnvironmentVariableW
#define WszExpandEnvironmentStrings ExpandEnvironmentStringsW
#define WszOutputDebugString OutputDebugStringW
#define WszFindResource FindResourceW
#define WszFindResourceEx FindResourceExW
#define WszEnumResourceTypes EnumResourceTypesW
#define WszEnumResourceNames EnumResourceNamesW
#define WszEnumResourceLanguages EnumResourceLanguagesW
#define WszBeginUpdateResource BeginUpdateResourceW
#define WszUpdateResource UpdateResourceW
#define WszEndUpdateResource EndUpdateResourceW
#define WszGlobalAddAtom GlobalAddAtomW
#define WszGlobalFindAtom GlobalFindAtomW
#define WszGlobalGetAtomName GlobalGetAtomNameW
#define WszAddAtom AddAtomW
#define WszFindAtom FindAtomW
#define WszGetAtomName GetAtomNameW
#define WszGetProfileInt GetProfileIntW
#define WszGetProfileString GetProfileStringW
#define WszWriteProfileString WriteProfileStringW
#define WszGetProfileSection GetProfileSectionW
#define WszWriteProfileSection WriteProfileSectionW
#define WszGetPrivateProfileInt GetPrivateProfileIntW
#define WszGetPrivateProfileString GetPrivateProfileStringW
#define WszWritePrivateProfileString WritePrivateProfileStringW
#define WszGetPrivateProfileSection GetPrivateProfileSectionW
#define WszWritePrivateProfileSection WritePrivateProfileSectionW
#define WszGetPrivateProfileSectionNames GetPrivateProfileSectionNamesW
#define WszGetPrivateProfileStruct GetPrivateProfileStructW
#define WszWritePrivateProfileStruct WritePrivateProfileStructW
#define WszGetDriveType GetDriveTypeW
#define WszGetSystemDirectory GetSystemDirectoryW
#define WszGetWindowsDirectory GetWindowsDirectoryW
#define WszGetDiskFreeSpace GetDiskFreeSpaceW
#define WszGetDiskFreeSpaceEx GetDiskFreeSpaceExW
#define WszDefineDosDevice DefineDosDeviceW
#define WszQueryDosDevice QueryDosDeviceW
#define WszQuerySymbolicLink QuerySymbolicLinkW
#define WszCreateNamedPipe CreateNamedPipeW
#define WszGetNamedPipeHandleState GetNamedPipeHandleStateW
#define WszCallNamedPipe CallNamedPipeW
#define WszWaitNamedPipe WaitNamedPipeW
#define WszSetVolumeLabel SetVolumeLabelW
#define WszGetVolumeInformation GetVolumeInformationW
#define WszClearEventLog ClearEventLogW
#define WszBackupEventLog BackupEventLogW
#define WszOpenEventLog OpenEventLogW
#define WszRegisterEventSource RegisterEventSourceW
#define WszOpenBackupEventLog OpenBackupEventLogW
#define WszReadEventLog ReadEventLogW
#define WszReportEvent ReportEventW
#define WszAccessCheckAndAuditAlarm AccessCheckAndAuditAlarmW
#define WszAccessCheckByTypeAndAuditAlarm AccessCheckByTypeAndAuditAlarmW
#define WszAccessCheckByTypeResultListAndAuditAlarm AccessCheckByTypeResultListAndAuditAlarmW
#define WszObjectOpenAuditAlarm ObjectOpenAuditAlarmW
#define WszObjectPrivilegeAuditAlarm ObjectPrivilegeAuditAlarmW
#define WszObjectCloseAuditAlarm ObjectCloseAuditAlarmW
#define WszObjectDeleteAuditAlarm ObjectDeleteAuditAlarmW
#define WszPrivilegedServiceAuditAlarm PrivilegedServiceAuditAlarmW
#define WszIsBadStringPtr __DO_NOT_USE__WszIsBadStringPtr__
#if !defined(FEATURE_CORESYSTEM) || defined(CROSSGEN_COMPILE)
#define WszLookupAccountSid LookupAccountSidW
#endif
#define WszLookupAccountName LookupAccountNameW
#define WszLookupPrivilegeValue LookupPrivilegeValueW
#define WszLookupPrivilegeName LookupPrivilegeNameW
#define WszLookupPrivilegeDisplayName LookupPrivilegeDisplayNameW
#define WszBuildCommDCB BuildCommDCBW
#define WszBuildCommDCBAndTimeouts BuildCommDCBAndTimeoutsW
#define WszCommConfigDialog CommConfigDialogW
#define WszGetDefaultCommConfig GetDefaultCommConfigW
#define WszSetDefaultCommConfig SetDefaultCommConfigW
#define WszGetComputerName GetComputerNameW
#define WszSetComputerName SetComputerNameW
#define WszGetUserName GetUserNameW
#define WszLogonUser LogonUserW
#define WszCreateProcessAsUser CreateProcessAsUserW
#define WszGetCurrentHwProfile GetCurrentHwProfileW
#define WszGetVersionEx GetVersionExW
#define WszCreateJobObject CreateJobObjectW
#define WszOpenJobObject OpenJobObjectW

// winuser.h
#define WszMAKEINTRESOURCE MAKEINTRESOURCEW
#define Wszwvsprintf wvsprintfW
#define WszLoadKeyboardLayout LoadKeyboardLayoutW
#define WszGetKeyboardLayoutName GetKeyboardLayoutNameW
#define WszCreateDesktop CreateDesktopW
#define WszOpenDesktop OpenDesktopW
#define WszEnumDesktops EnumDesktopsW
#define WszCreateWindowStation CreateWindowStationW
#define WszOpenWindowStation OpenWindowStationW
#define WszEnumWindowStations EnumWindowStationsW
#define WszGetUserObjectInformation GetUserObjectInformationW
#define WszSetUserObjectInformation SetUserObjectInformationW
#define WszRegisterWindowMessage RegisterWindowMessageW
#define WszSIZEZOOMSHOW SIZEZOOMSHOWW
#define WszWS_TILEDWINDOW WS_TILEDWINDOWW
#define WszGetMessage GetMessageW
#define WszDispatchMessage DispatchMessageW
#define WszPostMessage PostMessageW
#define WszPeekMessage PeekMessageW
#define WszSendMessage SendMessageW
#define WszSendMessageTimeout SendMessageTimeoutW
#define WszSendNotifyMessage SendNotifyMessageW
#define WszSendMessageCallback SendMessageCallbackW
#define WszBroadcastSystemMessage BroadcastSystemMessageW
#define WszRegisterDeviceNotification RegisterDeviceNotificationW
#define WszPostMessage PostMessageW
#define WszPostThreadMessage PostThreadMessageW
#define WszPostAppMessage PostAppMessageW
#define WszDefWindowProc DefWindowProcW
#define WszCallWindowProc CallWindowProcW
#define WszRegisterClass RegisterClassW
#define WszUnregisterClass UnregisterClassW
#define WszGetClassInfo GetClassInfoW
#define WszRegisterClassEx RegisterClassExW
#define WszGetClassInfoEx GetClassInfoExW
#define WszCreateWindowEx CreateWindowExW
#define WszCreateWindow CreateWindowW
#define WszCreateDialogParam CreateDialogParamW
#define WszCreateDialogIndirectParam CreateDialogIndirectParamW
#define WszCreateDialog CreateDialogW
#define WszCreateDialogIndirect CreateDialogIndirectW
#define WszDialogBoxParam DialogBoxParamW
#define WszDialogBoxIndirectParam DialogBoxIndirectParamW
#define WszDialogBox DialogBoxW
#define WszDialogBoxIndirect DialogBoxIndirectW
#define WszSetDlgItemText SetDlgItemTextW
#define WszGetDlgItemText GetDlgItemTextW
#define WszSendDlgItemMessage SendDlgItemMessageW
#define WszDefDlgProc DefDlgProcW
#define WszCallMsgFilter CallMsgFilterW
#define WszRegisterClipboardFormat RegisterClipboardFormatW
#define WszGetClipboardFormatName GetClipboardFormatNameW
#define WszCharToOem CharToOemW
#define WszOemToChar OemToCharW
#define WszCharToOemBuff CharToOemBuffW
#define WszOemToCharBuff OemToCharBuffW
#define WszCharUpper CharUpperW
#define WszCharUpperBuff CharUpperBuffW
#define WszCharLower CharLowerW
#define WszCharLowerBuff CharLowerBuffW
#define WszCharNext CharNextW
#define WszIsCharAlpha IsCharAlphaW
#define WszIsCharAlphaNumeric IsCharAlphaNumericW
#define WszIsCharUpper IsCharUpperW
#define WszIsCharLower IsCharLowerW
#define WszGetKeyNameText GetKeyNameTextW
#define WszVkKeyScan VkKeyScanW
#define WszVkKeyScanEx VkKeyScanExW
#define WszMapVirtualKey MapVirtualKeyW
#define WszMapVirtualKeyEx MapVirtualKeyExW
#define WszLoadAccelerators LoadAcceleratorsW
#define WszCreateAcceleratorTable CreateAcceleratorTableW
#define WszCopyAcceleratorTable CopyAcceleratorTableW
#define WszTranslateAccelerator TranslateAcceleratorW
#define WszLoadMenu LoadMenuW
#define WszLoadMenuIndirect LoadMenuIndirectW
#define WszChangeMenu ChangeMenuW
#define WszGetMenuString GetMenuStringW
#define WszInsertMenu InsertMenuW
#define WszAppendMenu AppendMenuW
#define WszModifyMenu ModifyMenuW
#define WszInsertMenuItem InsertMenuItemW
#define WszGetMenuItemInfo GetMenuItemInfoW
#define WszSetMenuItemInfo SetMenuItemInfoW
#define WszDrawText DrawTextW
#define WszDrawTextEx DrawTextExW
#define WszGrayString GrayStringW
#define WszDrawState DrawStateW
#define WszTabbedTextOut TabbedTextOutW
#define WszGetTabbedTextExtent GetTabbedTextExtentW
#define WszSetProp SetPropW
#define WszGetProp GetPropW
#define WszRemoveProp RemovePropW
#define WszEnumPropsEx EnumPropsExW
#define WszEnumProps EnumPropsW
#define WszSetWindowText SetWindowTextW
#define WszGetWindowText GetWindowTextW
#define WszGetWindowTextLength GetWindowTextLengthW
#define WszMessageBox LateboundMessageBoxW
#define WszMessageBoxEx MessageBoxExW
#define WszMessageBoxIndirect MessageBoxIndirectW
#define WszGetWindowLong GetWindowLongW
#define WszSetWindowLong SetWindowLongW
#define WszSetWindowLongPtr SetWindowLongPtrW
#define WszGetWindowLongPtr GetWindowLongPtrW
#define WszGetClassLong GetClassLongW
#define WszSetClassLong SetClassLongW
#define WszFindWindow FindWindowW
#define WszFindWindowEx FindWindowExW
#define WszGetClassName GetClassNameW
#define WszSetWindowsHook SetWindowsHookW
#define WszSetWindowsHook SetWindowsHookW
#define WszSetWindowsHookEx SetWindowsHookExW
#define WszLoadBitmap LoadBitmapW
#define WszLoadCursor LoadCursorW
#define WszLoadCursorFromFile LoadCursorFromFileW
#define WszLoadIcon LoadIconW
#define WszLoadImage LoadImageW
#define WszLoadString LoadStringW
#define WszIsDialogMessage IsDialogMessageW
#define WszDlgDirList DlgDirListW
#define WszDlgDirSelectEx DlgDirSelectExW
#define WszDlgDirListComboBox DlgDirListComboBoxW
#define WszDlgDirSelectComboBoxEx DlgDirSelectComboBoxExW
#define WszDefFrameProc DefFrameProcW
#define WszDefMDIChildProc DefMDIChildProcW
#define WszCreateMDIWindow CreateMDIWindowW
#define WszWinHelp WinHelpW
#define WszChangeDisplaySettings ChangeDisplaySettingsW
#define WszChangeDisplaySettingsEx ChangeDisplaySettingsExW
#define WszEnumDisplaySettings EnumDisplaySettingsW
#define WszEnumDisplayDevices EnumDisplayDevicesW
#define WszSystemParametersInfo SystemParametersInfoW
#define WszGetMonitorInfo GetMonitorInfoW
#define WszGetWindowModuleFileName GetWindowModuleFileNameW
#define WszRealGetWindowClass RealGetWindowClassW
#define WszGetAltTabInfo GetAltTabInfoW
#define WszRegOpenKeyEx ClrRegOpenKeyEx
#define WszRegOpenKey(hKey, wszSubKey, phkRes) ClrRegOpenKeyEx(hKey, wszSubKey, 0, KEY_ALL_ACCESS, phkRes)
#define WszRegQueryValue RegQueryValueW
#define WszRegQueryValueEx RegQueryValueExW
#define WszRegQueryValueExTrue RegQueryValueExW
#define WszRegQueryStringValueEx RegQueryValueExW

#ifndef FEATURE_CORECLR
#define WszRegDeleteKey RegDeleteKeyW
#define WszRegCreateKeyEx ClrRegCreateKeyEx
#define WszRegSetValueEx RegSetValueExW
#define WszRegDeleteValue RegDeleteValueW
#define WszRegLoadKey RegLoadKeyW
#define WszRegUnLoadKey RegUnLoadKeyW
#define WszRegRestoreKey RegRestoreKeyW
#define WszRegReplaceKey RegReplaceKeyW
#endif //#ifndef FEATURE_CORECLR

#define WszRegQueryInfoKey RegQueryInfoKeyW
#define WszRegEnumValue RegEnumValueW
#define WszRegEnumKeyEx RegEnumKeyExW
#define WszGetCalendarInfo GetCalendarInfoW
#define WszGetDateFormat GetDateFormatW
#define WszGetTimeFormat GetTimeFormatW
#define WszLCMapString LCMapStringW
#define WszMultiByteToWideChar MultiByteToWideChar
#define WszWideCharToMultiByte WideCharToMultiByte
#define WszCreateSemaphore CreateSemaphoreW
#define WszQueryActCtxW QueryActCtxW

// winnetwk.h
#define WszWNetGetConnection WNetGetConnectionW

#ifdef FEATURE_CORESYSTEM

// CoreSystem has CreateSemaphoreExW but not CreateSemaphoreW.
#undef WszCreateSemaphore
#define WszCreateSemaphore(_secattr, _count, _maxcount, _name) CreateSemaphoreExW((_secattr), (_count), (_maxcount), (_name), 0, SEMAPHORE_ALL_ACCESS)

// Same deal as above for GetFileVersionInfo/GetFileVersionInfoSize.
#undef GetFileVersionInfo
#define GetFileVersionInfo(_filename, _handle, _len, _data) GetFileVersionInfoEx(0, (_filename), (_handle), (_len), (_data))
#undef GetFileVersionInfoSize
#define GetFileVersionInfoSize(_filename, _handle) GetFileVersionInfoSizeEx(0, (_filename), (_handle))

#endif // FEATURE_CORESYSTEM

#ifndef _T
#define _T(str) W(str)
#endif

// on win98 and higher
#define Wszlstrlen      lstrlenW
#define Wszlstrcpy      lstrcpyW
#define Wszlstrcat      lstrcatW

//File and Directory Functions which need special handling for LongFile Names
//Note only the functions which are currently used are defined
#define WszLoadLibrary         LoadLibraryExWrapper
#define WszLoadLibraryEx       LoadLibraryExWrapper
#define WszCreateFile          CreateFileWrapper
#define WszSetFileAttributes   SetFileAttributesWrapper  
#define WszGetFileAttributes   GetFileAttributesWrapper
#define WszGetFileAttributesEx GetFileAttributesExWrapper
#define WszDeleteFile          DeleteFileWrapper
#define WszFindFirstFileEx     FindFirstFileExWrapper
#define WszFindNextFile        FindNextFileW
#define WszCopyFile            CopyFileWrapper
#define WszCopyFileEx          CopyFileExWrapper
#define WszMoveFileEx          MoveFileExWrapper
#define WszMoveFile(lpExistingFileName, lpNewFileName) WszMoveFileEx(lpExistingFileName, lpNewFileName, 0)
#define WszCreateDirectory     CreateDirectoryWrapper 
#define WszRemoveDirectory     RemoveDirectoryWrapper
#define WszCreateHardLink      CreateHardLinkWrapper

//Can not use extended syntax 
#define WszGetFullPathName     GetFullPathNameW

//Long Files will not work on these till redstone
#define WszGetCurrentDirectory GetCurrentDirectoryWrapper
#define WszGetTempFileName     GetTempFileNameWrapper
#define WszGetTempPath         GetTempPathWrapper

//APIS which have a buffer as an out parameter
#define WszGetEnvironmentVariable GetEnvironmentVariableWrapper
#define WszSearchPath          SearchPathWrapper
#define WszGetShortPathName    GetShortPathNameWrapper
#define WszGetLongPathName     GetLongPathNameWrapper
#define WszGetModuleFileName   GetModuleFileNameWrapper

//NOTE: IF the following API's are enabled ensure that they can work with LongFile Names
//See the usage and implementation of above API's
//
//#define WszGetCompressedFileSize GetCompressedFileSizeW
//#define WszMoveFileWithProgress MoveFileWithProgressW
//#define WszEncryptFile   EncryptFileW
//#define WszDecryptFile   DecryptFileW
//#define WszSetFileSecurity SetFileSecurityW
//#define WszGetFileSecurity GetFileSecurityW
//#define WszFindFirstChangeNotification FindFirstChangeNotificationW
//#define WszSetDllDirectory SetDllDirectoryW
//#define WszSetCurrentDirectory SetCurrentDirectoryW
//#define WszCreateDirectoryEx CreateDirectoryExW
//#define WszCreateSymbolicLink  CreateSymbolicLinkW
//#define WszGetBinaryType       GetBinaryTypeWrapper     //Coresys does not seem to have this API

#if FEATURE_PAL
#define WszFindFirstFile     FindFirstFileW
#else
#define WszFindFirstFile(_lpFileName_, _lpFindData_)       FindFirstFileExWrapper(_lpFileName_, FindExInfoStandard, _lpFindData_, FindExSearchNameMatch, NULL, 0)
#endif //FEATURE_PAL
//*****************************************************************************
// Prototypes for API's.
//*****************************************************************************

extern DWORD g_dwMaxDBCSCharByteSize;

void EnsureCharSetInfoInitialized();

inline DWORD GetMaxDBCSCharByteSize()
{
    // contract.h not visible here
    __annotation(W("WRAPPER ") W("GetMaxDBCSCharByteSize"));
#ifndef FEATURE_PAL
    EnsureCharSetInfoInitialized();

    _ASSERTE(g_dwMaxDBCSCharByteSize != 0);
    return (g_dwMaxDBCSCharByteSize);
#else // FEATURE_PAL
    return 3;
#endif // FEATURE_PAL
}

#ifndef FEATURE_PAL
BOOL RunningInteractive();
#else // !FEATURE_PAL
#define RunningInteractive() FALSE
#endif // !FEATURE_PAL

// Determines if the process is running as Local System or as a service. Note that this function uses the
// process' identity and not the thread's (if the thread is impersonating).
//
// If the function succeeds, it returns ERROR_SUCCESS, else it returns the error code returned by GetLastError()
DWORD RunningAsLocalSystemOrService(OUT BOOL& fIsLocalSystemOrService);

#ifndef Wsz_mbstowcs
#define Wsz_mbstowcs(szOut, szIn, iSize) WszMultiByteToWideChar(CP_ACP, 0, szIn, -1, szOut, iSize)
#endif

#ifndef Wsz_wcstombs
#define Wsz_wcstombs(szOut, szIn, iSize) WszWideCharToMultiByte(CP_ACP, 0, szIn, -1, szOut, iSize, 0, 0)
#endif

// For all platforms:

BOOL
WszCreateProcess(
    LPCWSTR lpApplicationName,
    LPCWSTR lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    BOOL bInheritHandles,
    DWORD dwCreationFlags,
    LPVOID lpEnvironment,
    LPCWSTR lpCurrentDirectory,
    LPSTARTUPINFOW lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
    );

DWORD
WszGetWorkingSet(
    VOID
    );

SIZE_T WszGetPagefileUsage();
DWORD  WszGetProcessHandleCount();

#if defined(_X86_) && defined(_MSC_VER)

//
// Windows SDK does not use intrinsics on x86. Redefine the interlocked operations to use intrinsics.
//

#include "intrin.h"

#define InterlockedIncrement            _InterlockedIncrement
#define InterlockedDecrement            _InterlockedDecrement
#define InterlockedExchange             _InterlockedExchange
#define InterlockedCompareExchange      _InterlockedCompareExchange
#define InterlockedExchangeAdd          _InterlockedExchangeAdd
#define InterlockedCompareExchange64    _InterlockedCompareExchange64
#define InterlockedAnd                  _InterlockedAnd
#define InterlockedOr                   _InterlockedOr

//
// There is no _InterlockedCompareExchangePointer intrinsic in VC++ for x86.
// winbase.h #defines InterlockedCompareExchangePointer as __InlineInterlockedCompareExchangePointer,
// which calls the Win32 InterlockedCompareExchange, not the intrinsic _InterlockedCompareExchange.
// We want the intrinsic, so we #undef the Windows version of this API, and define our own.
//
#ifdef InterlockedCompareExchangePointer
#undef InterlockedCompareExchangePointer
#endif

FORCEINLINE
PVOID
InterlockedCompareExchangePointer (
    __inout  PVOID volatile *Destination,
    __in_opt PVOID ExChange,
    __in_opt PVOID Comperand
    )
{
    return((PVOID)(LONG_PTR)_InterlockedCompareExchange((LONG volatile *)Destination, (LONG)(LONG_PTR)ExChange, (LONG)(LONG_PTR)Comperand));
}

#endif // _X86_ && _MSC_VER

#if defined(_ARM_) & !defined(FEATURE_PAL)
//
// InterlockedCompareExchangeAcquire/InterlockedCompareExchangeRelease is not mapped in SDK to the correct intrinsics. Remove once
// the SDK definition is fixed (OS Bug #516255)
//
#undef InterlockedCompareExchangeAcquire
#define InterlockedCompareExchangeAcquire _InterlockedCompareExchange_acq
#undef InterlockedCompareExchangeRelease
#define InterlockedCompareExchangeRelease _InterlockedCompareExchange_rel
#endif

#if defined(_X86_) & !defined(InterlockedIncrement64)

// Interlockedxxx64 that do not have intrinsics are only supported on Windows Server 2003
// or higher for X86 so define our own portable implementation

#undef InterlockedIncrement64
#define InterlockedIncrement64          __InterlockedIncrement64
#undef InterlockedDecrement64
#define InterlockedDecrement64          __InterlockedDecrement64
#undef InterlockedExchange64
#define InterlockedExchange64           __InterlockedExchange64
#undef InterlockedExchangeAdd64
#define InterlockedExchangeAdd64        __InterlockedExchangeAdd64

__forceinline LONGLONG __InterlockedIncrement64(LONGLONG volatile *Addend)
{
    LONGLONG Old;

    do {
        Old = *Addend;
    } while (InterlockedCompareExchange64(Addend,
                                          Old + 1,
                                          Old) != Old);

    return Old + 1;
}

__forceinline LONGLONG __InterlockedDecrement64(LONGLONG volatile *Addend)
{
    LONGLONG Old;

    do {
        Old = *Addend;
    } while (InterlockedCompareExchange64(Addend,
                                          Old - 1,
                                          Old) != Old);

    return Old - 1;
}

__forceinline LONGLONG __InterlockedExchange64(LONGLONG volatile * Target, LONGLONG Value)
{
    LONGLONG Old;

    do {
        Old = *Target;
    } while (InterlockedCompareExchange64(Target,
                                          Value,
                                          Old) != Old);

    return Old;
}

__forceinline LONGLONG __InterlockedExchangeAdd64(LONGLONG volatile * Addend, LONGLONG Value)
{
    LONGLONG Old;

    do {
        Old = *Addend;
    } while (InterlockedCompareExchange64(Addend,
                                          Old + Value,
                                          Old) != Old);

    return Old;
}

#endif // _X86_

//
// RtlVerifyVersionInfo() type mask bits
// Making our copy of type mask bits as the original
// macro name are redefined in public\internal\NDP\inc\product_version.h
//
//
#define CLR_VER_MINORVERSION                0x0000001
#define CLR_VER_MAJORVERSION                0x0000002
#define CLR_VER_BUILDNUMBER                 0x0000004
#define CLR_VER_PLATFORMID                  0x0000008
#define CLR_VER_SERVICEPACKMINOR            0x0000010
#define CLR_VER_SERVICEPACKMAJOR            0x0000020
#define CLR_VER_SUITENAME                   0x0000040
#define CLR_VER_PRODUCT_TYPE                0x0000080

BOOL GetOSVersion(LPOSVERSIONINFOW osVer);

// Output printf-style formatted text to the debugger if it's present or stdout otherwise.
inline void DbgWPrintf(const LPCWSTR wszFormat, ...)
{
    WCHAR wszBuffer[4096];

    va_list args;
    va_start(args, wszFormat);

    _vsnwprintf_s(wszBuffer, sizeof(wszBuffer) / sizeof(WCHAR), _TRUNCATE, wszFormat, args);

    va_end(args);

    if (IsDebuggerPresent())
    {
        OutputDebugStringW(wszBuffer);
    }
    else
    {
        fwprintf(stdout, W("%s"), wszBuffer);
        fflush(stdout);
    }
}

typedef int (*MessageBoxWFnPtr)(HWND hWnd,
                                LPCWSTR lpText,
                                LPCWSTR lpCaption,
                                UINT uType);

inline int LateboundMessageBoxW(HWND hWnd,
                                LPCWSTR lpText,
                                LPCWSTR lpCaption,
                                UINT uType)
{
#ifndef FEATURE_PAL
    // User32 should exist on all systems where displaying a message box makes sense.
    HMODULE hGuiExtModule = WszLoadLibrary(W("user32"));
    if (hGuiExtModule)
    {
        int result = IDCANCEL;
        MessageBoxWFnPtr fnptr = (MessageBoxWFnPtr)GetProcAddress(hGuiExtModule, "MessageBoxW");
        if (fnptr)
            result = fnptr(hWnd, lpText, lpCaption, uType);

        FreeLibrary(hGuiExtModule);
        return result;
    }
#endif // !FEATURE_PAL

    // No luck. Output the caption and text to the debugger if present or stdout otherwise.
    if (lpText == NULL)
        lpText = W("<null>");
    if (lpCaption == NULL)
        lpCaption = W("<null>");
    DbgWPrintf(W("**** MessageBox invoked, title '%s' ****\n"), lpCaption);
    DbgWPrintf(W("  %s\n"), lpText);
    DbgWPrintf(W("********\n"));
    DbgWPrintf(W("\n"));

    // Indicate to the caller that message box was not actually displayed
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
}

inline int LateboundMessageBoxA(HWND hWnd,
                                LPCSTR lpText,
                                LPCSTR lpCaption,
                                UINT uType)
{
    if (lpText == NULL)
        lpText = "<null>";
    if (lpCaption == NULL)
        lpCaption = "<null>";

    SIZE_T cchText = strlen(lpText) + 1;
    LPWSTR wszText = (LPWSTR)_alloca(cchText * sizeof(WCHAR));
    swprintf_s(wszText, cchText, W("%S"), lpText);

    SIZE_T cchCaption = strlen(lpCaption) + 1;
    LPWSTR wszCaption = (LPWSTR)_alloca(cchCaption * sizeof(WCHAR));
    swprintf_s(wszCaption, cchCaption, W("%S"), lpCaption);

    return LateboundMessageBoxW(hWnd, wszText, wszCaption, uType);
}

#if defined(FEATURE_CORESYSTEM) && !defined(CROSSGEN_COMPILE)

#define MessageBoxW LateboundMessageBoxW
#define MessageBoxA LateboundMessageBoxA

#endif // FEATURE_CORESYSTEM

#endif  // __WIN_WRAP_H__
