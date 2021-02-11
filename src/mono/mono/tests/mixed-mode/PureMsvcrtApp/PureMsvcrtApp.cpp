#include <vcclr.h>

using namespace System;
using namespace System::Runtime::InteropServices;

void WriteStringManaged (const wchar_t* str)
{
	Console::WriteLine (Marshal::PtrToStringUni ((IntPtr) (void*) str));
}

int main (array<System::String^> ^args)
{
	Console::WriteLine (L"Pure MSVCRT console application");
	pin_ptr<const wchar_t> str = PtrToStringChars (L"WriteStringManaged");
	WriteStringManaged (str);
	return 0;
}
