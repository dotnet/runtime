/*
 * coree.c: mscoree.dll functions
 *
 * Author:
 *   Kornel Pal <http://www.kornelpal.hu/>
 *
 * Copyright (C) 2008 Kornel Pal
 */

#include <config.h>

#ifdef PLATFORM_WIN32

#if _WIN32_WINNT < 0x0501
/* Required for ACTCTX. */
#undef _WIN32_WINNT
#define _WIN32_WINNT 0x0501
#endif /* _WIN32_WINNT < 0x0501 */

#include <string.h>
#include <glib.h>
#include <mono/io-layer/io-layer.h>
#include <mono/utils/mono-path.h>
#include "cil-coff.h"
#include "metadata-internals.h"
#include "image.h"
#include "assembly.h"
#include "domain-internals.h"
#include "appdomain.h"
#include "object.h"
#include "loader.h"
#include "threads.h"
#include "environment.h"
#include "coree.h"

HMODULE mono_module_handle = NULL;
HMODULE coree_module_handle = NULL;

static gboolean init_from_coree = FALSE;

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

/* Entry point called by LdrLoadDll of ntdll.dll after _CorValidateImage. */
BOOL STDMETHODCALLTYPE _CorDllMain(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved)
{
	MonoAssembly* assembly;
	MonoImage* image;
	gchar* file_name;
	gchar* error;

	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
		DisableThreadLibraryCalls (hInst);

		file_name = mono_get_module_file_name (hInst);

		if (mono_get_root_domain ()) {
			image = mono_image_open_from_module_handle (hInst, mono_path_resolve_symlinks (file_name), 0, NULL);
		} else {
			init_from_coree = TRUE;
			mono_runtime_load (file_name, NULL);
			error = (gchar*) mono_check_corlib_version ();
			if (error) {
				g_free (error);
				g_free (file_name);
				mono_runtime_quit ();
				return FALSE;
			}

			image = mono_image_open (file_name, NULL);
			mono_close_exe_image ();
			if (image)
				/* Decrement reference count to zero. (Image will not be closed.) */
				mono_image_close (image);
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
		if (image->tables [MONO_TABLE_ASSEMBLY].rows && ((MonoCLIImageInfo*) image->image_info)->cli_cli_header.ch_vtable_fixups.rva)
			assembly = mono_assembly_open (file_name, NULL);

		g_free (file_name);
		break;
	case DLL_PROCESS_DETACH:
		if (lpReserved != NULL)
			/* The process is terminating. */
			return TRUE;
		file_name = mono_get_module_file_name (hInst);
		image = mono_image_loaded (file_name);
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
	MonoDomain* domain;
	MonoAssembly* assembly;
	MonoImage* image;
	MonoMethod* method;
	guint32 entry;
	gchar* file_name;
	gchar* error;
	int argc;
	gunichar2** argvw;
	gchar** argv;
	int i;

	file_name = mono_get_module_file_name (GetModuleHandle (NULL));
	init_from_coree = TRUE;
	domain = mono_runtime_load (file_name, NULL);

	error = (gchar*) mono_check_corlib_version ();
	if (error) {
		g_free (error);
		g_free (file_name);
		MessageBox (NULL, L"Corlib not in sync with this runtime.", NULL, MB_ICONERROR);
		mono_runtime_quit ();
		ExitProcess (1);
	}

	assembly = mono_assembly_open (file_name, NULL);
	mono_close_exe_image ();
	if (!assembly) {
		g_free (file_name);
		MessageBox (NULL, L"Cannot open assembly.", NULL, MB_ICONERROR);
		mono_runtime_quit ();
		ExitProcess (1);
	}

	image = assembly->image;
	entry = mono_image_get_entry_point (image);
	if (!entry) {
		g_free (file_name);
		MessageBox (NULL, L"Assembly doesn't have an entry point.", NULL, MB_ICONERROR);
		mono_runtime_quit ();
		ExitProcess (1);
	}

	method = mono_get_method (image, entry, NULL);
	if (method == NULL) {
		g_free (file_name);
		MessageBox (NULL, L"The entry point method could not be loaded.", NULL, MB_ICONERROR);
		mono_runtime_quit ();
		ExitProcess (1);
	}

	argvw = CommandLineToArgvW (GetCommandLine (), &argc);
	argv = g_new0 (gchar*, argc);
	argv [0] = file_name;
	for (i = 1; i < argc; ++i)
		argv [i] = g_utf16_to_utf8 (argvw [i], -1, NULL, NULL, NULL);
	LocalFree (argvw);

	mono_runtime_run_main (method, argc, argv, NULL);
	mono_thread_manage ();

	mono_runtime_quit ();

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
		mono_runtime_quit ();
	}
#endif
	ExitProcess (exitCode);
}

