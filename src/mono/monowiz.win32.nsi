; =================================================================
; mono.nsi - This NSIS script creates Mono Setup wizard for Windows
;
;            Requires NSIS 2.0 (Nullsoft Scriptable Install System)
;            From http://nsis.sourceforge.net/site/index.php
; =================================================================
;
; (C) Copyright 2003 by Johannes Roith
; (C) Copyright 2003, 2004 by Daniel Morgan
;
; Authors: 
;       Johannes Roith <johannes@jroith.de>
;       Daniel Morgan <danielmorgan@verizon.net>
;
; This .nsi includes code from the NSIS Archives:
; function StrReplace and VersionCheck 
; by Hendri Adriaens
; HendriAdriaens@hotmail.com
; 
; =====================================================
;
; This script can build a binary setup wizard of mono.
; It is released under the GNU GPL.
;
; =====================================================
; SET MILESTONE & SOURCE DIR
; =====================================================
; set by makefile!!
; !define MILESTONE 0.29 
; !define SOURCE_INSTALL_DIR E:\cygwin\home\danmorg\mono029\*.*

!define MILESTONE 0.31
!define SOURCE_INSTALL_DIR c:\b\install\*.* 
;E:\cygwin\home\danmorg\mono029\*.*

; =====================================================
; SET LOGO
; =====================================================
;
;  Beautification:
;
;  This adds a Mono-specific Image on the left
;  You can choose between the light or dark one.
;  
;  If you wish no mono-specifi logo, please outcomment
;  the lines.
;  
;  "light" is enabled.
;
;  !define MUI_SPECIALBITMAP "mono-win32-setup-dark.bmp"
   !define MUI_SPECIALBITMAP "mono-win32-setup-light.bmp"

; =====================================================
; BUILDING
; =====================================================
;
; 1. Build mono to a clean directory prefix.
;
; 2. In your install directory, delete the *.a files.
;     Most people won't need them and it saves ~ 4 MB.
;
; 3. Type "make win32setup"
;
; 4. The output file is mono-[MILESTONE]-win32-1.exe
;
;
; =====================================================
; MONO & REGISTRY / DETECTING MONO
; =====================================================
;
;
; This setup creates several Registry Keys:
;
; HKEY_LOCAL_MACHINE SOFTWARE\Mono DefaultCLR
; HKEY_LOCAL_MACHINE SOFTWARE\Mono\${MILESTONE} SdkInstallRoot
; HKEY_LOCAL_MACHINE SOFTWARE\Mono\${MILESTONE} FrameworkAssemblyDirectory
; HKEY_LOCAL_MACHINE SOFTWARE\Mono\${MILESTONE} MonoConfigDir
;
; =====================================================
;
; To get the current Mono Install Directory:
;
; 1. Get DefaultCLR
; 2. Get HKEY_LOCAL_MACHINE SOFTWARE\Mono\$THE_DEFAULT_CLR_VALUE SdkInstallRoot
;
; =====================================================
;
; To get the current Mono assembly Directory:
;
; 1. Get DefaultCLR
; 2. Get HKEY_LOCAL_MACHINE SOFTWARE\Mono\$THE_DEFAULT_CLR_VALUE FrameworkAssemblyDirectory
; 
; =====================================================
; Do not edit below
; =====================================================
;
;
; =====================================================
; GENERAL SETTING - NEED NOT TO BE CHANGED
; =====================================================

!define NAME "Mono" 
!define TARGET_INSTALL_DIR "$PROGRAMFILES\Mono-${MILESTONE}" 
!define OUTFILE mono-${MILESTONE}-win32-1.exe

Name ${NAME}
Caption "Mono ${MILESTONE} Setup"

!include "MUI.nsh"
!include "Sections.nsh"

SetCompressor bzip2
SilentInstall normal
ShowInstDetails show
SetDateSave on
SetDatablockOptimize on
CRCCheck on
BGGradient 000000 800000 FFFFFF
InstallColors FF8080 000030
XPStyle on
AutoCloseWindow false

; =====================================================
; SCRIPT
; =====================================================

#!define MUI_WELCOMEPAGE
#!define MUI_DIRECTORYPAGE
#!define MUI_DIRECTORYSELECTIONPAGE
 
!define MUI_WELCOMEPAGE_TEXT "This wizard will guide you through the installation of Mono for Windows.\r\n\r\n\r\n$_CLICK"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_LINK "Visit Mono's website for the latest news"
!define MUI_FINISHPAGE_LINK_LOCATION "http://www.go-mono.com/"

