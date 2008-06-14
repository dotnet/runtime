#define WINVER 0x0500
#define _WIN32_WINNT 0x0500

#include <vcclr.h>
#include <windows.h>

using namespace System;
using namespace System::Runtime::InteropServices;

void WriteStringManaged (const wchar_t* str)
{
	Console::WriteLine (Marshal::PtrToStringUni ((IntPtr) (void*) str));
}

#pragma managed(push, off)
void WriteStringUnmanaged (const wchar_t* str)
{
	DWORD count;
	WriteConsole (GetStdHandle (STD_OUTPUT_HANDLE), str, (DWORD) wcslen (str), &count, NULL);
}

void WriteStringWrapper (const wchar_t* str)
{
	WriteStringManaged (str);
}
#pragma managed(pop)

int main (array<String^>^ args)
{
	Console::WriteLine ("Mixed-mode MSVCRT console application");
	pin_ptr<const wchar_t> str = PtrToStringChars (L"WriteStringUnmanaged" + Environment::NewLine);
	WriteStringUnmanaged (str);
	WriteStringWrapper (L"WriteStringManaged");
	return 0;
}
