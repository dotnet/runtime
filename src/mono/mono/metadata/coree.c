/**
 * \file
 * mscoree.dll functions
 *
 * Author:
 *   Kornel Pal <http://www.kornelpal.hu/>
 *
 * Copyright (C) 2008 Kornel Pal
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#ifdef HOST_WIN32

#include <glib.h>
#include <string.h>
#include <mono/utils/mono-path.h>
#include "utils/w32api.h"
#include "cil-coff.h"
#include "metadata-internals.h"
#include "image.h"
#include "image-internals.h"
#include "assembly-internals.h"
#include "domain-internals.h"
#include "appdomain.h"
#include "object.h"
#include "object-internals.h"
#include "loader.h"
#include "threads.h"
#include "environment.h"
#include "coree.h"
#include "coree-internals.h"
#include <mono/utils/w32subset.h>

gchar*
mono_get_module_file_name (HMODULE module_handle)
{
	gunichar2* file_name;
	gchar* file_name_utf8;
	DWORD buffer_size;
	DWORD size;

	buffer_size = 1024;
	file_name = g_new (gunichar2, buffer_size);

	for (;;) {
		size = GetModuleFileName (module_handle, file_name, buffer_size);
		if (!size) {
			g_free (file_name);
			return NULL;
		}

		g_assert (size <= buffer_size);
		if (size != buffer_size)
			break;

		buffer_size += 1024;
		file_name = g_realloc (file_name, buffer_size * sizeof (gunichar2));
	}

	file_name_utf8 = g_utf16_to_utf8 (file_name, size, NULL, NULL, NULL);
	g_free (file_name);

	return file_name_utf8;
}

HMODULE coree_module_handle = NULL;
static gboolean init_from_coree = FALSE;

#if HAVE_API_SUPPORT_WIN32_COREE
#include <shellapi.h>

/* Entry point called by LdrLoadDll of ntdll.dll after _CorValidateImage. */
BOOL STDMETHODCALLTYPE _CorDllMain(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved)
{
	MonoAssembly* assembly;
	MonoImage* image;
	gchar* file_name;
	gchar* error;
	MonoAssemblyLoadContext *alc = mono_alc_get_default ();

	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
		DisableThreadLibraryCalls (hInst);

		file_name = mono_get_module_file_name (hInst);

		if (mono_get_root_domain ()) {
			image = mono_image_open_from_module_handle (alc, hInst, mono_path_resolve_symlinks (file_name), TRUE, NULL);
		} else {
			init_from_coree = TRUE;
			mono_runtime_load (file_name, NULL);
			error = (gchar*) mono_check_corlib_version ();
			if (error) {
				g_free (error);
				g_free (file_name);
				mono_runtime_quit_internal ();
				return FALSE;
			}

			image = mono_image_open (file_name, NULL);
			if (image) {
				image->storage->has_entry_point = TRUE;
				mono_close_exe_image ();
				/* Decrement reference count to zero. (Image will not be closed.) */
				mono_image_close (image);
			}
		}

		if (!image) {
			g_free (file_name);
			return FALSE;
		}

		/*
		 * FIXME: Find a better way to call mono_image_fixup_vtable. Only
		 * loader trampolines should be used and assembly loading should
		 * probably be delayed until the first call to an exported function.
		 */
		if (table_info_get_rows (&image->tables [MONO_TABLE_ASSEMBLY]) && image->image_info->cli_cli_header.ch_vtable_fixups.rva) {
			MonoAssemblyOpenRequest req;
			mono_assembly_request_prepare_open (&req, alc);
			assembly = mono_assembly_request_open (file_name, &req, NULL);
		}

		g_free (file_name);
		break;
	case DLL_PROCESS_DETACH:
		if (lpReserved != NULL)
			/* The process is terminating. */
			return TRUE;
		file_name = mono_get_module_file_name (hInst);
		image = mono_image_loaded_internal (alc, file_name);
		if (image)
			mono_image_close (image);

		g_free (file_name);
		break;
	}

	return TRUE;
}

/* Called by ntdll.dll reagardless of entry point after _CorValidateImage. */
__int32 STDMETHODCALLTYPE _CorExeMain(void)
{
	ERROR_DECL (error);
	MonoDomain* domain;
	MonoAssembly* assembly;
	MonoImage* image;
	MonoMethod* method;
	guint32 entry;
	gchar* file_name;
	gchar* corlib_version_error;
	int argc;
	gunichar2** argvw;
	gchar** argv;
	int i;

	file_name = mono_get_module_file_name (NULL);
	init_from_coree = TRUE;
	domain = mono_runtime_load (file_name, NULL);

	corlib_version_error = (gchar*) mono_check_corlib_version ();
	if (corlib_version_error) {
		g_free (corlib_version_error);
		g_free (file_name);
		MessageBox (NULL, L"Corlib not in sync with this runtime.", NULL, MB_ICONERROR);
		mono_runtime_quit_internal ();
		ExitProcess (1);
	}

	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, mono_alc_get_default ());
	assembly = mono_assembly_request_open (file_name, &req, NULL);
	mono_close_exe_image ();
	if (!assembly) {
		g_free (file_name);
		MessageBox (NULL, L"Cannot open assembly.", NULL, MB_ICONERROR);
		mono_runtime_quit_internal ();
		ExitProcess (1);
	}

	image = assembly->image;
	entry = mono_image_get_entry_point (image);
	if (!entry) {
		g_free (file_name);
		MessageBox (NULL, L"Assembly doesn't have an entry point.", NULL, MB_ICONERROR);
		mono_runtime_quit_internal ();
		ExitProcess (1);
	}

	method = mono_get_method_checked (image, entry, NULL, NULL, error);
	if (method == NULL) {
		g_free (file_name);
		mono_error_cleanup (error); /* FIXME don't swallow the error */
		MessageBox (NULL, L"The entry point method could not be loaded.", NULL, MB_ICONERROR);
		mono_runtime_quit_internal ();
		ExitProcess (1);
	}

	argvw = CommandLineToArgvW (GetCommandLine (), &argc);
	argv = g_new0 (gchar*, argc);
	argv [0] = file_name;
	for (i = 1; i < argc; ++i)
		argv [i] = g_utf16_to_utf8 (argvw [i], -1, NULL, NULL, NULL);
	LocalFree (argvw);

	mono_runtime_run_main_checked (method, argc, argv, error);
	mono_error_raise_exception_deprecated (error); /* OK, triggers unhandled exn handler */
	mono_thread_manage_internal ();

	mono_runtime_quit_internal ();

	/* return does not terminate the process. */
	ExitProcess (mono_environment_exitcode_get ());
}

