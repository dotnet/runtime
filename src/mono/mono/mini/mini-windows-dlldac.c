#include <config.h>

#ifdef HOST_WIN32
#include <winsock2.h>
#include <windows.h>
#include <winnt.h>

#if defined(TARGET_AMD64) && !defined(DISABLE_JIT)
#include "mono/mini/mini.h"
#include "mono/mini/mini-amd64.h"
#include "mono/utils/mono-publib.h"

typedef enum _FUNCTION_TABLE_TYPE {
    RF_SORTED,
    RF_UNSORTED,
    RF_CALLBACK
} FUNCTION_TABLE_TYPE;

typedef struct _DYNAMIC_FUNCTION_TABLE {
    LIST_ENTRY Links;
    PRUNTIME_FUNCTION FunctionTable;
    LARGE_INTEGER TimeStamp;
    ULONG64 MinimumAddress;
    ULONG64 MaximumAddress;
    ULONG64 BaseAddress;
    PGET_RUNTIME_FUNCTION_CALLBACK Callback;
    PVOID Context;
    PWSTR OutOfProcessCallbackDll;
    FUNCTION_TABLE_TYPE Type;
    ULONG EntryCount;
} DYNAMIC_FUNCTION_TABLE, *PDYNAMIC_FUNCTION_TABLE;

typedef BOOL (ReadMemoryFunction)(PVOID user_context, LPCVOID base_address, PVOID buffer, SIZE_T size, SIZE_T *read);

BOOL
read_memory(PVOID user_context, LPCVOID base_address, PVOID buffer, SIZE_T size, SIZE_T* read)
{
    return ReadProcessMemory ((HANDLE)user_context, base_address, buffer, size, read);
}

MONO_EXTERN_C
MONO_API_EXPORT DWORD
OutOfProcessFunctionTableCallbackEx (ReadMemoryFunction read_memory, PVOID user_context, PVOID table_address, PDWORD entries, PRUNTIME_FUNCTION *functions)
{
	DYNAMIC_FUNCTION_TABLE func_table = { 0 };
	DynamicFunctionTableEntry func_table_entry = { 0 };
	PRUNTIME_FUNCTION rt_funcs = NULL;
	size_t reads = 0;
	DWORD result = 0xC0000001;

	if (read_memory (user_context, table_address, &func_table, sizeof (func_table), &reads)) {
		if (func_table.Context != NULL && read_memory (user_context, func_table.Context, &func_table_entry, sizeof (func_table_entry), &reads)) {
			if (func_table_entry.rt_funcs_current_count != 0) {
				rt_funcs = (PRUNTIME_FUNCTION)HeapAlloc (GetProcessHeap (), HEAP_ZERO_MEMORY, func_table_entry.rt_funcs_current_count * sizeof (RUNTIME_FUNCTION));
				if (rt_funcs) {
					if (read_memory (user_context, func_table_entry.rt_funcs, rt_funcs, func_table_entry.rt_funcs_current_count * sizeof (RUNTIME_FUNCTION), &reads)) {
						*entries = func_table_entry.rt_funcs_current_count;
						*functions = rt_funcs;
						result = 0x00000000;
					}
				}
			}
		}
	}

	return result;
}

MONO_EXTERN_C
MONO_API_EXPORT DWORD
OutOfProcessFunctionTableCallback (HANDLE process, PVOID table_address, PDWORD entries, PRUNTIME_FUNCTION *functions)
{
	return OutOfProcessFunctionTableCallbackEx (&read_memory, process, table_address, entries, functions);
}
#endif /* defined(TARGET_AMD64) && !defined(DISABLE_JIT) */

#ifdef _MSC_VER
MONO_EXTERN_C
BOOL APIENTRY
DllMain (HMODULE module_handle, DWORD reason, LPVOID reserved)
{
	return TRUE;
}
#endif

#else

MONO_EMPTY_SOURCE_FILE (mini_windows_dlldac);
#endif /* HOST_WIN32 */
