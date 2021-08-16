// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// EEMessageBox.h
//

//
// This module contains the definition for the message box utility code for use
// inside the Execution Engine. These APIs ensure the GC mode is properly
// toggled to preemptive before the dialog is displayed.
//
//*****************************************************************************

#ifndef _H_EEMESSAGEBOX
#define _H_EEMESSAGEBOX

//========================================================================
// APIs to pop messages boxes. These should be used instead of the UtilXXX
// versions since they ensure we properly switch to preemptive GC mode and
// validate that the thread can tolerate GC transitions before calling
// out.
//========================================================================

int EEMessageBoxCatastrophicVA(
                  UINT uText,               // Text for MessageBox
                  UINT uTitle,              // Title for MessageBox
                  UINT uType,               // Style of MessageBox
                  BOOL showFileNameInTitle, // Flag to show FileName in Caption
                  va_list insertionArgs);   // Additional Arguments

int EEMessageBoxCatastrophic(
                  UINT iText,       // Text for MessageBox
                  UINT iTitle,      // Title for MessageBox
                  ...);             // Additional Arguments

int EEMessageBoxCatastrophicWithCustomizedStyle(
                  UINT iText,               // Text for MessageBox
                  UINT iTitle,              // Title for MessageBox
                  UINT uType,               // Style of MessageBox
                  BOOL showFileNameInTitle, // Flag to show FileName in Caption
                  ...);                     // Additional Arguments

#ifdef _DEBUG

int EEMessageBoxNonLocalizedDebugOnly(
                  LPCWSTR lpText,    // Text message
                  LPCWSTR lpCaption, // Caption
                  UINT uType,       // Style of MessageBox
                  ... );            // Additional Arguments

#endif // _DEBUG

// If we didn't display a dialog to the user, this method returns IDIGNORE, unlike the others that return IDABORT.
int EEMessageBoxNonLocalizedNonFatal(
                  LPCWSTR lpText,   // Text message
                  LPCWSTR lpTitle,  // Caption
                  UINT uType,       // Style of MessageBox
                  ... );            // Additional Arguments

// If we didn't display a dialog to the user, this method returns IDIGNORE, unlike the others that return IDABORT.
int EEMessageBoxNonLocalizedNonFatal(
                  LPCWSTR lpText,   // Text message
                  LPCWSTR lpTitle,  // Caption
                  LPCWSTR lpDetails,// Detailed message like a stack trace
                  UINT uType,       // Style of MessageBox
                  ... );            // Additional Arguments

#endif /* _H_EEMESSAGEBOX */

