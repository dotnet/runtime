#define WINVER 0x0500
#define _WIN32_WINNT 0x0500

#include <windows.h>

using namespace System;
using namespace System::Runtime::InteropServices;

void WriteStringManaged (const wchar_t* str)
{
	Console::WriteLine (Marshal::PtrToStringUni ((IntPtr) (void*) str));
}

#pragma managed(push, off)
void __stdcall WriteStringUnmanaged (const wchar_t* str)
{
	WriteStringManaged (str);
}
#pragma managed(pop)

#pragma warning(disable:4483)
 
void __clrcall __identifier(".cctor") ()
{
}
