// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly tailcall.exe{}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
    tail. call    int32 method2()
    ret
}
.method public     static    int32 method2() {
    ldc.i4      100
    ret 
}
