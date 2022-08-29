#include <windows.h>
#include "minipal.h"

void MagicInit();
void GetStringFromAddr(DWORD_PTR dwAddr, _Out_writes_(stringBufferSize) LPSTR szString, size_t stringBufferSize);

// Get a string describing the symbol located at the specified address.
// Parameters:
//  address      - address to find the symbol for
//  buffer       - buffer to store the resulting string
//  bufferSize   - size of the buffer
void VMToOSInterface::GetSymbolFromAddress(const void* address, char* buffer, size_t bufferSize)
{
    MagicInit();
    GetStringFromAddr((DWORD_PTR)address, buffer, bufferSize);
}
