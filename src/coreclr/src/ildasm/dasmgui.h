//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

BOOL CreateGUI();
void GUISetModule(__in __nullterminated const char *pszModule);
void GUIMainLoop();
void GUIAddOpcode(__inout_opt __nullterminated const char *szString, __in_opt void *GUICookie);
BOOL GUIAddItemsToList();
void GUIAddOpcode(__inout __nullterminated const char *szString);
void DestroyGUI();
UINT GetDasmMBRTLStyle();

BOOL DisassembleMemberByName(__in __nullterminated const char *pszClassName, __in __nullterminated const char *pszMemberName, __in __nullterminated const char *pszSig);
BOOL IsGuiILOnly();