/* Called by msvcrt.dll when shutting down. */
void STDMETHODCALLTYPE CorExitProcess(int exitCode)
{
	/* FIXME: This is not currently supported by the runtime. */
#if 0
	if (mono_get_root_domain () && !mono_runtime_is_shutting_down ()) {
		mono_runtime_set_shutting_down ();
		mono_thread_suspend_all_other_threads ();
		mono_runtime_quit_internal ();
	}
#endif
	ExitProcess (exitCode);
}

/* Called by ntdll.dll before _CorDllMain and _CorExeMain. */
STDAPI _CorValidateImage(PVOID *ImageBase, LPCWSTR FileName)
{
	IMAGE_DOS_HEADER* DosHeader;
	IMAGE_NT_HEADERS32* NtHeaders32;
	IMAGE_DATA_DIRECTORY* CliHeaderDir;
#ifdef _WIN64
	IMAGE_NT_HEADERS64* NtHeaders64;
	MonoCLIHeader* CliHeader;
	DWORD SizeOfHeaders;
#endif
	DWORD* Address;
	DWORD OldProtect;

	DosHeader = (IMAGE_DOS_HEADER*)*ImageBase;
	if (DosHeader->e_magic != IMAGE_DOS_SIGNATURE)
		return STATUS_INVALID_IMAGE_FORMAT;

	NtHeaders32 = (IMAGE_NT_HEADERS32*)((DWORD_PTR)DosHeader + DosHeader->e_lfanew);
	if (NtHeaders32->Signature != IMAGE_NT_SIGNATURE)
		return STATUS_INVALID_IMAGE_FORMAT;

#ifdef _WIN64
	NtHeaders64 = (IMAGE_NT_HEADERS64*)NtHeaders32;
	if (NtHeaders64->OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC)
	{
		if (NtHeaders64->OptionalHeader.NumberOfRvaAndSizes <= IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR)
			return STATUS_INVALID_IMAGE_FORMAT;

		CliHeaderDir = &NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR];
		if (!CliHeaderDir->VirtualAddress)
			return STATUS_INVALID_IMAGE_FORMAT;

		CliHeader = (MonoCLIHeader*)((DWORD_PTR)DosHeader + CliHeaderDir->VirtualAddress);
		if (CliHeader->ch_flags & CLI_FLAGS_32BITREQUIRED)
			return STATUS_INVALID_IMAGE_FORMAT;

		if (CliHeader->ch_flags & CLI_FLAGS_ILONLY)
		{
			/* Avoid calling _CorDllMain because imports are not resolved for IL only images. */
			if (NtHeaders64->OptionalHeader.AddressOfEntryPoint != 0)
			{
				Address = &NtHeaders64->OptionalHeader.AddressOfEntryPoint;
				if (!VirtualProtect(Address, sizeof(DWORD), PAGE_READWRITE, &OldProtect))
					return E_UNEXPECTED;
				*Address = (DWORD)0;
				if (!VirtualProtect(Address, sizeof(DWORD), OldProtect, &OldProtect))
					return E_UNEXPECTED;
			}
		}

		return STATUS_SUCCESS;
	}

	if (NtHeaders32->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR32_MAGIC)
		return STATUS_INVALID_IMAGE_FORMAT;

	if (NtHeaders32->OptionalHeader.NumberOfRvaAndSizes <= IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR)
		return STATUS_INVALID_IMAGE_FORMAT;

	CliHeaderDir = &NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR];
	if (!CliHeaderDir->VirtualAddress)
		return STATUS_INVALID_IMAGE_FORMAT;

	CliHeader = (MonoCLIHeader*)((DWORD_PTR)DosHeader + CliHeaderDir->VirtualAddress);
	if (!(CliHeader->ch_flags & CLI_FLAGS_ILONLY) || (CliHeader->ch_flags & CLI_FLAGS_32BITREQUIRED))
		return STATUS_INVALID_IMAGE_FORMAT;

	/* Fixup IMAGE_NT_HEADERS32 to IMAGE_NT_HEADERS64. */
	SizeOfHeaders = NtHeaders32->OptionalHeader.SizeOfHeaders;
	if (SizeOfHeaders < DosHeader->e_lfanew + sizeof(IMAGE_NT_HEADERS64) + (sizeof(IMAGE_SECTION_HEADER) * NtHeaders32->FileHeader.NumberOfSections))
		return STATUS_INVALID_IMAGE_FORMAT;

	if (!VirtualProtect(DosHeader, SizeOfHeaders, PAGE_READWRITE, &OldProtect))
		return E_UNEXPECTED;

	memmove(NtHeaders64 + 1, IMAGE_FIRST_SECTION(NtHeaders32), sizeof(IMAGE_SECTION_HEADER) * NtHeaders32->FileHeader.NumberOfSections);

	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES - 1].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES - 1].VirtualAddress = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].Size = NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].Size;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress = NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT].VirtualAddress = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IAT].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IAT].VirtualAddress = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT].VirtualAddress = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG].VirtualAddress = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS].VirtualAddress = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_GLOBALPTR].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_GLOBALPTR].VirtualAddress = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_ARCHITECTURE].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_ARCHITECTURE].VirtualAddress = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].Size = NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].Size;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].VirtualAddress = NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].VirtualAddress;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC].Size = NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC].Size;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC].VirtualAddress = NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC].VirtualAddress;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY].Size = NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY].Size;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY].VirtualAddress = NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY].VirtualAddress;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION].VirtualAddress = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].Size = NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].Size;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress = NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].Size = 0;
	NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress = 0;

	NtHeaders64->OptionalHeader.NumberOfRvaAndSizes = IMAGE_NUMBEROF_DIRECTORY_ENTRIES;
	NtHeaders64->OptionalHeader.LoaderFlags = NtHeaders32->OptionalHeader.LoaderFlags;
	NtHeaders64->OptionalHeader.SizeOfHeapCommit = (ULONGLONG)NtHeaders32->OptionalHeader.SizeOfHeapCommit;
	NtHeaders64->OptionalHeader.SizeOfHeapReserve = (ULONGLONG)NtHeaders32->OptionalHeader.SizeOfHeapReserve;
	NtHeaders64->OptionalHeader.SizeOfStackCommit = (ULONGLONG)NtHeaders32->OptionalHeader.SizeOfStackCommit;
	NtHeaders64->OptionalHeader.SizeOfStackReserve = (ULONGLONG)NtHeaders32->OptionalHeader.SizeOfStackReserve;
	NtHeaders64->OptionalHeader.DllCharacteristics = NtHeaders32->OptionalHeader.DllCharacteristics;
	NtHeaders64->OptionalHeader.Subsystem = NtHeaders32->OptionalHeader.Subsystem;
	NtHeaders64->OptionalHeader.CheckSum = NtHeaders32->OptionalHeader.CheckSum;
	NtHeaders64->OptionalHeader.SizeOfHeaders = NtHeaders32->OptionalHeader.SizeOfHeaders;
	NtHeaders64->OptionalHeader.SizeOfImage = NtHeaders32->OptionalHeader.SizeOfImage;
	NtHeaders64->OptionalHeader.Win32VersionValue = NtHeaders32->OptionalHeader.Win32VersionValue;
	NtHeaders64->OptionalHeader.MinorSubsystemVersion = NtHeaders32->OptionalHeader.MinorSubsystemVersion;
	NtHeaders64->OptionalHeader.MajorSubsystemVersion = NtHeaders32->OptionalHeader.MajorSubsystemVersion;
	NtHeaders64->OptionalHeader.MinorImageVersion = NtHeaders32->OptionalHeader.MinorImageVersion;
	NtHeaders64->OptionalHeader.MajorImageVersion = NtHeaders32->OptionalHeader.MajorImageVersion;
	NtHeaders64->OptionalHeader.MinorOperatingSystemVersion = NtHeaders32->OptionalHeader.MinorOperatingSystemVersion;
	NtHeaders64->OptionalHeader.MajorOperatingSystemVersion = NtHeaders32->OptionalHeader.MajorOperatingSystemVersion;
	NtHeaders64->OptionalHeader.FileAlignment = NtHeaders32->OptionalHeader.FileAlignment;
	NtHeaders64->OptionalHeader.SectionAlignment = NtHeaders32->OptionalHeader.SectionAlignment;
	NtHeaders64->OptionalHeader.ImageBase = (ULONGLONG)NtHeaders32->OptionalHeader.ImageBase;
	/* BaseOfCode is at the same offset. */
	NtHeaders64->OptionalHeader.AddressOfEntryPoint = 0;
	NtHeaders64->OptionalHeader.Magic = IMAGE_NT_OPTIONAL_HDR64_MAGIC;
	NtHeaders64->FileHeader.SizeOfOptionalHeader = sizeof(IMAGE_OPTIONAL_HEADER64);

	if (!VirtualProtect(DosHeader, SizeOfHeaders, OldProtect, &OldProtect))
		return E_UNEXPECTED;
