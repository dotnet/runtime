// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       hardware_physmem.c
/// \brief      Get the total amount of physical memory (RAM)
//
//  Author:     Jonathan Nieder
//
///////////////////////////////////////////////////////////////////////////////

#include "common.h"

#include "tuklib_physmem.h"


extern LZMA_API(uint64_t)
lzma_physmem(void)
{
	// It is simpler to make lzma_physmem() a wrapper for
	// tuklib_physmem() than to hack appropriate symbol visibility
	// support for the tuklib modules.
	return tuklib_physmem();
}