!define MUI_FINISHPAGE_NOREBOOTSUPPORT

!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
 
!insertmacro MUI_LANGUAGE "ENGLISH"

AutoCloseWindow false
ShowInstDetails show

OutFile ${OUTFILE}
InstallDir "${TARGET_INSTALL_DIR}"

;========================
; Uninstaller
;========================

Section "Uninstall"

  MessageBox MB_YESNO "Are you sure you want to uninstall Mono from your system?" IDNO NoUnInstall

  Delete $INSTDIR\Uninst.exe ; delete Uninstaller
  DeleteRegKey HKLM SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Mono-${MILESTONE} ; Remove Entry in Software List

  MessageBox MB_YESNO "Mono was installed into $INSTDIR. Should this directory be removed completly?" IDNO GoNext1
  RMDir /r $INSTDIR
  GoNext1:

  DeleteRegKey HKLM SOFTWARE\Mono\${MILESTONE}

  ; If the Default-Key is the current Milestone, we just remove the wrappers

  ReadRegStr $0 HKEY_LOCAL_MACHINE SOFTWARE\Mono\ DefaultCLR
  StrCmp $0 ${MILESTONE} DeleteWrappers

  MessageBox MB_YESNO "Mono ${MILESTONE} has been removed, but the default installation of Mono differs form this version. Should the wrappers and the Mono registry key be still be removed? This could disable other Mono installations." IDNO GoNext2

  DeleteWrappers:

  ; Complete Uninstall

  DeleteRegKey HKLM SOFTWARE\Mono
  Delete $WINDIR\monobasepath.bat
  Delete $WINDIR\mcs.bat
  Delete $WINDIR\mbas.bat
  Delete $WINDIR\mint.bat
  Delete $WINDIR\mono.bat
  Delete $WINDIR\monodis.bat
  Delete $WINDIR\monoilasm.bat
  Delete $WINDIR\sqlsharp.bat
  Delete $WINDIR\secutil.bat
  Delete $WINDIR\cert2spc.bat
  Delete $WINDIR\monoresgen.bat
  Delete $WINDIR\monosn.bat
  Delete $WINDIR\cilc.bat

  GoNext2:
  NoUnInstall:

SectionEnd


 Section

 ; Warn people if a newer Mono is already installed

 ReadRegStr $0 HKEY_LOCAL_MACHINE SOFTWARE\Mono\ DefaultCLR
 Push $0
 Push ${MILESTONE} 
 Call VersionCheck
 Pop $0
 StrCmp $0 0 NoAskInstall
 StrCmp $0 2 NoAskInstall
 MessageBox MB_YESNO "A newer Mono version is already installed. Still continue?" IDNO NoInstall

 NoAskInstall:

 DetailPrint "Installing Mono Files..."
 SetOverwrite on
 SetOutPath $INSTDIR
 File /r ${SOURCE_INSTALL_DIR}

 WriteUninstaller Uninst.exe

 WriteRegStr HKEY_LOCAL_MACHINE SOFTWARE\Mono\${MILESTONE} SdkInstallRoot $INSTDIR
 WriteRegStr HKEY_LOCAL_MACHINE SOFTWARE\Mono\${MILESTONE} FrameworkAssemblyDirectory $INSTDIR\lib
 WriteRegStr HKEY_LOCAL_MACHINE SOFTWARE\Mono\${MILESTONE} MonoConfigDir $INSTDIR\etc\mono
 ;WriteRegStr HKEY_LOCAL_MACHINE SOFTWARE\Mono\${MILESTONE} GtkSharpLibPath $INSTDIR\lib
 WriteRegStr HKEY_LOCAL_MACHINE SOFTWARE\Mono DefaultCLR ${MILESTONE}

 ; Mono Uninstall Entry in Windows Software List in the Control panel
 WriteRegStr HKEY_LOCAL_MACHINE SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Mono-${MILESTONE} DisplayName "Mono ${MILESTONE}"
 WriteRegStr HKEY_LOCAL_MACHINE SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Mono-${MILESTONE} UninstallString $INSTDIR\uninst.exe

 ;original string is like C:\mono-0.20\install
 StrCpy $5 $INSTDIR 
 Push $5
 Push "\" ;search for this string
 Push "/" ;replace with this string
 Call StrReplace
 ;resulting string which is like C:/mono-0.20/install
 Pop $6

