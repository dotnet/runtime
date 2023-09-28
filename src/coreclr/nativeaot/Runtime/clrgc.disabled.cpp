#include <stdio.h>

typedef long HRESULT;

HRESULT InitializeDefaultGC();

HRESULT InitializeGCSelector()
{
    return InitializeDefaultGC();
}