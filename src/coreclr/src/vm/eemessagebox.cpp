// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// EEMessageBox.h
//

//
// This module contains the implementation for the message box utility code for 
// use inside the Execution Engine. These APIs ensure the GC mode is properly
// toggled to preemptive before the dialog is displayed. 
//
//*****************************************************************************

#include "common.h"
#include "eemessagebox.h"

// Undef these so we can call them from the EE versions.
#undef UtilMessageBoxCatastrophicVA
#undef UtilMessageBoxVA
#undef UtilMessageBoxNonLocalizedVA

int EEMessageBoxCatastrophicVA(
                  UINT uText,               // Text for MessageBox
                  UINT uTitle,              // Title for MessageBox
                  UINT uType,               // Style of MessageBox
                  BOOL showFileNameInTitle, // Flag to show FileName in Caption
                  va_list insertionArgs)    // Additional Arguments
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    return UtilMessageBoxCatastrophicVA(uText, uTitle, uType, showFileNameInTitle, insertionArgs);
}

int EEMessageBoxCatastrophic(
                  UINT uText,       // Text for MessageBox
                  UINT uTitle,      // Title for MessageBox
                  ...)              // Additional Arguments
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    va_list marker;
    va_start(marker, uTitle);

    int result = EEMessageBoxCatastrophicVA(uText, uTitle, MB_OK | MB_ICONERROR, TRUE, marker);
    va_end( marker );

    return result;
}

int EEMessageBoxCatastrophicWithCustomizedStyle(
                  UINT uText,               // Text for MessageBox
                  UINT uTitle,              // Title for MessageBox
                  UINT uType,               // Style of MessageBox
                  BOOL showFileNameInTitle, // Flag to show FileName in Caption
                  ...)                      // Additional Arguments
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    va_list marker;
    va_start(marker, showFileNameInTitle);

    int result = EEMessageBoxCatastrophicVA(uText, uTitle, uType, showFileNameInTitle, marker);
    va_end( marker );

    return result;
}

#ifdef _DEBUG

int EEMessageBoxNonLocalizedDebugOnly(
                  LPCWSTR lpText,   // Text message
                  LPCWSTR lpTitle,  // Caption
                  UINT uType,       // Style of MessageBox
                  ... )             // Additional Arguments
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        NOTHROW;
    }
    CONTRACTL_END;

    GCX_PREEMP();

    va_list marker;
    va_start(marker, uType);

    int result = UtilMessageBoxNonLocalizedVA(NULL, lpText, lpTitle, uType, FALSE, TRUE, NULL, marker);
    va_end( marker );

    return result;
}

#endif // _DEBUG

// If we didn't display a dialog to the user, this method returns IDIGNORE, unlike the others that return IDABORT.
int EEMessageBoxNonLocalizedNonFatal(
                  LPCWSTR lpText,   // Text message
                  LPCWSTR lpTitle,  // Caption
                  UINT uType,       // Style of MessageBox
                  ... )             // Additional Arguments
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        NOTHROW;
    }
    CONTRACTL_END;

    GCX_PREEMP();

    va_list marker;
    va_start(marker, uType);
    BOOL inputFromUser = FALSE;

    int result = UtilMessageBoxNonLocalizedVA(NULL, lpText, lpTitle, NULL, uType, FALSE, TRUE, &inputFromUser, marker);
    va_end( marker );

	if (inputFromUser == FALSE && result == IDABORT)
		result = IDIGNORE;

    return result;
}

// If we didn't display a dialog to the user, this method returns IDIGNORE, unlike the others that return IDABORT.
int EEMessageBoxNonLocalizedNonFatal(
                  LPCWSTR lpText,   // Text message
                  LPCWSTR lpTitle,  // Caption
                  LPCWSTR lpDetails,// Detailed message like a stack trace
                  UINT uType,       // Style of MessageBox
                  ... )             // Additional Arguments
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        NOTHROW;
    }
    CONTRACTL_END;

    GCX_PREEMP();

    va_list marker;
    va_start(marker, uType);
    BOOL inputFromUser = FALSE;

    int result = UtilMessageBoxNonLocalizedVA(NULL, lpText, lpTitle, lpDetails, uType, FALSE, TRUE, &inputFromUser, marker);
    va_end( marker );

	if (inputFromUser == FALSE && result == IDABORT)
		result = IDIGNORE;

    return result;
}

// Redefine these to errors just in case code is added after this point in the file.
#define UtilMessageBoxCatastrophicVA __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")
#define UtilMessageBoxVA __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")
#define UtilMessageBoxNonLocalizedVA __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")

