// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <iostream>

#using <System.Console.dll>
#using <System.Runtime.Loader.dll>

using namespace System;
using namespace System::Reflection;
using namespace System::Runtime::Loader;

public ref class ManagedClass
{
private:
    static int s_count = 0;
public:
    static void Print()
    {
        Assembly^ assembly = Assembly::GetExecutingAssembly();
        AssemblyLoadContext^ alc = AssemblyLoadContext::GetLoadContext(assembly);
        Console::WriteLine("[C++/CLI] ManagedClass: AssemblyLoadContext = " + alc->ToString());
    }
};

extern "C" __declspec(dllexport) void __cdecl NativeEntryPoint()
{
    std::cout << "[C++/CLI] NativeEntryPoint: calling managed class" << std::endl;
    ManagedClass::Print();
}
