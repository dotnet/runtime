#define WINVER 0x0500
#define _WIN32_WINNT 0x0500

#include <windows.h>

typedef HRESULT (STDAPICALLTYPE *MONOFIXUPCOREE) (HMODULE);
typedef void (__stdcall *WRITESTRING) (const wchar_t*);

#ifdef _WIN64
#include <stdint.h>
extern "C" void __security_check_cookie(unsigned __int64 value)
{
}
#endif

int main ()
{
	HMODULE mscoree_module_handle;
	HMODULE mono_module_handle;
	MONOFIXUPCOREE MonoFixupCorEE;
	HMODULE module_handle;
	WRITESTRING WriteStringUnmanaged;

	mscoree_module_handle = LoadLibrary (L"mscoree.dll");
	if (mscoree_module_handle == NULL)
		return 1;

	mono_module_handle = LoadLibrary (L"mono.dll");
	if (mono_module_handle == NULL)
		return 2;
	MonoFixupCorEE = (MONOFIXUPCOREE) GetProcAddress (mono_module_handle, "MonoFixupCorEE");
	if (MonoFixupCorEE == NULL || !SUCCEEDED (MonoFixupCorEE (mscoree_module_handle)))
		return 3;

	module_handle = LoadLibrary (L"MixedModeLibrary.dll");
	if (module_handle == NULL)
		return 4;

	WriteStringUnmanaged = (WRITESTRING) GetProcAddress (module_handle, "WriteStringUnmanaged");
	if (WriteStringUnmanaged == NULL)
		return 5;

	WriteStringUnmanaged (L"WriteStringUnmanaged");

	ExitProcess (0);
}