;========================
; Write the wrapper files
;========================

; create bin/mono wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\mono.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe "$$@"'
FileClose $0

; create bin/mint wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\mint.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mint.exe "$$@"'
FileClose $0

; create bin/mcs wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\mcs.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe $6/bin/mcs.exe "$$@"'
FileClose $0

; create bin/mbas wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\mbas.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe $6/bin/mbas.exe "$$@"'
FileClose $0

; create bin/sqlsharp wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\sqlsharp.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe $6/bin/sqlsharp.exe "$$@"'
FileClose $0

; create bin/monodis wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\monodis.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe $6/bin/monodis.exe "$$@"'
FileClose $0

; create bin/monoresgen wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\monoresgen.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe $6/bin/monoresgen.exe "$$@"'
FileClose $0

; create bin/monoilasm wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\monoilasm.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe $6/bin/monoilasm.exe "$$@"'
FileClose $0

; create bin/monosn wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\monosn.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe $6/bin/monosn.exe "$$@"'
FileClose $0

; create bin/secutil wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\secutil.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe $6/bin/secutil.exe "$$@"'
FileClose $0

; create bin/cert2spc wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\cert2spc.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe $6/bin/cert2spc.exe "$$@"'
FileClose $0

; create bin/cilc wrapper to be used if the user has cygwin
FileOpen $0 "$INSTDIR\bin\cilc.exe.sh" "w"
FileWrite $0 "#!/bin/sh$\r$\n"
FileWrite $0 "export MONO_PATH=$6/lib$\r$\n"
FileWrite $0 "export MONO_CFG_DIR=$6/etc/mono$\r$\n"
FileWrite $0 '$6/bin/mono.exe $6/bin/cilc.exe "$$@"'
FileClose $0

;
; These wrappers are copied to the windows directory.
;

;========================
; Write the path file
;========================

FileOpen $0 "$WINDIR\monobasepath.bat" "w"
FileWrite $0 'set MONO_BASEPATH="$INSTDIR"$\r$\n'
FileWrite $0 'set MONO_PATH=$INSTDIR\lib$\r$\n'
FileWrite $0 'set MONO_CFG_DIR=$INSTDIR\etc'
FileClose $0


;========================
; Write the mcs file
;========================

FileOpen $0 "$WINDIR\mcs.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\mono.exe" "$INSTDIR\bin\mcs.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0

;========================
; Write the mbas file
;========================

FileOpen $0 "$WINDIR\mbas.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\mono.exe" "$INSTDIR\bin\mbas.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0

;========================
; Write the mint file
;========================

FileOpen $0 "$WINDIR\mint.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\mint.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0

;========================
; Write the mono file
;========================

FileOpen $0 "$WINDIR\mono.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\mono.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"
FileClose $0

;========================
; Write monodis
;========================

FileOpen $0 "$WINDIR\monodis.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\monodis.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0

;========================
; Write monoilasm
;========================

FileOpen $0 "$WINDIR\monoilasm.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\mono.exe" "$INSTDIR\bin\monoilasm.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0


;========================
; Write the sqlsharp file
;========================

FileOpen $0 "$WINDIR\sqlsharp.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\mono.exe" "$INSTDIR\bin\sqlsharp.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0

;========================
; Write the secutil file
;========================

FileOpen $0 "$WINDIR\secutil.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\mono.exe" "$INSTDIR\bin\secutil.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0

;========================
; Write the cert2spc file
;========================

FileOpen $0 "$WINDIR\cert2spc.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\mono.exe" "$INSTDIR\bin\cert2spec.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0


;========================
; Write the monoresgen file
;========================

FileOpen $0 "$WINDIR\monoresgen.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\mono.exe" "$INSTDIR\bin\monoresgen.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0

;========================
; Write the monosn file
;========================

FileOpen $0 "$WINDIR\monosn.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\monosn.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0

;========================
; Write the cilc file
;========================

FileOpen $0 "$WINDIR\cilc.bat" "w"

