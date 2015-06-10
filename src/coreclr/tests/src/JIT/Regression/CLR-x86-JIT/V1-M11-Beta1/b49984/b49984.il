// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly ILGEN_0x693bdfcb {}
.class ILGEN_0x693bdfcb {
.method static int32 Main() {
.entrypoint
.maxstack	20
 .try
  {
   ldc.i4 65
   leave TRY_0x0
  }
 catch ['mscorlib']System.Exception
  {
    leave TRY_0x0
  }
 TRY_0x0:

 ldc.i4 100
 ret
}
}