#else
	if (NtHeaders32->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR32_MAGIC)
		return STATUS_INVALID_IMAGE_FORMAT;

	if (NtHeaders32->OptionalHeader.NumberOfRvaAndSizes <= IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR)
		return STATUS_INVALID_IMAGE_FORMAT;

	CliHeaderDir = &NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR];
	if (!CliHeaderDir->VirtualAddress)
		return STATUS_INVALID_IMAGE_FORMAT;

	Address = &NtHeaders32->OptionalHeader.AddressOfEntryPoint;
	if (!VirtualProtect(Address, sizeof(DWORD), PAGE_READWRITE, &OldProtect))
		return E_UNEXPECTED;
	if (NtHeaders32->FileHeader.Characteristics & IMAGE_FILE_DLL)
		*Address = (DWORD)((DWORD_PTR)&_CorDllMain - (DWORD_PTR)DosHeader);
	else
		*Address = (DWORD)((DWORD_PTR)&_CorExeMain - (DWORD_PTR)DosHeader);
	if (!VirtualProtect(Address, sizeof(DWORD), OldProtect, &OldProtect))
		return E_UNEXPECTED;
#endif

	return STATUS_SUCCESS;
}

/* Called by ntdll.dll. */
STDAPI_(VOID) _CorImageUnloading(PVOID ImageBase)
{
	/* Nothing to do. */
}