FileWrite $0 "@echo off$\r$\n"
FileWrite $0 "call monobasepath.bat$\r$\n"
FileWrite $0 "set MONOARGS=$\r$\n"
FileWrite $0 ":loop$\r$\n"
FileWrite $0 "if x%1 == x goto :done$\r$\n"
FileWrite $0 "set MONOARGS=%MONOARGS% %1$\r$\n"
FileWrite $0 "shift$\r$\n"
FileWrite $0 "goto loop$\r$\n"
FileWrite $0 ":done$\r$\n"
FileWrite $0 "setlocal$\r$\n"
FileWrite $0 'set path="$INSTDIR\bin\;$INSTDIR\lib\;$INSTDIR\icu\bin;%path%"$\r$\n'
FileWrite $0 '"$INSTDIR\bin\mono.exe" "$INSTDIR\bin\cilc.exe" %MONOARGS%$\r$\n'
FileWrite $0 "endlocal$\r$\n"

FileClose $0

; ============= glib-2.0.pc ===============
FileOpen $0 "$INSTDIR\lib\pkgconfig\glib-2.0.pc" "w"
FileWrite $0 "prefix=$6$\r$\n"
FileWrite $0 "exec_prefix=$${prefix}$\r$\n"
FileWrite $0 "libdir=$${exec_prefix}/lib$\r$\n"
FileWrite $0 "includedir=$${prefix}/include$\r$\n"
FileWrite $0 "$\r$\n"
FileWrite $0 "glib_genmarshal=glib-genmarshal$\r$\n"
FileWrite $0 "gobject_query=gobject-query$\r$\n"
FileWrite $0 "glib_mkenums=glib-mkenums$\r$\n"
FileWrite $0 "$\r$\n"
FileWrite $0 "Name: GLib$\r$\n"
FileWrite $0 "Description: C Utility Library$\r$\n"
FileWrite $0 "Version: 2.0.4$\r$\n"
FileWrite $0 "Libs: -L$${libdir} -lglib-2.0 -lintl -liconv $\r$\n"
FileWrite $0 "Cflags: -I$${includedir}/glib-2.0 -I$${libdir}/glib-2.0/include $\r$\n"
FileClose $0

; ============= gmodule-2.0.pc ===============
FileOpen $0 "$INSTDIR\lib\pkgconfig\gmodule-2.0.pc" "w"
FileWrite $0 "prefix=$6$\r$\n"
FileWrite $0 "exec_prefix=$${prefix}$\r$\n"
FileWrite $0 "libdir=$${exec_prefix}/lib$\r$\n"
FileWrite $0 "includedir=$${prefix}/include$\r$\n"
FileWrite $0 "$\r$\n"
FileWrite $0 "gmodule_supported=true$\r$\n"
FileWrite $0 "$\r$\n"
FileWrite $0 "Name: GModule$\r$\n"
FileWrite $0 "Description: Dynamic module loader for GLib$\r$\n"
FileWrite $0 "Requires: glib-2.0$\r$\n"
FileWrite $0 "Version: 2.0.4$\r$\n"
FileWrite $0 "Libs: -L$${libdir} -lgmodule-2.0 $\r$\n"
FileWrite $0 "Cflags:$\r$\n"
FileClose $0

; ============= gobject-2.0.pc ===============
FileOpen $0 "$INSTDIR\lib\pkgconfig\gobject-2.0.pc" "w"
FileWrite $0 "prefix=$6$\r$\n"
FileWrite $0 "exec_prefix=$${prefix}$\r$\n"
FileWrite $0 "libdir=$${exec_prefix}/lib$\r$\n"
FileWrite $0 "includedir=$${prefix}/include$\r$\n"
FileWrite $0 "$\r$\n"
FileWrite $0 "Name: GObject$\r$\n"
FileWrite $0 "Description: GLib Type, Object, Parameter and Signal Library$\r$\n"
FileWrite $0 "Requires: glib-2.0$\r$\n"
FileWrite $0 "Version: 2.0.4$\r$\n"
FileWrite $0 "Libs: -L$${libdir} -lgobject-2.0$\r$\n"
FileWrite $0 "Cflags:$\r$\n"
FileClose $0

; ============= gthread-2.0.pc ===============
FileOpen $0 "$INSTDIR\lib\pkgconfig\gthread-2.0.pc" "w"
FileWrite $0 "prefix=$6$\r$\n"
FileWrite $0 "exec_prefix=$${prefix}$\r$\n"
FileWrite $0 "libdir=$${exec_prefix}/lib$\r$\n"
FileWrite $0 "includedir=$${prefix}/include$\r$\n"
FileWrite $0 "$\r$\n"
FileWrite $0 "Name: GThread$\r$\n"
FileWrite $0 "Description: Thread support for GLib$\r$\n"
FileWrite $0 "Requires: glib-2.0$\r$\n"
FileWrite $0 "Version: 2.0.4$\r$\n"
FileWrite $0 "Libs: -L$${libdir} -lgthread-2.0 $\r$\n"
FileWrite $0 "Cflags: -D_REENTRANT$\r$\n"
FileClose $0

