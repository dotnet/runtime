// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/
/*****************************************************************************/
#ifndef REGDEF
#error  Must define REGDEF macro before including this file
#endif
/*****************************************************************************/
/*                  The following is x86 specific                            */
/*****************************************************************************/
/*
REGDEF(name, rnum,  mask, sname) */
REGDEF(FPV0,    0,  0x01, "FPV0"  )
REGDEF(FPV1,    1,  0x02, "FPV1"  )
REGDEF(FPV2,    2,  0x04, "FPV2"  )
REGDEF(FPV3,    3,  0x08, "FPV3"  )
REGDEF(FPV4,    4,  0x10, "FPV4"  )
REGDEF(FPV5,    5,  0x20, "FPV5"  )
REGDEF(FPV6,    6,  0x40, "FPV6"  )
REGDEF(FPV7,    7,  0x80, "FPV7"  )


/*****************************************************************************/
#undef  REGDEF
/*****************************************************************************/
