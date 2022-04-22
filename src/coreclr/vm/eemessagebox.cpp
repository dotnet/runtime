// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

// Forward declare the needed MessageBox API.
int UtilMessageBoxCatastrophicVA(
                  UINT uText,       // Text for MessageBox
                  UINT uTitle,      // Title for MessageBox
                  UINT uType,       // Style of MessageBox
                  BOOL ShowFileNameInTitle, // Flag to show FileName in Caption
                  va_list args);    // Additional Arguments

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

    int result = UtilMessageBoxCatastrophicVA(uText, uTitle, uType, showFileNameInTitle, marker);

    va_end( marker );

    return result;
}
