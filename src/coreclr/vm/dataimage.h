// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef _DATAIMAGE_H_
#define _DATAIMAGE_H_

// IMAGE_REL_BASED_PTR is architecture specific reloc of virtual address
#ifdef TARGET_64BIT
#define IMAGE_REL_BASED_PTR IMAGE_REL_BASED_DIR64
#else // !TARGET_64BIT
#define IMAGE_REL_BASED_PTR IMAGE_REL_BASED_HIGHLOW
#endif // !TARGET_64BIT

// Special NGEN-specific relocation type for relative pointer (used to make NGen relocation section smaller)
#define IMAGE_REL_BASED_RELPTR            0x7D

#endif // _DATAIMAGE_H_
