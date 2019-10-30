// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly ldloca_s_i1.exe {}
.class ldloca_s_i1 {

.method public static int32 test_int8()
{
.locals (int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8,
         int8, int8, int8, int8, int8, int8, int8, int8)
.zeroinit
.maxstack       2

         ldloca.s    0x00
         dup
         conv.i1
         stind.i1
         ldloca.s    0x00
         ldind.i1
         ldloca.s    0x00
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x01
         dup
         conv.i1
         stind.i1
         ldloca.s    0x01
         ldind.i1
         ldloca.s    0x01
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x02
         dup
         conv.i1
         stind.i1
         ldloca.s    0x02
         ldind.i1
         ldloca.s    0x02
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x03
         dup
         conv.i1
         stind.i1
         ldloca.s    0x03
         ldind.i1
         ldloca.s    0x03
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x04
         dup
         conv.i1
         stind.i1
         ldloca.s    0x04
         ldind.i1
         ldloca.s    0x04
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x05
         dup
         conv.i1
         stind.i1
         ldloca.s    0x05
         ldind.i1
         ldloca.s    0x05
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x06
         dup
         conv.i1
         stind.i1
         ldloca.s    0x06
         ldind.i1
         ldloca.s    0x06
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x07
         dup
         conv.i1
         stind.i1
         ldloca.s    0x07
         ldind.i1
         ldloca.s    0x07
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x08
         dup
         conv.i1
         stind.i1
         ldloca.s    0x08
         ldind.i1
         ldloca.s    0x08
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x09
         dup
         conv.i1
         stind.i1
         ldloca.s    0x09
         ldind.i1
         ldloca.s    0x09
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0a
         dup
         conv.i1
         stind.i1
         ldloca.s    0x0a
         ldind.i1
         ldloca.s    0x0a
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0b
         dup
         conv.i1
         stind.i1
         ldloca.s    0x0b
         ldind.i1
         ldloca.s    0x0b
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0c
         dup
         conv.i1
         stind.i1
         ldloca.s    0x0c
         ldind.i1
         ldloca.s    0x0c
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0d
         dup
         conv.i1
         stind.i1
         ldloca.s    0x0d
         ldind.i1
         ldloca.s    0x0d
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0e
         dup
         conv.i1
         stind.i1
         ldloca.s    0x0e
         ldind.i1
         ldloca.s    0x0e
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0f
         dup
         conv.i1
         stind.i1
         ldloca.s    0x0f
         ldind.i1
         ldloca.s    0x0f
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x10
         dup
         conv.i1
         stind.i1
         ldloca.s    0x10
         ldind.i1
         ldloca.s    0x10
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x11
         dup
         conv.i1
         stind.i1
         ldloca.s    0x11
         ldind.i1
         ldloca.s    0x11
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x12
         dup
         conv.i1
         stind.i1
         ldloca.s    0x12
         ldind.i1
         ldloca.s    0x12
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x13
         dup
         conv.i1
         stind.i1
         ldloca.s    0x13
         ldind.i1
         ldloca.s    0x13
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x14
         dup
         conv.i1
         stind.i1
         ldloca.s    0x14
         ldind.i1
         ldloca.s    0x14
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x15
         dup
         conv.i1
         stind.i1
         ldloca.s    0x15
         ldind.i1
         ldloca.s    0x15
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x16
         dup
         conv.i1
         stind.i1
         ldloca.s    0x16
         ldind.i1
         ldloca.s    0x16
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x17
         dup
         conv.i1
         stind.i1
         ldloca.s    0x17
         ldind.i1
         ldloca.s    0x17
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x18
         dup
         conv.i1
         stind.i1
         ldloca.s    0x18
         ldind.i1
         ldloca.s    0x18
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x19
         dup
         conv.i1
         stind.i1
         ldloca.s    0x19
         ldind.i1
         ldloca.s    0x19
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1a
         dup
         conv.i1
         stind.i1
         ldloca.s    0x1a
         ldind.i1
         ldloca.s    0x1a
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1b
         dup
         conv.i1
         stind.i1
         ldloca.s    0x1b
         ldind.i1
         ldloca.s    0x1b
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1c
         dup
         conv.i1
         stind.i1
         ldloca.s    0x1c
         ldind.i1
         ldloca.s    0x1c
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1d
         dup
         conv.i1
         stind.i1
         ldloca.s    0x1d
         ldind.i1
         ldloca.s    0x1d
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1e
         dup
         conv.i1
         stind.i1
         ldloca.s    0x1e
         ldind.i1
         ldloca.s    0x1e
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1f
         dup
         conv.i1
         stind.i1
         ldloca.s    0x1f
         ldind.i1
         ldloca.s    0x1f
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x20
         dup
         conv.i1
         stind.i1
         ldloca.s    0x20
         ldind.i1
         ldloca.s    0x20
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x21
         dup
         conv.i1
         stind.i1
         ldloca.s    0x21
         ldind.i1
         ldloca.s    0x21
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x22
         dup
         conv.i1
         stind.i1
         ldloca.s    0x22
         ldind.i1
         ldloca.s    0x22
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x23
         dup
         conv.i1
         stind.i1
         ldloca.s    0x23
         ldind.i1
         ldloca.s    0x23
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x24
         dup
         conv.i1
         stind.i1
         ldloca.s    0x24
         ldind.i1
         ldloca.s    0x24
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x25
         dup
         conv.i1
         stind.i1
         ldloca.s    0x25
         ldind.i1
         ldloca.s    0x25
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x26
         dup
         conv.i1
         stind.i1
         ldloca.s    0x26
         ldind.i1
         ldloca.s    0x26
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x27
         dup
         conv.i1
         stind.i1
         ldloca.s    0x27
         ldind.i1
         ldloca.s    0x27
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x28
         dup
         conv.i1
         stind.i1
         ldloca.s    0x28
         ldind.i1
         ldloca.s    0x28
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x29
         dup
         conv.i1
         stind.i1
         ldloca.s    0x29
         ldind.i1
         ldloca.s    0x29
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2a
         dup
         conv.i1
         stind.i1
         ldloca.s    0x2a
         ldind.i1
         ldloca.s    0x2a
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2b
         dup
         conv.i1
         stind.i1
         ldloca.s    0x2b
         ldind.i1
         ldloca.s    0x2b
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2c
         dup
         conv.i1
         stind.i1
         ldloca.s    0x2c
         ldind.i1
         ldloca.s    0x2c
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2d
         dup
         conv.i1
         stind.i1
         ldloca.s    0x2d
         ldind.i1
         ldloca.s    0x2d
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2e
         dup
         conv.i1
         stind.i1
         ldloca.s    0x2e
         ldind.i1
         ldloca.s    0x2e
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2f
         dup
         conv.i1
         stind.i1
         ldloca.s    0x2f
         ldind.i1
         ldloca.s    0x2f
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x30
         dup
         conv.i1
         stind.i1
         ldloca.s    0x30
         ldind.i1
         ldloca.s    0x30
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x31
         dup
         conv.i1
         stind.i1
         ldloca.s    0x31
         ldind.i1
         ldloca.s    0x31
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x32
         dup
         conv.i1
         stind.i1
         ldloca.s    0x32
         ldind.i1
         ldloca.s    0x32
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x33
         dup
         conv.i1
         stind.i1
         ldloca.s    0x33
         ldind.i1
         ldloca.s    0x33
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x34
         dup
         conv.i1
         stind.i1
         ldloca.s    0x34
         ldind.i1
         ldloca.s    0x34
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x35
         dup
         conv.i1
         stind.i1
         ldloca.s    0x35
         ldind.i1
         ldloca.s    0x35
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x36
         dup
         conv.i1
         stind.i1
         ldloca.s    0x36
         ldind.i1
         ldloca.s    0x36
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x37
         dup
         conv.i1
         stind.i1
         ldloca.s    0x37
         ldind.i1
         ldloca.s    0x37
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x38
         dup
         conv.i1
         stind.i1
         ldloca.s    0x38
         ldind.i1
         ldloca.s    0x38
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x39
         dup
         conv.i1
         stind.i1
         ldloca.s    0x39
         ldind.i1
         ldloca.s    0x39
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3a
         dup
         conv.i1
         stind.i1
         ldloca.s    0x3a
         ldind.i1
         ldloca.s    0x3a
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3b
         dup
         conv.i1
         stind.i1
         ldloca.s    0x3b
         ldind.i1
         ldloca.s    0x3b
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3c
         dup
         conv.i1
         stind.i1
         ldloca.s    0x3c
         ldind.i1
         ldloca.s    0x3c
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3d
         dup
         conv.i1
         stind.i1
         ldloca.s    0x3d
         ldind.i1
         ldloca.s    0x3d
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3e
         dup
         conv.i1
         stind.i1
         ldloca.s    0x3e
         ldind.i1
         ldloca.s    0x3e
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3f
         dup
         conv.i1
         stind.i1
         ldloca.s    0x3f
         ldind.i1
         ldloca.s    0x3f
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x40
         dup
         conv.i1
         stind.i1
         ldloca.s    0x40
         ldind.i1
         ldloca.s    0x40
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x41
         dup
         conv.i1
         stind.i1
         ldloca.s    0x41
         ldind.i1
         ldloca.s    0x41
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x42
         dup
         conv.i1
         stind.i1
         ldloca.s    0x42
         ldind.i1
         ldloca.s    0x42
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x43
         dup
         conv.i1
         stind.i1
         ldloca.s    0x43
         ldind.i1
         ldloca.s    0x43
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x44
         dup
         conv.i1
         stind.i1
         ldloca.s    0x44
         ldind.i1
         ldloca.s    0x44
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x45
         dup
         conv.i1
         stind.i1
         ldloca.s    0x45
         ldind.i1
         ldloca.s    0x45
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x46
         dup
         conv.i1
         stind.i1
         ldloca.s    0x46
         ldind.i1
         ldloca.s    0x46
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x47
         dup
         conv.i1
         stind.i1
         ldloca.s    0x47
         ldind.i1
         ldloca.s    0x47
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x48
         dup
         conv.i1
         stind.i1
         ldloca.s    0x48
         ldind.i1
         ldloca.s    0x48
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x49
         dup
         conv.i1
         stind.i1
         ldloca.s    0x49
         ldind.i1
         ldloca.s    0x49
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4a
         dup
         conv.i1
         stind.i1
         ldloca.s    0x4a
         ldind.i1
         ldloca.s    0x4a
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4b
         dup
         conv.i1
         stind.i1
         ldloca.s    0x4b
         ldind.i1
         ldloca.s    0x4b
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4c
         dup
         conv.i1
         stind.i1
         ldloca.s    0x4c
         ldind.i1
         ldloca.s    0x4c
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4d
         dup
         conv.i1
         stind.i1
         ldloca.s    0x4d
         ldind.i1
         ldloca.s    0x4d
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4e
         dup
         conv.i1
         stind.i1
         ldloca.s    0x4e
         ldind.i1
         ldloca.s    0x4e
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4f
         dup
         conv.i1
         stind.i1
         ldloca.s    0x4f
         ldind.i1
         ldloca.s    0x4f
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x50
         dup
         conv.i1
         stind.i1
         ldloca.s    0x50
         ldind.i1
         ldloca.s    0x50
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x51
         dup
         conv.i1
         stind.i1
         ldloca.s    0x51
         ldind.i1
         ldloca.s    0x51
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x52
         dup
         conv.i1
         stind.i1
         ldloca.s    0x52
         ldind.i1
         ldloca.s    0x52
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x53
         dup
         conv.i1
         stind.i1
         ldloca.s    0x53
         ldind.i1
         ldloca.s    0x53
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x54
         dup
         conv.i1
         stind.i1
         ldloca.s    0x54
         ldind.i1
         ldloca.s    0x54
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x55
         dup
         conv.i1
         stind.i1
         ldloca.s    0x55
         ldind.i1
         ldloca.s    0x55
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x56
         dup
         conv.i1
         stind.i1
         ldloca.s    0x56
         ldind.i1
         ldloca.s    0x56
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x57
         dup
         conv.i1
         stind.i1
         ldloca.s    0x57
         ldind.i1
         ldloca.s    0x57
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x58
         dup
         conv.i1
         stind.i1
         ldloca.s    0x58
         ldind.i1
         ldloca.s    0x58
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x59
         dup
         conv.i1
         stind.i1
         ldloca.s    0x59
         ldind.i1
         ldloca.s    0x59
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5a
         dup
         conv.i1
         stind.i1
         ldloca.s    0x5a
         ldind.i1
         ldloca.s    0x5a
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5b
         dup
         conv.i1
         stind.i1
         ldloca.s    0x5b
         ldind.i1
         ldloca.s    0x5b
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5c
         dup
         conv.i1
         stind.i1
         ldloca.s    0x5c
         ldind.i1
         ldloca.s    0x5c
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5d
         dup
         conv.i1
         stind.i1
         ldloca.s    0x5d
         ldind.i1
         ldloca.s    0x5d
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5e
         dup
         conv.i1
         stind.i1
         ldloca.s    0x5e
         ldind.i1
         ldloca.s    0x5e
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5f
         dup
         conv.i1
         stind.i1
         ldloca.s    0x5f
         ldind.i1
         ldloca.s    0x5f
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x60
         dup
         conv.i1
         stind.i1
         ldloca.s    0x60
         ldind.i1
         ldloca.s    0x60
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x61
         dup
         conv.i1
         stind.i1
         ldloca.s    0x61
         ldind.i1
         ldloca.s    0x61
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x62
         dup
         conv.i1
         stind.i1
         ldloca.s    0x62
         ldind.i1
         ldloca.s    0x62
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x63
         dup
         conv.i1
         stind.i1
         ldloca.s    0x63
         ldind.i1
         ldloca.s    0x63
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x64
         dup
         conv.i1
         stind.i1
         ldloca.s    0x64
         ldind.i1
         ldloca.s    0x64
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x65
         dup
         conv.i1
         stind.i1
         ldloca.s    0x65
         ldind.i1
         ldloca.s    0x65
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x66
         dup
         conv.i1
         stind.i1
         ldloca.s    0x66
         ldind.i1
         ldloca.s    0x66
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x67
         dup
         conv.i1
         stind.i1
         ldloca.s    0x67
         ldind.i1
         ldloca.s    0x67
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x68
         dup
         conv.i1
         stind.i1
         ldloca.s    0x68
         ldind.i1
         ldloca.s    0x68
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x69
         dup
         conv.i1
         stind.i1
         ldloca.s    0x69
         ldind.i1
         ldloca.s    0x69
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6a
         dup
         conv.i1
         stind.i1
         ldloca.s    0x6a
         ldind.i1
         ldloca.s    0x6a
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6b
         dup
         conv.i1
         stind.i1
         ldloca.s    0x6b
         ldind.i1
         ldloca.s    0x6b
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6c
         dup
         conv.i1
         stind.i1
         ldloca.s    0x6c
         ldind.i1
         ldloca.s    0x6c
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6d
         dup
         conv.i1
         stind.i1
         ldloca.s    0x6d
         ldind.i1
         ldloca.s    0x6d
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6e
         dup
         conv.i1
         stind.i1
         ldloca.s    0x6e
         ldind.i1
         ldloca.s    0x6e
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6f
         dup
         conv.i1
         stind.i1
         ldloca.s    0x6f
         ldind.i1
         ldloca.s    0x6f
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x70
         dup
         conv.i1
         stind.i1
         ldloca.s    0x70
         ldind.i1
         ldloca.s    0x70
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x71
         dup
         conv.i1
         stind.i1
         ldloca.s    0x71
         ldind.i1
         ldloca.s    0x71
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x72
         dup
         conv.i1
         stind.i1
         ldloca.s    0x72
         ldind.i1
         ldloca.s    0x72
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x73
         dup
         conv.i1
         stind.i1
         ldloca.s    0x73
         ldind.i1
         ldloca.s    0x73
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x74
         dup
         conv.i1
         stind.i1
         ldloca.s    0x74
         ldind.i1
         ldloca.s    0x74
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x75
         dup
         conv.i1
         stind.i1
         ldloca.s    0x75
         ldind.i1
         ldloca.s    0x75
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x76
         dup
         conv.i1
         stind.i1
         ldloca.s    0x76
         ldind.i1
         ldloca.s    0x76
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x77
         dup
         conv.i1
         stind.i1
         ldloca.s    0x77
         ldind.i1
         ldloca.s    0x77
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x78
         dup
         conv.i1
         stind.i1
         ldloca.s    0x78
         ldind.i1
         ldloca.s    0x78
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x79
         dup
         conv.i1
         stind.i1
         ldloca.s    0x79
         ldind.i1
         ldloca.s    0x79
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7a
         dup
         conv.i1
         stind.i1
         ldloca.s    0x7a
         ldind.i1
         ldloca.s    0x7a
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7b
         dup
         conv.i1
         stind.i1
         ldloca.s    0x7b
         ldind.i1
         ldloca.s    0x7b
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7c
         dup
         conv.i1
         stind.i1
         ldloca.s    0x7c
         ldind.i1
         ldloca.s    0x7c
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7d
         dup
         conv.i1
         stind.i1
         ldloca.s    0x7d
         ldind.i1
         ldloca.s    0x7d
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7e
         dup
         conv.i1
         stind.i1
         ldloca.s    0x7e
         ldind.i1
         ldloca.s    0x7e
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7f
         dup
         conv.i1
         stind.i1
         ldloca.s    0x7f
         ldind.i1
         ldloca.s    0x7f
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x80
         dup
         conv.i1
         stind.i1
         ldloca.s    0x80
         ldind.i1
         ldloca.s    0x80
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x81
         dup
         conv.i1
         stind.i1
         ldloca.s    0x81
         ldind.i1
         ldloca.s    0x81
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x82
         dup
         conv.i1
         stind.i1
         ldloca.s    0x82
         ldind.i1
         ldloca.s    0x82
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x83
         dup
         conv.i1
         stind.i1
         ldloca.s    0x83
         ldind.i1
         ldloca.s    0x83
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x84
         dup
         conv.i1
         stind.i1
         ldloca.s    0x84
         ldind.i1
         ldloca.s    0x84
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x85
         dup
         conv.i1
         stind.i1
         ldloca.s    0x85
         ldind.i1
         ldloca.s    0x85
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x86
         dup
         conv.i1
         stind.i1
         ldloca.s    0x86
         ldind.i1
         ldloca.s    0x86
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x87
         dup
         conv.i1
         stind.i1
         ldloca.s    0x87
         ldind.i1
         ldloca.s    0x87
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x88
         dup
         conv.i1
         stind.i1
         ldloca.s    0x88
         ldind.i1
         ldloca.s    0x88
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x89
         dup
         conv.i1
         stind.i1
         ldloca.s    0x89
         ldind.i1
         ldloca.s    0x89
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8a
         dup
         conv.i1
         stind.i1
         ldloca.s    0x8a
         ldind.i1
         ldloca.s    0x8a
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8b
         dup
         conv.i1
         stind.i1
         ldloca.s    0x8b
         ldind.i1
         ldloca.s    0x8b
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8c
         dup
         conv.i1
         stind.i1
         ldloca.s    0x8c
         ldind.i1
         ldloca.s    0x8c
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8d
         dup
         conv.i1
         stind.i1
         ldloca.s    0x8d
         ldind.i1
         ldloca.s    0x8d
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8e
         dup
         conv.i1
         stind.i1
         ldloca.s    0x8e
         ldind.i1
         ldloca.s    0x8e
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8f
         dup
         conv.i1
         stind.i1
         ldloca.s    0x8f
         ldind.i1
         ldloca.s    0x8f
         conv.i1
         ceq
         brfalse    FAIL

         ldloca.s    0x90
         dup
         conv.i1
         stind.i1
         ldloca.s    0x90
         ldind.i1
         ldloca.s    0x90
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x91
         dup
         conv.i1
         stind.i1
         ldloca.s    0x91
         ldind.i1
         ldloca.s    0x91
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x92
         dup
         conv.i1
         stind.i1
         ldloca.s    0x92
         ldind.i1
         ldloca.s    0x92
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x93
         dup
         conv.i1
         stind.i1
         ldloca.s    0x93
         ldind.i1
         ldloca.s    0x93
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x94
         dup
         conv.i1
         stind.i1
         ldloca.s    0x94
         ldind.i1
         ldloca.s    0x94
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x95
         dup
         conv.i1
         stind.i1
         ldloca.s    0x95
         ldind.i1
         ldloca.s    0x95
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x96
         dup
         conv.i1
         stind.i1
         ldloca.s    0x96
         ldind.i1
         ldloca.s    0x96
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x97
         dup
         conv.i1
         stind.i1
         ldloca.s    0x97
         ldind.i1
         ldloca.s    0x97
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x98
         dup
         conv.i1
         stind.i1
         ldloca.s    0x98
         ldind.i1
         ldloca.s    0x98
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x99
         dup
         conv.i1
         stind.i1
         ldloca.s    0x99
         ldind.i1
         ldloca.s    0x99
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9a
         dup
         conv.i1
         stind.i1
         ldloca.s    0x9a
         ldind.i1
         ldloca.s    0x9a
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9b
         dup
         conv.i1
         stind.i1
         ldloca.s    0x9b
         ldind.i1
         ldloca.s    0x9b
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9c
         dup
         conv.i1
         stind.i1
         ldloca.s    0x9c
         ldind.i1
         ldloca.s    0x9c
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9d
         dup
         conv.i1
         stind.i1
         ldloca.s    0x9d
         ldind.i1
         ldloca.s    0x9d
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9e
         dup
         conv.i1
         stind.i1
         ldloca.s    0x9e
         ldind.i1
         ldloca.s    0x9e
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9f
         dup
         conv.i1
         stind.i1
         ldloca.s    0x9f
         ldind.i1
         ldloca.s    0x9f
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa0
         dup
         conv.i1
         stind.i1
         ldloca.s    0xa0
         ldind.i1
         ldloca.s    0xa0
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa1
         dup
         conv.i1
         stind.i1
         ldloca.s    0xa1
         ldind.i1
         ldloca.s    0xa1
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa2
         dup
         conv.i1
         stind.i1
         ldloca.s    0xa2
         ldind.i1
         ldloca.s    0xa2
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa3
         dup
         conv.i1
         stind.i1
         ldloca.s    0xa3
         ldind.i1
         ldloca.s    0xa3
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa4
         dup
         conv.i1
         stind.i1
         ldloca.s    0xa4
         ldind.i1
         ldloca.s    0xa4
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa5
         dup
         conv.i1
         stind.i1
         ldloca.s    0xa5
         ldind.i1
         ldloca.s    0xa5
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa6
         dup
         conv.i1
         stind.i1
         ldloca.s    0xa6
         ldind.i1
         ldloca.s    0xa6
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa7
         dup
         conv.i1
         stind.i1
         ldloca.s    0xa7
         ldind.i1
         ldloca.s    0xa7
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa8
         dup
         conv.i1
         stind.i1
         ldloca.s    0xa8
         ldind.i1
         ldloca.s    0xa8
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa9
         dup
         conv.i1
         stind.i1
         ldloca.s    0xa9
         ldind.i1
         ldloca.s    0xa9
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xaa
         dup
         conv.i1
         stind.i1
         ldloca.s    0xaa
         ldind.i1
         ldloca.s    0xaa
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xab
         dup
         conv.i1
         stind.i1
         ldloca.s    0xab
         ldind.i1
         ldloca.s    0xab
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xac
         dup
         conv.i1
         stind.i1
         ldloca.s    0xac
         ldind.i1
         ldloca.s    0xac
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xad
         dup
         conv.i1
         stind.i1
         ldloca.s    0xad
         ldind.i1
         ldloca.s    0xad
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xae
         dup
         conv.i1
         stind.i1
         ldloca.s    0xae
         ldind.i1
         ldloca.s    0xae
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xaf
         dup
         conv.i1
         stind.i1
         ldloca.s    0xaf
         ldind.i1
         ldloca.s    0xaf
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb0
         dup
         conv.i1
         stind.i1
         ldloca.s    0xb0
         ldind.i1
         ldloca.s    0xb0
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb1
         dup
         conv.i1
         stind.i1
         ldloca.s    0xb1
         ldind.i1
         ldloca.s    0xb1
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb2
         dup
         conv.i1
         stind.i1
         ldloca.s    0xb2
         ldind.i1
         ldloca.s    0xb2
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb3
         dup
         conv.i1
         stind.i1
         ldloca.s    0xb3
         ldind.i1
         ldloca.s    0xb3
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb4
         dup
         conv.i1
         stind.i1
         ldloca.s    0xb4
         ldind.i1
         ldloca.s    0xb4
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb5
         dup
         conv.i1
         stind.i1
         ldloca.s    0xb5
         ldind.i1
         ldloca.s    0xb5
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb6
         dup
         conv.i1
         stind.i1
         ldloca.s    0xb6
         ldind.i1
         ldloca.s    0xb6
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb7
         dup
         conv.i1
         stind.i1
         ldloca.s    0xb7
         ldind.i1
         ldloca.s    0xb7
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb8
         dup
         conv.i1
         stind.i1
         ldloca.s    0xb8
         ldind.i1
         ldloca.s    0xb8
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb9
         dup
         conv.i1
         stind.i1
         ldloca.s    0xb9
         ldind.i1
         ldloca.s    0xb9
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xba
         dup
         conv.i1
         stind.i1
         ldloca.s    0xba
         ldind.i1
         ldloca.s    0xba
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xbb
         dup
         conv.i1
         stind.i1
         ldloca.s    0xbb
         ldind.i1
         ldloca.s    0xbb
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xbc
         dup
         conv.i1
         stind.i1
         ldloca.s    0xbc
         ldind.i1
         ldloca.s    0xbc
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xbd
         dup
         conv.i1
         stind.i1
         ldloca.s    0xbd
         ldind.i1
         ldloca.s    0xbd
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xbe
         dup
         conv.i1
         stind.i1
         ldloca.s    0xbe
         ldind.i1
         ldloca.s    0xbe
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xbf
         dup
         conv.i1
         stind.i1
         ldloca.s    0xbf
         ldind.i1
         ldloca.s    0xbf
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc0
         dup
         conv.i1
         stind.i1
         ldloca.s    0xc0
         ldind.i1
         ldloca.s    0xc0
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc1
         dup
         conv.i1
         stind.i1
         ldloca.s    0xc1
         ldind.i1
         ldloca.s    0xc1
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc2
         dup
         conv.i1
         stind.i1
         ldloca.s    0xc2
         ldind.i1
         ldloca.s    0xc2
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc3
         dup
         conv.i1
         stind.i1
         ldloca.s    0xc3
         ldind.i1
         ldloca.s    0xc3
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc4
         dup
         conv.i1
         stind.i1
         ldloca.s    0xc4
         ldind.i1
         ldloca.s    0xc4
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc5
         dup
         conv.i1
         stind.i1
         ldloca.s    0xc5
         ldind.i1
         ldloca.s    0xc5
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc6
         dup
         conv.i1
         stind.i1
         ldloca.s    0xc6
         ldind.i1
         ldloca.s    0xc6
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc7
         dup
         conv.i1
         stind.i1
         ldloca.s    0xc7
         ldind.i1
         ldloca.s    0xc7
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc8
         dup
         conv.i1
         stind.i1
         ldloca.s    0xc8
         ldind.i1
         ldloca.s    0xc8
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc9
         dup
         conv.i1
         stind.i1
         ldloca.s    0xc9
         ldind.i1
         ldloca.s    0xc9
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xca
         dup
         conv.i1
         stind.i1
         ldloca.s    0xca
         ldind.i1
         ldloca.s    0xca
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xcb
         dup
         conv.i1
         stind.i1
         ldloca.s    0xcb
         ldind.i1
         ldloca.s    0xcb
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xcc
         dup
         conv.i1
         stind.i1
         ldloca.s    0xcc
         ldind.i1
         ldloca.s    0xcc
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xcd
         dup
         conv.i1
         stind.i1
         ldloca.s    0xcd
         ldind.i1
         ldloca.s    0xcd
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xce
         dup
         conv.i1
         stind.i1
         ldloca.s    0xce
         ldind.i1
         ldloca.s    0xce
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xcf
         dup
         conv.i1
         stind.i1
         ldloca.s    0xcf
         ldind.i1
         ldloca.s    0xcf
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd0
         dup
         conv.i1
         stind.i1
         ldloca.s    0xd0
         ldind.i1
         ldloca.s    0xd0
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd1
         dup
         conv.i1
         stind.i1
         ldloca.s    0xd1
         ldind.i1
         ldloca.s    0xd1
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd2
         dup
         conv.i1
         stind.i1
         ldloca.s    0xd2
         ldind.i1
         ldloca.s    0xd2
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd3
         dup
         conv.i1
         stind.i1
         ldloca.s    0xd3
         ldind.i1
         ldloca.s    0xd3
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd4
         dup
         conv.i1
         stind.i1
         ldloca.s    0xd4
         ldind.i1
         ldloca.s    0xd4
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd5
         dup
         conv.i1
         stind.i1
         ldloca.s    0xd5
         ldind.i1
         ldloca.s    0xd5
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd6
         dup
         conv.i1
         stind.i1
         ldloca.s    0xd6
         ldind.i1
         ldloca.s    0xd6
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd7
         dup
         conv.i1
         stind.i1
         ldloca.s    0xd7
         ldind.i1
         ldloca.s    0xd7
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd8
         dup
         conv.i1
         stind.i1
         ldloca.s    0xd8
         ldind.i1
         ldloca.s    0xd8
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd9
         dup
         conv.i1
         stind.i1
         ldloca.s    0xd9
         ldind.i1
         ldloca.s    0xd9
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xda
         dup
         conv.i1
         stind.i1
         ldloca.s    0xda
         ldind.i1
         ldloca.s    0xda
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xdb
         dup
         conv.i1
         stind.i1
         ldloca.s    0xdb
         ldind.i1
         ldloca.s    0xdb
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xdc
         dup
         conv.i1
         stind.i1
         ldloca.s    0xdc
         ldind.i1
         ldloca.s    0xdc
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xdd
         dup
         conv.i1
         stind.i1
         ldloca.s    0xdd
         ldind.i1
         ldloca.s    0xdd
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xde
         dup
         conv.i1
         stind.i1
         ldloca.s    0xde
         ldind.i1
         ldloca.s    0xde
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xdf
         dup
         conv.i1
         stind.i1
         ldloca.s    0xdf
         ldind.i1
         ldloca.s    0xdf
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe0
         dup
         conv.i1
         stind.i1
         ldloca.s    0xe0
         ldind.i1
         ldloca.s    0xe0
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe1
         dup
         conv.i1
         stind.i1
         ldloca.s    0xe1
         ldind.i1
         ldloca.s    0xe1
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe2
         dup
         conv.i1
         stind.i1
         ldloca.s    0xe2
         ldind.i1
         ldloca.s    0xe2
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe3
         dup
         conv.i1
         stind.i1
         ldloca.s    0xe3
         ldind.i1
         ldloca.s    0xe3
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe4
         dup
         conv.i1
         stind.i1
         ldloca.s    0xe4
         ldind.i1
         ldloca.s    0xe4
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe5
         dup
         conv.i1
         stind.i1
         ldloca.s    0xe5
         ldind.i1
         ldloca.s    0xe5
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe6
         dup
         conv.i1
         stind.i1
         ldloca.s    0xe6
         ldind.i1
         ldloca.s    0xe6
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe7
         dup
         conv.i1
         stind.i1
         ldloca.s    0xe7
         ldind.i1
         ldloca.s    0xe7
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe8
         dup
         conv.i1
         stind.i1
         ldloca.s    0xe8
         ldind.i1
         ldloca.s    0xe8
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe9
         dup
         conv.i1
         stind.i1
         ldloca.s    0xe9
         ldind.i1
         ldloca.s    0xe9
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xea
         dup
         conv.i1
         stind.i1
         ldloca.s    0xea
         ldind.i1
         ldloca.s    0xea
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xeb
         dup
         conv.i1
         stind.i1
         ldloca.s    0xeb
         ldind.i1
         ldloca.s    0xeb
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xec
         dup
         conv.i1
         stind.i1
         ldloca.s    0xec
         ldind.i1
         ldloca.s    0xec
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xed
         dup
         conv.i1
         stind.i1
         ldloca.s    0xed
         ldind.i1
         ldloca.s    0xed
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xee
         dup
         conv.i1
         stind.i1
         ldloca.s    0xee
         ldind.i1
         ldloca.s    0xee
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xef
         dup
         conv.i1
         stind.i1
         ldloca.s    0xef
         ldind.i1
         ldloca.s    0xef
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf0
         dup
         conv.i1
         stind.i1
         ldloca.s    0xf0
         ldind.i1
         ldloca.s    0xf0
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf1
         dup
         conv.i1
         stind.i1
         ldloca.s    0xf1
         ldind.i1
         ldloca.s    0xf1
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf2
         dup
         conv.i1
         stind.i1
         ldloca.s    0xf2
         ldind.i1
         ldloca.s    0xf2
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf3
         dup
         conv.i1
         stind.i1
         ldloca.s    0xf3
         ldind.i1
         ldloca.s    0xf3
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf4
         dup
         conv.i1
         stind.i1
         ldloca.s    0xf4
         ldind.i1
         ldloca.s    0xf4
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf5
         dup
         conv.i1
         stind.i1
         ldloca.s    0xf5
         ldind.i1
         ldloca.s    0xf5
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf6
         dup
         conv.i1
         stind.i1
         ldloca.s    0xf6
         ldind.i1
         ldloca.s    0xf6
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf7
         dup
         conv.i1
         stind.i1
         ldloca.s    0xf7
         ldind.i1
         ldloca.s    0xf7
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf8
         dup
         conv.i1
         stind.i1
         ldloca.s    0xf8
         ldind.i1
         ldloca.s    0xf8
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf9
         dup
         conv.i1
         stind.i1
         ldloca.s    0xf9
         ldind.i1
         ldloca.s    0xf9
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xfa
         dup
         conv.i1
         stind.i1
         ldloca.s    0xfa
         ldind.i1
         ldloca.s    0xfa
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xfb
         dup
         conv.i1
         stind.i1
         ldloca.s    0xfb
         ldind.i1
         ldloca.s    0xfb
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xfc
         dup
         conv.i1
         stind.i1
         ldloca.s    0xfc
         ldind.i1
         ldloca.s    0xfc
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xfd
         dup
         conv.i1
         stind.i1
         ldloca.s    0xfd
         ldind.i1
         ldloca.s    0xfd
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xfe
         dup
         conv.i1
         stind.i1
         ldloca.s    0xfe
         ldind.i1
         ldloca.s    0xfe
         conv.i1
         ceq
         brfalse    FAIL
         
         ldloca.s    0xff
         dup
         conv.i1
         stind.i1
         ldloca.s    0xff
         ldind.i1
         ldloca.s    0xff
         conv.i1
         ceq
         brfalse    FAIL
         
         ldc.i4.1
         ret
         
FAIL:
        ldc.i4.0
        ret         
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack       2
        call            int32 ldloca_s_i1::test_int8()
        ldc.i4.1
        ceq
        brfalse         FAIL

        ldc.i4 100
        ret

FAIL:
        ldc.i4          0x0
        ret

}
}