STDAPI CorBindToRuntimeEx(LPCWSTR pwszVersion, LPCWSTR pwszBuildFlavor, DWORD startupFlags, REFCLSID rclsid, REFIID riid, LPVOID FAR *ppv)
{
	if (ppv == NULL)
		return E_POINTER;

	*ppv = NULL;
	return E_NOTIMPL;
}

STDAPI CorBindToRuntime(LPCWSTR pwszVersion, LPCWSTR pwszBuildFlavor, REFCLSID rclsid, REFIID riid, LPVOID FAR *ppv)
{
	return CorBindToRuntimeEx (pwszVersion, pwszBuildFlavor, 0, rclsid, riid, ppv);
}

HMODULE WINAPI MonoLoadImage(LPCWSTR FileName)
{
	HANDLE FileHandle;
	DWORD FileSize;
	HANDLE MapHandle;
	IMAGE_DOS_HEADER* DosHeader;
	IMAGE_NT_HEADERS32* NtHeaders32;
#ifdef _WIN64
	IMAGE_NT_HEADERS64* NtHeaders64;
#endif
	HMODULE ModuleHandle;

	FileHandle = CreateFile(FileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
	if (FileHandle == INVALID_HANDLE_VALUE)
		return NULL;

	FileSize = GetFileSize(FileHandle, NULL); 
	if (FileSize == INVALID_FILE_SIZE)
		goto CloseFile;

	MapHandle = CreateFileMapping(FileHandle, NULL, PAGE_READONLY, 0, 0, NULL);
	if (MapHandle == NULL)
		goto CloseFile;

	DosHeader = (IMAGE_DOS_HEADER*)MapViewOfFile(MapHandle, FILE_MAP_READ, 0, 0, 0);
	if (DosHeader == NULL)
		goto CloseMap;

	if (FileSize < sizeof(IMAGE_DOS_HEADER) || DosHeader->e_magic != IMAGE_DOS_SIGNATURE || FileSize < DosHeader->e_lfanew + sizeof(IMAGE_NT_HEADERS32))
		goto InvalidImageFormat;

	NtHeaders32 = (IMAGE_NT_HEADERS32*)((DWORD_PTR)DosHeader + DosHeader->e_lfanew);
	if (NtHeaders32->Signature != IMAGE_NT_SIGNATURE)
		goto InvalidImageFormat;

#ifdef _WIN64
	NtHeaders64 = (IMAGE_NT_HEADERS64*)NtHeaders32;
	if (NtHeaders64->OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC)
	{
		if (FileSize < DosHeader->e_lfanew + sizeof(IMAGE_NT_HEADERS64) ||
			NtHeaders64->OptionalHeader.NumberOfRvaAndSizes <= IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR ||
			!NtHeaders64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress)
				goto InvalidImageFormat;

		goto ValidImage;
	}
#endif

	if (NtHeaders32->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR32_MAGIC ||
		NtHeaders32->OptionalHeader.NumberOfRvaAndSizes <= IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR ||
		!NtHeaders32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress)
	{
InvalidImageFormat:
		SetLastError(STATUS_INVALID_IMAGE_FORMAT);
		goto UnmapView;
	}

#ifdef _WIN64
ValidImage:
#endif
	UnmapViewOfFile(DosHeader);
	CloseHandle(MapHandle);

	ModuleHandle = LoadLibrary(FileName);

	CloseHandle(FileHandle);
	return ModuleHandle;

UnmapView:
	UnmapViewOfFile(DosHeader);
CloseMap:
	CloseHandle(MapHandle);
CloseFile:
	CloseHandle(FileHandle);
	return NULL;
}

typedef struct _EXPORT_FIXUP
{
	LPCSTR Name;
	union
	{
		PVOID Pointer;
		DWORD_PTR DWordPtr;
		BYTE Bytes[sizeof(PVOID)];
#ifdef _M_IA64
		PLABEL_DESCRIPTOR* PLabel;
#endif
	} ProcAddress;
} EXPORT_FIXUP;

/* Has to be binary ordered. */
static const EXPORT_FIXUP ExportFixups[] = {
	{"CorBindToRuntime", {(PVOID)&CorBindToRuntime}},
	{"CorBindToRuntimeEx", {(PVOID)&CorBindToRuntimeEx}},
	{"CorExitProcess", {(PVOID)&CorExitProcess}},
	{"_CorDllMain", {(PVOID)&_CorDllMain}},
	{"_CorExeMain", {(PVOID)&_CorExeMain}},
	{"_CorImageUnloading", {(PVOID)&_CorImageUnloading}},
	{"_CorValidateImage", {(PVOID)&_CorValidateImage}},
	{NULL, {NULL}}
};

#define EXPORT_FIXUP_COUNT (sizeof(ExportFixups) / sizeof(EXPORT_FIXUP) - 1)

static HMODULE ExportFixupModuleHandle = NULL;
static DWORD ExportFixupRvas[EXPORT_FIXUP_COUNT];

/* Fixup exported functions of mscoree.dll to our implementations. */
STDAPI MonoFixupCorEE(HMODULE ModuleHandle)
{
	IMAGE_DOS_HEADER* DosHeader;
	IMAGE_NT_HEADERS* NtHeaders;
	IMAGE_DATA_DIRECTORY* ExportDataDir;
	IMAGE_EXPORT_DIRECTORY* ExportDir;
	DWORD* Functions;
	DWORD* Names;
	WORD* NameOrdinals;
	EXPORT_FIXUP* ExportFixup;
	DWORD* ExportFixupRva;
	DWORD* Address;
	DWORD OldProtect;
	DWORD ProcRva;
	DWORD i;
	int cmp;
#ifdef _WIN64
	MEMORY_BASIC_INFORMATION MemoryInfo;
	PVOID Region;
	PVOID RegionBase;
	PVOID MaxRegionBase;
#ifdef _M_IA64
	PLABEL_DESCRIPTOR* PLabel;

#define ELEMENT_SIZE sizeof(PLABEL_DESCRIPTOR)
#define REGION_WRITE_PROTECT PAGE_READWRITE
#define REGION_PROTECT PAGE_READ
#else
	BYTE* Trampoline;

#define ELEMENT_SIZE 13
#define REGION_WRITE_PROTECT PAGE_EXECUTE_READWRITE
#define REGION_PROTECT PAGE_EXECUTE_READ
#endif
#endif

	if (ExportFixupModuleHandle != NULL)
		return ModuleHandle == ExportFixupModuleHandle ? S_OK : E_FAIL;

	DosHeader = (IMAGE_DOS_HEADER*)ModuleHandle;
	if (DosHeader == NULL)
		return E_POINTER;

	if (DosHeader->e_magic != IMAGE_DOS_SIGNATURE)
		return E_INVALIDARG;

	NtHeaders = (IMAGE_NT_HEADERS*)((DWORD_PTR)DosHeader + DosHeader->e_lfanew);
	if (NtHeaders->Signature != IMAGE_NT_SIGNATURE)
		return E_INVALIDARG;

	if (NtHeaders->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR_MAGIC)
		return E_INVALIDARG;

	if (NtHeaders->OptionalHeader.NumberOfRvaAndSizes <= IMAGE_DIRECTORY_ENTRY_EXPORT)
		return E_FAIL;

	ExportDataDir = &NtHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
	if (!ExportDataDir->VirtualAddress)
		return E_FAIL;

#ifdef _WIN64
	/* Allocate memory after base address because RVAs are 32-bit unsigned integers. */
	RegionBase = DosHeader;
	MaxRegionBase = (PVOID)((DWORD_PTR)RegionBase + (DWORD_PTR)(0x100000000L - (ELEMENT_SIZE * (EXPORT_FIXUP_COUNT - 1))));
	for (;;)
	{
		if (!VirtualQuery(RegionBase, &MemoryInfo, sizeof(MEMORY_BASIC_INFORMATION)))
			return E_UNEXPECTED;
		if (MemoryInfo.State == MEM_FREE && MemoryInfo.RegionSize >= ELEMENT_SIZE * EXPORT_FIXUP_COUNT)
		{
			Region = VirtualAlloc(RegionBase, ELEMENT_SIZE * EXPORT_FIXUP_COUNT, MEM_COMMIT | MEM_RESERVE, REGION_WRITE_PROTECT);
			if (Region != NULL)
				break;
		}
		RegionBase = (PVOID)((DWORD_PTR)MemoryInfo.BaseAddress + (DWORD_PTR)MemoryInfo.RegionSize);
		if (RegionBase > MaxRegionBase)
			return E_OUTOFMEMORY;
	}

#ifdef _M_IA64
	PLabel = (PLABEL_DESCRIPTOR*)Region;
#else
	Trampoline = (BYTE*)Region;
#endif
#endif

	ExportDir = (IMAGE_EXPORT_DIRECTORY*)((DWORD_PTR)DosHeader + ExportDataDir->VirtualAddress);
	Functions = (DWORD*)((DWORD_PTR)DosHeader + ExportDir->AddressOfFunctions);
	Names = (DWORD*)((DWORD_PTR)DosHeader + ExportDir->AddressOfNames);
	NameOrdinals = (WORD*)((DWORD_PTR)DosHeader + ExportDir->AddressOfNameOrdinals);
	ExportFixup = (EXPORT_FIXUP*)&ExportFixups;
	ExportFixupRva = (DWORD*)&ExportFixupRvas;

	for (i = 0; i < ExportDir->NumberOfNames; i++)
	{
		cmp = strcmp((LPCSTR)((DWORD_PTR)DosHeader + Names[i]), ExportFixup->Name);
		if (cmp > 0)
			return E_FAIL;

		if (cmp == 0)
		{
#ifdef _WIN64
#if defined(_M_IA64)
			ProcRva = (DWORD)((DWORD_PTR)PLabel - (DWORD_PTR)DosHeader);
			*(PLabel)++ = *ExportFixup->ProcAddress.PLabel;
#elif defined(_M_X64)
			ProcRva = (DWORD)((DWORD_PTR)Trampoline - (DWORD_PTR)DosHeader);
			/* mov r11, ExportFixup->ProcAddress */
			*(Trampoline)++ = 0x49;
			*(Trampoline)++ = 0xBB;
			*(Trampoline)++ = ExportFixup->ProcAddress.Bytes[0];
			*(Trampoline)++ = ExportFixup->ProcAddress.Bytes[1];
			*(Trampoline)++ = ExportFixup->ProcAddress.Bytes[2];
			*(Trampoline)++ = ExportFixup->ProcAddress.Bytes[3];
			*(Trampoline)++ = ExportFixup->ProcAddress.Bytes[4];
			*(Trampoline)++ = ExportFixup->ProcAddress.Bytes[5];
			*(Trampoline)++ = ExportFixup->ProcAddress.Bytes[6];
			*(Trampoline)++ = ExportFixup->ProcAddress.Bytes[7];
			/* jmp r11 */
			*(Trampoline)++ = 0x41;
			*(Trampoline)++ = 0xFF;
			*(Trampoline)++ = 0xE3;
#else
#error Unsupported architecture.
#endif
#else
			ProcRva = (DWORD)(ExportFixup->ProcAddress.DWordPtr - (DWORD_PTR)DosHeader);
#endif
			Address = &Functions[NameOrdinals[i]];
			if (!VirtualProtect(Address, sizeof(DWORD), PAGE_READWRITE, &OldProtect))
				return E_UNEXPECTED;
			*ExportFixupRva = *Address;
			*Address = ProcRva;
			if (!VirtualProtect(Address, sizeof(DWORD), OldProtect, &OldProtect))
				return E_UNEXPECTED;
			ExportFixup++;
			if (ExportFixup->Name == NULL) {
#ifdef _WIN64
				if (!VirtualProtect(Region, ELEMENT_SIZE * EXPORT_FIXUP_COUNT, REGION_PROTECT, &OldProtect))
					return E_UNEXPECTED;
#endif

				ExportFixupModuleHandle = ModuleHandle;
				return S_OK;
			}
			ExportFixupRva++;
		}
	}
	return E_FAIL;
}

/* Executable images are only mapped by the OS loader. We need to do fixups for native code support. */
STDAPI MonoFixupExe(HMODULE ModuleHandle)
{
	IMAGE_DOS_HEADER* DosHeader;
	IMAGE_NT_HEADERS* NtHeaders;
	DWORD_PTR* Address;
	DWORD OldProtect;
	DWORD_PTR BaseDiff;

	DosHeader = (IMAGE_DOS_HEADER*)ModuleHandle;
	if (DosHeader == NULL)
		return E_POINTER;

	if (DosHeader->e_magic != IMAGE_DOS_SIGNATURE)
		return E_INVALIDARG;

	NtHeaders = (IMAGE_NT_HEADERS*)((DWORD_PTR)DosHeader + DosHeader->e_lfanew);
	if (NtHeaders->Signature != IMAGE_NT_SIGNATURE)
		return E_INVALIDARG;

	if (NtHeaders->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR_MAGIC)
		return E_INVALIDARG;

	if (NtHeaders->FileHeader.Characteristics & IMAGE_FILE_DLL)
		return S_OK;

	if (NtHeaders->OptionalHeader.NumberOfRvaAndSizes > IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR)
	{
		IMAGE_DATA_DIRECTORY* CliHeaderDir;
		MonoCLIHeader* CliHeader;

		CliHeaderDir = &NtHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR];
		if (CliHeaderDir->VirtualAddress)
		{
			CliHeader = (MonoCLIHeader*)((DWORD_PTR)DosHeader + CliHeaderDir->VirtualAddress);
			if (CliHeader->ch_flags & CLI_FLAGS_ILONLY)
				return S_OK;
		}
	}

	BaseDiff = (DWORD_PTR)DosHeader - NtHeaders->OptionalHeader.ImageBase;
	if (BaseDiff != 0)
	{
		if (NtHeaders->FileHeader.Characteristics & IMAGE_FILE_RELOCS_STRIPPED)
			return E_FAIL;

		Address = &NtHeaders->OptionalHeader.ImageBase;
		if (!VirtualProtect(Address, sizeof(DWORD_PTR), PAGE_READWRITE, &OldProtect))
			return E_UNEXPECTED;
		*Address = (DWORD_PTR)DosHeader;
		if (!VirtualProtect(Address, sizeof(DWORD_PTR), OldProtect, &OldProtect))
			return E_UNEXPECTED;

		if (NtHeaders->OptionalHeader.NumberOfRvaAndSizes > IMAGE_DIRECTORY_ENTRY_BASERELOC)
		{
			IMAGE_DATA_DIRECTORY* BaseRelocDir;
			IMAGE_BASE_RELOCATION* BaseReloc;
			USHORT* RelocBlock;
			ULONG BaseRelocSize;
			ULONG RelocBlockSize;
			USHORT RelocOffset;
			DWORD_PTR UNALIGNED *RelocFixup;

			BaseRelocDir = &NtHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC];
			if (BaseRelocDir->VirtualAddress)
			{
				BaseReloc = (IMAGE_BASE_RELOCATION*)((DWORD_PTR)DosHeader + BaseRelocDir->VirtualAddress);
				BaseRelocSize = BaseRelocDir->Size;

				while (BaseRelocSize)
				{
					RelocBlockSize = BaseReloc->SizeOfBlock;

					if (!RelocBlockSize || BaseRelocSize < RelocBlockSize)
						return E_FAIL;

					BaseRelocSize -= RelocBlockSize;
					RelocBlock = (USHORT*)((DWORD_PTR)BaseReloc + sizeof(IMAGE_BASE_RELOCATION));
					RelocBlockSize -= sizeof(IMAGE_BASE_RELOCATION);
					RelocBlockSize /= sizeof(USHORT);

					while (RelocBlockSize-- != 0)
					{
						RelocOffset = *RelocBlock & (USHORT)0x0fff;
						RelocFixup = (DWORD_PTR*)((DWORD_PTR)DosHeader + BaseReloc->VirtualAddress + RelocOffset);

						switch (*RelocBlock >> 12)
						{
							case IMAGE_REL_BASED_ABSOLUTE:
								break;

#ifdef _WIN64
							case IMAGE_REL_BASED_DIR64:
#else
							case IMAGE_REL_BASED_HIGHLOW:
#endif
								if (!VirtualProtect(RelocFixup, sizeof(DWORD_PTR), PAGE_EXECUTE_READWRITE, &OldProtect))
									return E_UNEXPECTED;
								*RelocFixup += BaseDiff;
								if (!VirtualProtect(RelocFixup, sizeof(DWORD_PTR), OldProtect, &OldProtect))
									return E_UNEXPECTED;
								break;

							default:
								return E_FAIL;
						}

						RelocBlock++;
					}
					BaseReloc = (IMAGE_BASE_RELOCATION*)RelocBlock;
				}
			}
		}
	}

	if (NtHeaders->OptionalHeader.NumberOfRvaAndSizes > IMAGE_DIRECTORY_ENTRY_IMPORT)
	{
		IMAGE_DATA_DIRECTORY* ImportDir;
		IMAGE_IMPORT_DESCRIPTOR* ImportDesc;
		HMODULE ImportModuleHandle;
		IMAGE_THUNK_DATA* ImportThunkData;
		DWORD_PTR ProcAddress;

		ImportDir = &NtHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
		if (ImportDir->VirtualAddress != 0)
		{
			ImportDesc = (IMAGE_IMPORT_DESCRIPTOR*)((DWORD_PTR)DosHeader + ImportDir->VirtualAddress);
			while (ImportDesc->Name && ImportDesc->OriginalFirstThunk)
			{
				gchar *file_utf8 = (gchar *)((DWORD_PTR)DosHeader + ImportDesc->Name);

				gunichar2 *file_utf16 = g_utf8_to_utf16 (file_utf8, (glong)strlen (file_utf8), NULL, NULL, NULL);
				ImportModuleHandle = NULL;
				if (file_utf16 != NULL) {
					ImportModuleHandle = LoadLibraryW(file_utf16);
					g_free (file_utf16);
				}

				if (ImportModuleHandle == NULL)
					return E_FAIL;

				ImportThunkData = (IMAGE_THUNK_DATA*)((DWORD_PTR)DosHeader + ImportDesc->OriginalFirstThunk);
				while (ImportThunkData->u1.Ordinal != 0)
				{
					if (IMAGE_SNAP_BY_ORDINAL(ImportThunkData->u1.Ordinal))
						ProcAddress = (DWORD_PTR)GetProcAddress(ImportModuleHandle, (LPCSTR)IMAGE_ORDINAL(ImportThunkData->u1.Ordinal));
					else
					{
						IMAGE_IMPORT_BY_NAME* ImportByName = (IMAGE_IMPORT_BY_NAME*)((DWORD_PTR)DosHeader + ImportThunkData->u1.AddressOfData);
						ProcAddress = (DWORD_PTR)GetProcAddress(ImportModuleHandle, (PCSTR)ImportByName->Name);
					}
					if (ProcAddress == 0)
						return E_FAIL;
					Address = (DWORD_PTR*)((DWORD_PTR)ImportThunkData - ImportDesc->OriginalFirstThunk + ImportDesc->FirstThunk);
					if (!VirtualProtect(Address, sizeof(DWORD_PTR), PAGE_READWRITE, &OldProtect))
						return E_UNEXPECTED;
					*Address = ProcAddress;
					if (!VirtualProtect(Address, sizeof(DWORD_PTR), OldProtect, &OldProtect))
						return E_UNEXPECTED;
					ImportThunkData++;
				}

				ImportDesc++;
			}
		}
	}

	return S_OK;
}

