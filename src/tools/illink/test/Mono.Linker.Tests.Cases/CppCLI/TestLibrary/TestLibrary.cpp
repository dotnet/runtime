#include "TestLibrary.h"

#include <iostream>

extern "C" __declspec(dllexport) int __stdcall NativeEntryPoint(int p)
{
	std::cout << "Native method\n";
	return p * 11;
}