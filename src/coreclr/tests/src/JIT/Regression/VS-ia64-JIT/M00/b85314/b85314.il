// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly s.exe{}
.class public s {
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
  ldc.i4 0
	switch (TC1,TC2,TC3)
  ldc.i4 5
  ret
		TC1:
  ldc.i4 100
  ret
		TC2:
  ldc.i4 6
  ret
		TC3:
  ldc.i4 7
  ret
}
}