void
mono_coree_set_act_ctx (const char* file_name)
{
	typedef HANDLE (WINAPI* CREATEACTCTXW_PROC) (PCACTCTXW pActCtx);
	typedef BOOL (WINAPI* ACTIVATEACTCTX_PROC) (HANDLE hActCtx, ULONG_PTR* lpCookie);

	HMODULE kernel32_handle;
	CREATEACTCTXW_PROC CreateActCtx_proc;
	ACTIVATEACTCTX_PROC ActivateActCtx_proc;
	gchar* full_path;
	gunichar2* full_path_utf16;
	gchar* dir_name;
	gunichar2* dir_name_utf16;
	gchar* base_name;
	gunichar2* base_name_utf16;
	ACTCTX act_ctx;
	HANDLE handle;
	ULONG_PTR cookie;

	kernel32_handle = GetModuleHandle (L"kernel32.dll");
	if (!kernel32_handle)
		return;
	CreateActCtx_proc = (CREATEACTCTXW_PROC) GetProcAddress (kernel32_handle, "CreateActCtxW");
	if (!CreateActCtx_proc)
		return;
	ActivateActCtx_proc = (ACTIVATEACTCTX_PROC) GetProcAddress (kernel32_handle, "ActivateActCtx");
	if (!ActivateActCtx_proc)
		return;

	full_path = mono_path_canonicalize (file_name);
	full_path_utf16 = g_utf8_to_utf16 (full_path, -1, NULL, NULL, NULL);
	dir_name = g_path_get_dirname (full_path);
	dir_name_utf16 = g_utf8_to_utf16 (dir_name, -1, NULL, NULL, NULL);
	base_name = g_path_get_basename (full_path);
	base_name_utf16 = g_utf8_to_utf16 (base_name, -1, NULL, NULL, NULL);
	g_free (base_name);
	g_free (dir_name);
	g_free (full_path);

	memset (&act_ctx, 0, sizeof (ACTCTX));
	act_ctx.cbSize = sizeof (ACTCTX);
	act_ctx.dwFlags = ACTCTX_FLAG_SET_PROCESS_DEFAULT | ACTCTX_FLAG_ASSEMBLY_DIRECTORY_VALID | ACTCTX_FLAG_RESOURCE_NAME_VALID | ACTCTX_FLAG_APPLICATION_NAME_VALID;
	act_ctx.lpSource = full_path_utf16;
	act_ctx.lpAssemblyDirectory = dir_name_utf16;
	act_ctx.lpResourceName = CREATEPROCESS_MANIFEST_RESOURCE_ID;
	act_ctx.lpApplicationName = base_name_utf16;

	handle = CreateActCtx_proc (&act_ctx);
	if (handle == INVALID_HANDLE_VALUE && GetLastError () == ERROR_SXS_PROCESS_DEFAULT_ALREADY_SET) {
		act_ctx.dwFlags &= ~ACTCTX_FLAG_SET_PROCESS_DEFAULT;
		handle = CreateActCtx_proc (&act_ctx);
	}

	g_free (base_name_utf16);
	g_free (dir_name_utf16);
	g_free (full_path_utf16);

	if (handle != INVALID_HANDLE_VALUE)
		ActivateActCtx_proc (handle, &cookie);
}

