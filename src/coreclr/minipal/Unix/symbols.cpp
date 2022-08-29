#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <dlfcn.h>
#include "minipal.h"
#include <cxxabi.h>

// Get a string describing the symbol located at the specified address.
// Parameters:
//  address      - address to find the symbol for
//  buffer       - buffer to store the resulting string
//  bufferSize   - size of the buffer
void VMToOSInterface::GetSymbolFromAddress(const void* address, char* buffer, size_t bufferSize)
{
    Dl_info dlInfo;
    int st = dladdr(address, &dlInfo);
    if (st != 0)
    {
        const char* filename = dlInfo.dli_fname;
        const char* lastSlash = strrchr(dlInfo.dli_fname, '/');
        if (lastSlash != nullptr)
        {
            filename = lastSlash + 1;
        }

        if (dlInfo.dli_sname != nullptr)
        {
            // Symbol was found
            const char* demangledName = abi::__cxa_demangle(dlInfo.dli_sname, NULL, NULL, &st);
            snprintf(buffer, bufferSize, "%s + 0x%zx (%s + 0x%zx)", (demangledName != nullptr) ? demangledName : dlInfo.dli_sname, (char*)address - (char*)dlInfo.dli_saddr, filename, (char*)address - (char*)dlInfo.dli_fbase);
            free((void*)demangledName);
        }
        else
        {
            // No symbol was found, return name of the shared library and an offset in it
            snprintf(buffer, bufferSize, "<unknown> (%s + 0x%zx)", filename, (char*)address - (char*)dlInfo.dli_fbase);
        }
    }
    else
    {
        // dladdr has failed to locate the address, so just print it
        snprintf(buffer, bufferSize, "%p", address);
    }
}
