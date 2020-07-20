// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// Stubs.h
//
// This file contains a template for the default entry point stubs of a COM+
// IL only program.  One can emit these stubs (with some fix-ups) and make
// the code supplied the entry point value for the image.  The fix-ups will
// in turn cause mscoree.dll to be loaded and the correct entry point to be
// called.
//
// Note: Although these stubs contain x86 specific code, they are used
// for all platforms
//
//*****************************************************************************
#ifndef __STUBS_H__
#define __STUBS_H__

//*****************************************************************************
// This stub is designed for a x86 Windows application.  It will call the
// _CorExeMain function in mscoree.dll.  This entry point will in turn load
// and run the IL program.
//
//    jump _CorExeMain();
//
// The code jumps to the imported function _CorExeMain using the iat.
// The address in the template is address of the iat entry which is
// fixed up by the loader when the image is paged in.
//*****************************************************************************

constexpr BYTE ExeMainX86Template[] =
{
	// Jump through IAT to _CorExeMain
	0xFF, 0x25,				// jmp [iat:_CorDllMain entry]
		0x00, 0x00, 0x00, 0x00,		//   address to replace

};

#define ExeMainX86TemplateSize		sizeof(ExeMainX86Template)
#define CorExeMainX86IATOffset		2

//*****************************************************************************
// This stub is designed for a x86 Windows application.  It will call the
// _CorDllMain function in mscoree.dll with with the base entry point for
// the loaded DLL.  This entry point will in turn load and run the IL program.
//
//    jump _CorDllMain
//
// The code jumps to the imported function _CorExeMain using the iat.
// The address in the template is address of the iat entry which is
// fixed up by the loader when the image is paged in.
//*****************************************************************************

constexpr BYTE DllMainX86Template[] =
{
	// Jump through IAT to CorDllMain
	0xFF, 0x25,				// jmp [iat:_CorDllMain entry]
		0x00, 0x00, 0x00, 0x00,		//   address to replace
};

#define DllMainX86TemplateSize		sizeof(DllMainX86Template)
#define CorDllMainX86IATOffset		2

//*****************************************************************************
// This stub is designed for a AMD64 Windows application.  It will call the
// _CorExeMain function in mscoree.dll.  This entry point will in turn load
// and run the IL program.
//
//    mov rax, _CorExeMain();
//    jmp [rax]
//
// The code jumps to the imported function _CorExeMain using the iat.
// The address in the template is address of the iat entry which is
// fixed up by the loader when the image is paged in.
//*****************************************************************************

constexpr BYTE ExeMainAMD64Template[] =
{
	// Jump through IAT to _CorExeMain
	0x48, 0xA1,				// rex.w rex.b mov rax,[following address]
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,//address of iat:_CorExeMain entry
    0xFF, 0xE0              // jmp [rax]
};

#define ExeMainAMD64TemplateSize		sizeof(ExeMainAMD64Template)
#define CorExeMainAMD64IATOffset		2

//*****************************************************************************
// This stub is designed for a AMD64 Windows application.  It will call the
// _CorDllMain function in mscoree.dll with with the base entry point for
// the loaded DLL.  This entry point will in turn load and run the IL program.
//
//    mov rax, _CorDllMain();
//    jmp [rax]
//
// The code jumps to the imported function _CorDllMain using the iat.
// The address in the template is address of the iat entry which is
// fixed up by the loader when the image is paged in.
//*****************************************************************************

constexpr BYTE DllMainAMD64Template[] =
{
	// Jump through IAT to CorDllMain
	0x48, 0xA1,				// rex.w rex.b mov rax,[following address]
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,//address of iat:_CorDllMain entry
    0xFF, 0xE0              // jmp [rax]
};

#define DllMainAMD64TemplateSize		sizeof(DllMainAMD64Template)
#define CorDllMainAMD64IATOffset		2

//*****************************************************************************
// This stub is designed for an ia64 Windows application.  It will call the
// _CorExeMain function in mscoree.dll.  This entry point will in turn load
// and run the IL program.
//
//    jump _CorExeMain();
//
// The code jumps to the imported function _CorExeMain using the iat.
// We set the value of gp to point at the iat table entry for _CorExeMain
//*****************************************************************************

constexpr BYTE ExeMainIA64Template[] =
{
    // ld8    r9  = [gp]    ;;
    // ld8    r10 = [r9],8
    // nop.i                ;;
    // ld8    gp  = [r9]
    // mov    b6  = r10
    // br.cond.sptk.few  b6
    //
    0x0B, 0x48, 0x00, 0x02, 0x18, 0x10, 0xA0, 0x40,
    0x24, 0x30, 0x28, 0x00, 0x00, 0x00, 0x04, 0x00,
    0x10, 0x08, 0x00, 0x12, 0x18, 0x10, 0x60, 0x50,
    0x04, 0x80, 0x03, 0x00, 0x60, 0x00, 0x80, 0x00
};

#define ExeMainIA64TemplateSize		sizeof(ExeMainIA64Template)

//*****************************************************************************
// This stub is designed for an ia64 Windows application.  It will call the
// _CorDllMain function in mscoree.dll with with the base entry point for
// the loaded DLL.  This entry point will in turn load and run the IL program.
//
//    jump _CorDllMain
//
// The code jumps to the imported function _CorExeMain using the iat.
// We set the value of gp to point at the iat table entry for _CorExeMain
//*****************************************************************************

constexpr BYTE DllMainIA64Template[] =
{
    // ld8    r9  = [gp]    ;;
    // ld8    r10 = [r9],8
    // nop.i                ;;
    // ld8    gp  = [r9]
    // mov    b6  = r10
    // br.cond.sptk.few  b6
    //
    0x0B, 0x48, 0x00, 0x02, 0x18, 0x10, 0xA0, 0x40,
    0x24, 0x30, 0x28, 0x00, 0x00, 0x00, 0x04, 0x00,
    0x10, 0x08, 0x00, 0x12, 0x18, 0x10, 0x60, 0x50,
    0x04, 0x80, 0x03, 0x00, 0x60, 0x00, 0x80, 0x00
};

#define DllMainIA64TemplateSize		sizeof(DllMainIA64Template)

#endif  // __STUBS_H__