void
mono_load_coree (const char* exe_file_name)
{
	HMODULE module_handle;
	gunichar2* file_name;
	UINT required_size;
	UINT size;

	if (coree_module_handle)
		return;

	// No GC safe transition because this is called early in driver.c
	if (!init_from_coree && exe_file_name)
		mono_coree_set_act_ctx (exe_file_name);

	/* ntdll.dll loads mscoree.dll from the system32 directory. */
	required_size = GetSystemDirectory (NULL, 0);
	file_name = g_new (gunichar2, required_size + 12);
	size = GetSystemDirectory (file_name, required_size);
	g_assert (size < required_size);
	if (file_name [size - 1] != L'\\')
		file_name [size++] = L'\\';
	memcpy (&file_name [size], L"mscoree.dll", 12 * sizeof (gunichar2));

	module_handle = LoadLibrary (file_name);
	g_free (file_name);

	if (module_handle && !SUCCEEDED (MonoFixupCorEE (module_handle))) {
		FreeLibrary (module_handle);
		module_handle = NULL;
	}

	coree_module_handle = module_handle;
}

void
mono_fixup_exe_image (MonoImage* image)
{
	if (!init_from_coree && image && m_image_is_module_handle (image))
		MonoFixupExe ((HMODULE) image->raw_data);
}

