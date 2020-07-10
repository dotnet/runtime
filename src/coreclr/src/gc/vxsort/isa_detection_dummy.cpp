// ISA_Detection.cpp : Diese Datei enthält die Funktion "main". Hier beginnt und endet die Ausführung des Programms.
//
#include "common.h"

#include "do_vxsort.h"

#if defined(TARGET_AMD64) && defined(TARGET_WINDOWS)

bool SupportsInstructionSet(InstructionSet instructionSet)
{
    return false;
}
#endif // defined(TARGET_AMD64) && defined(TARGET_WINDOWS)