/* Called by ntdll.dll before _CorDllMain and _CorExeMain. */
STDAPI _CorValidateImage(PVOID *ImageBase, LPCWSTR FileName)
{
	IMAGE_DOS_HEADER* DosHeader;
	IMAGE_NT_HEADERS* NtHeaders;
	DWORD* Address;
	DWORD OldProtect;

	DosHeader = (IMAGE_DOS_HEADER*)*ImageBase;
	if (DosHeader->e_magic != IMAGE_DOS_SIGNATURE)
		return STATUS_INVALID_IMAGE_FORMAT;

	NtHeaders = (IMAGE_NT_HEADERS*)((DWORD_PTR)DosHeader + DosHeader->e_lfanew);
	if (NtHeaders->Signature != IMAGE_NT_SIGNATURE)
		return STATUS_INVALID_IMAGE_FORMAT;

	if (NtHeaders->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR_MAGIC)
		return STATUS_INVALID_IMAGE_FORMAT;

	Address = &NtHeaders->OptionalHeader.AddressOfEntryPoint;
	if (!VirtualProtect(Address, sizeof(DWORD), PAGE_READWRITE, &OldProtect))
		return E_UNEXPECTED;
	if (NtHeaders->FileHeader.Characteristics & IMAGE_FILE_DLL)
		*Address = (DWORD)((DWORD_PTR)&_CorDllMain - (DWORD_PTR)DosHeader);
	else
		*Address = (DWORD)((DWORD_PTR)&_CorExeMain - (DWORD_PTR)DosHeader);
	if (!VirtualProtect(Address, sizeof(DWORD), OldProtect, &OldProtect))
		return E_UNEXPECTED;

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

/* Fixup exported functions of mscoree.dll to our implementations. */
STDAPI MonoFixupCorEE(HMODULE ModuleHandle)
{
	typedef struct _EXPORT_FIXUP
	{
		LPCSTR Name;
		DWORD_PTR ProcAddress;
	} EXPORT_FIXUP;

	/* Has to be binary ordered. */
	const EXPORT_FIXUP ExportFixups[] = {
		{"CorBindToRuntime", (DWORD_PTR)&CorBindToRuntime},
		{"CorBindToRuntimeEx", (DWORD_PTR)&CorBindToRuntimeEx},
		{"CorExitProcess", (DWORD_PTR)&CorExitProcess},
		{"_CorDllMain", (DWORD_PTR)&_CorDllMain},
		{"_CorExeMain", (DWORD_PTR)&_CorExeMain},
		{"_CorImageUnloading", (DWORD_PTR)&_CorImageUnloading},
		{"_CorValidateImage", (DWORD_PTR)&_CorValidateImage},
		{NULL, 0}
	};

	IMAGE_DOS_HEADER* DosHeader;
	IMAGE_NT_HEADERS* NtHeaders;
	IMAGE_DATA_DIRECTORY* ExportDataDir;
	IMAGE_EXPORT_DIRECTORY* ExportDir;
	DWORD* Functions;
	DWORD* Names;
	WORD* NameOrdinals;
	EXPORT_FIXUP* ExportFixup;
	DWORD* Address;
	DWORD OldProtect;
	DWORD ProcRVA;
	DWORD i;

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
	if (!ExportDataDir->VirtualAddress || !ExportDataDir->Size)
		return E_FAIL;

	ExportDir = (IMAGE_EXPORT_DIRECTORY*)((DWORD_PTR)DosHeader + ExportDataDir->VirtualAddress);
	Functions = (DWORD*)((DWORD_PTR)DosHeader + ExportDir->AddressOfFunctions);
	Names = (DWORD*)((DWORD_PTR)DosHeader + ExportDir->AddressOfNames);
	NameOrdinals = (WORD*)((DWORD_PTR)DosHeader + ExportDir->AddressOfNameOrdinals);
	ExportFixup = (EXPORT_FIXUP*)&ExportFixups;

	for (i = 0; i < ExportDir->NumberOfNames; i++)
	{
		int cmp = strcmp((LPCSTR)((DWORD_PTR)DosHeader + Names[i]), ExportFixup->Name);
		if (cmp > 0)
			return E_FAIL;
		if (cmp == 0)
		{
			Address = &Functions[NameOrdinals[i]];
			ProcRVA = (DWORD)(ExportFixup->ProcAddress - (DWORD_PTR)DosHeader);
			if (*Address != ProcRVA) {
				if (!VirtualProtect(Address, sizeof(DWORD), PAGE_READWRITE, &OldProtect))
					return E_UNEXPECTED;
				*Address = ProcRVA;
				if (!VirtualProtect(Address, sizeof(DWORD), OldProtect, &OldProtect))
					return E_UNEXPECTED;
			}
			ExportFixup++;
			if (ExportFixup->Name == NULL)
				return S_OK;
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
	if (DosHeader == NULL || DosHeader->e_magic != IMAGE_DOS_SIGNATURE)
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
		if (CliHeaderDir->VirtualAddress && CliHeaderDir->Size)
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
			if (BaseRelocDir->VirtualAddress && BaseRelocDir->Size)
			{
				BaseReloc = (IMAGE_BASE_RELOCATION*)((DWORD_PTR)DosHeader + BaseRelocDir->VirtualAddress);
				BaseRelocSize = BaseRelocDir->Size;

				while (BaseRelocSize)
				{
					RelocBlockSize = BaseReloc->SizeOfBlock;

					if (!RelocBlockSize || BaseRelocSize < RelocBlockSize)
						return E_FAIL;

					BaseRelocSize -= RelocBlockSize;
					RelocBlock = (USHORT*)((DWORD_PTR)BaseReloc + IMAGE_SIZEOF_BASE_RELOCATION);
					RelocBlockSize -= IMAGE_SIZEOF_BASE_RELOCATION;
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
				ImportModuleHandle = LoadLibraryA((PCSTR)((DWORD_PTR)DosHeader + ImportDesc->Name));
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
						ProcAddress = (DWORD_PTR)GetProcAddress(ImportModuleHandle, ImportByName->Name);
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

static void
mono_set_act_ctx (const char* file_name)
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
	act_ctx.lpResourceName = MAKEINTRESOURCE (CREATEPROCESS_MANIFEST_RESOURCE_ID);
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
	gunichar2* file_name;
	UINT required_size;
	UINT size;

	if (coree_module_handle)
		return;

	if (!init_from_coree && exe_file_name)
		mono_set_act_ctx (exe_file_name);

	/* ntdll.dll loads mscoree.dll from the system32 directory. */
	required_size = GetSystemDirectory (NULL, 0);
	file_name = g_new (gunichar2, required_size + 12);
	size = GetSystemDirectory (file_name, required_size);
	g_assert (size < required_size);
	if (file_name [size - 1] != L'\\')
		file_name [size++] = L'\\';
	memcpy (&file_name [size], L"mscoree.dll", 12 * sizeof (gunichar2));

	coree_module_handle = LoadLibrary (file_name);
	g_free (file_name);

	if (coree_module_handle && !SUCCEEDED (MonoFixupCorEE (coree_module_handle))) {
		FreeLibrary (coree_module_handle);
		coree_module_handle = NULL;
	}
}

void
mono_fixup_exe_image (MonoImage* image)
{
	if (!init_from_coree && image && image->is_module_handle)
		MonoFixupExe ((HMODULE) image->raw_data);
}

#endif /* PLATFORM_WIN32 */