#elif !HAVE_EXTERN_DEFINED_WIN32_COREE
BOOL STDMETHODCALLTYPE
_CorDllMain(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved)
{
	g_unsupported_api ("_CorDllMain");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}

__int32 STDMETHODCALLTYPE
_CorExeMain(void)
{
	g_unsupported_api ("_CorExeMain");
	SetLastError (ERROR_NOT_SUPPORTED);
	ExitProcess (EXIT_FAILURE);
}

STDAPI
_CorValidateImage(PVOID *ImageBase, LPCWSTR FileName)
{
	g_unsupported_api ("_CorValidateImage");
	SetLastError (ERROR_NOT_SUPPORTED);
	return E_UNEXPECTED;
}

HMODULE WINAPI
MonoLoadImage(LPCWSTR FileName)
{
	g_unsupported_api ("MonoLoadImage");
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL;
}

void
mono_coree_set_act_ctx (const char *file_name)
{
	g_unsupported_api ("CreateActCtx, ActivateActCtx");
	SetLastError (ERROR_NOT_SUPPORTED);
	return;
}

void
mono_load_coree (const char* exe_file_name)
{
	g_unsupported_api ("mono_load_coree");
	SetLastError (ERROR_NOT_SUPPORTED);
	return;
}

void
mono_fixup_exe_image (MonoImage* image)
{
	return;
}
#endif /* HAVE_API_SUPPORT_WIN32_COREE */

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (coree);

#endif /* HOST_WIN32 */
