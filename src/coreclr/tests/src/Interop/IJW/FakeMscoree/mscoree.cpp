#include <windows.h>

// Entrypoint jumped to by IJW dlls when their dllmain is called
extern "C" __declspec(dllexport) BOOL WINAPI _CorDllMain(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved)
{
    return TRUE;
}
