
#include <stdio.h>
#include <stdint.h>

#if defined(_WIN32)
extern "C" int __managed__Main(int argc, wchar_t* argv[]);
#else
extern "C" int __managed__Main(int argc, char* argv[]);
#endif

extern "C" void SetExitCodeInManagedSide(int32_t exitCode);

#if defined(_WIN32)
int __cdecl wmain(int argc, wchar_t* argv[])
#else
int main(int argc, char* argv[])
#endif
{
    puts("hello from native code");
    SetExitCodeInManagedSide(100);
    return __managed__Main(argc, argv);
}