; ============= libintl.pc ===============
FileOpen $0 "$INSTDIR\lib\pkgconfig\libintl.pc" "w"
FileWrite $0 "prefix=$6$\r$\n"
FileWrite $0 "exec_prefix=$${prefix}$\r$\n"
FileWrite $0 "libdir=$${exec_prefix}/lib$\r$\n"
FileWrite $0 "includedir=$${prefix}/include$\r$\n"
FileWrite $0 "$\r$\n"
FileWrite $0 "Name: libintl$\r$\n"
FileWrite $0 "Description: The intl library from GNU gettext$\r$\n"
FileWrite $0 "Version: 0.10.40-tml$\r$\n"
FileWrite $0 "Libs: -L$${libdir} -lintl$\r$\n"
FileWrite $0 "Cflags: -I$${includedir}$\r$\n"
FileClose $0

NoInstall:
SectionEnd

; function StrReplace
; by Hendri Adriaens
; HendriAdriaens@hotmail.com
; found in the NSIS Archives
function StrReplace
  Exch $0 ;this will replace wrong characters
  Exch
  Exch $1 ;needs to be replaced
  Exch
  Exch 2
  Exch $2 ;the orginal string
  Push $3 ;counter
  Push $4 ;temp character
  Push $5 ;temp string
  Push $6 ;length of string that need to be replaced
  Push $7 ;length of string that will replace
  Push $R0 ;tempstring
  Push $R1 ;tempstring
  Push $R2 ;tempstring
  StrCpy $3 "-1"
  StrCpy $5 ""
  StrLen $6 $1
  StrLen $7 $0
  Loop:
  IntOp $3 $3 + 1
  StrCpy $4 $2 $6 $3
  StrCmp $4 "" ExitLoop
  StrCmp $4 $1 Replace
  Goto Loop
  Replace:
  StrCpy $R0 $2 $3
  IntOp $R2 $3 + $6
  StrCpy $R1 $2 "" $R2
  StrCpy $2 $R0$0$R1
  IntOp $3 $3 + $7
  Goto Loop
  ExitLoop:
  StrCpy $0 $2
  Pop $R2
  Pop $R1
  Pop $R0
  Pop $7
  Pop $6
  Pop $5
  Pop $4
  Pop $3
  Pop $2
  Pop $1
  Exch $0
FunctionEnd

Function VersionCheck
  Exch $0 ;second versionnumber
  Exch
  Exch $1 ;first versionnumber
  Push $R0 ;counter for $0
  Push $R1 ;counter for $1
  Push $3 ;temp char
  Push $4 ;temp string for $0
  Push $5 ;temp string for $1
  StrCpy $R0 "-1"
  StrCpy $R1 "-1"
  Start:
  StrCpy $4 ""
  DotLoop0:
  IntOp $R0 $R0 + 1
  StrCpy $3 $0 1 $R0
  StrCmp $3 "" DotFound0
  StrCmp $3 "." DotFound0
  StrCpy $4 $4$3
  Goto DotLoop0
  DotFound0:
  StrCpy $5 ""
  DotLoop1:
  IntOp $R1 $R1 + 1
  StrCpy $3 $1 1 $R1
  StrCmp $3 "" DotFound1
  StrCmp $3 "." DotFound1
  StrCpy $5 $5$3
  Goto DotLoop1
  DotFound1:
  Strcmp $4 "" 0 Not4
    StrCmp $5 "" Equal
    Goto Ver2Less
  Not4:
  StrCmp $5 "" Ver2More
  IntCmp $4 $5 Start Ver2Less Ver2More
  Equal:
  StrCpy $0 "0"
  Goto Finish
  Ver2Less:
  StrCpy $0 "1"
  Goto Finish
  Ver2More:
  StrCpy $0 "2"
  Finish:
  Pop $5
  Pop $4
  Pop $3
  Pop $R1
  Pop $R0
  Pop $1
  Exch $0
FunctionEnd
