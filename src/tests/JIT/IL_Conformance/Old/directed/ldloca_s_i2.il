// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly ldloca_s_i2.exe {}
.class ldloca_s_i2 {

.method public static int32 test_int16()
{
.locals (int16 l000, int16 l001, int16 l002, int16 l003, int16 l004, int16 l005, int16 l006, int16 l007,
         int16 l008, int16 l009, int16 l010, int16 l011, int16 l012, int16 l013, int16 l014, int16 l015,
         int16 l016, int16 l017, int16 l018, int16 l019, int16 l020, int16 l021, int16 l022, int16 l023,
         int16 l024, int16 l025, int16 l026, int16 l027, int16 l028, int16 l029, int16 l030, int16 l031,
         int16 l032, int16 l033, int16 l034, int16 l035, int16 l036, int16 l037, int16 l038, int16 l039,
         int16 l040, int16 l041, int16 l042, int16 l043, int16 l044, int16 l045, int16 l046, int16 l047,
         int16 l048, int16 l049, int16 l050, int16 l051, int16 l052, int16 l053, int16 l054, int16 l055,
         int16 l056, int16 l057, int16 l058, int16 l059, int16 l060, int16 l061, int16 l062, int16 l063,
         
         int16 l064, int16 l065, int16 l066, int16 l067, int16 l068, int16 l069, int16 l070, int16 l071,
         int16 l072, int16 l073, int16 l074, int16 l075, int16 l076, int16 l077, int16 l078, int16 l079,
         int16 l080, int16 l081, int16 l082, int16 l083, int16 l084, int16 l085, int16 l086, int16 l087,
         int16 l088, int16 l089, int16 l090, int16 l091, int16 l092, int16 l093, int16 l094, int16 l095,
         int16 l096, int16 l097, int16 l098, int16 l099, int16 l100, int16 l101, int16 l102, int16 l103,
         int16 l104, int16 l105, int16 l106, int16 l107, int16 l108, int16 l109, int16 l110, int16 l111,
         int16 l112, int16 l113, int16 l114, int16 l115, int16 l116, int16 l117, int16 l118, int16 l119,
         int16 l120, int16 l121, int16 l122, int16 l123, int16 l124, int16 l125, int16 l126, int16 l127,
         
         int16 l128, int16 l129, int16 l130, int16 l131, int16 l132, int16 l133, int16 l134, int16 l135,
         int16 l136, int16 l137, int16 l138, int16 l139, int16 l140, int16 l141, int16 l142, int16 l143,
         int16 l144, int16 l145, int16 l146, int16 l147, int16 l148, int16 l149, int16 l150, int16 l151,
         int16 l152, int16 l153, int16 l154, int16 l155, int16 l156, int16 l157, int16 l158, int16 l159,
         int16 l160, int16 l161, int16 l162, int16 l163, int16 l164, int16 l165, int16 l166, int16 l167,
         int16 l168, int16 l169, int16 l170, int16 l171, int16 l172, int16 l173, int16 l174, int16 l175,
         int16 l176, int16 l177, int16 l178, int16 l179, int16 l180, int16 l181, int16 l182, int16 l183,
         int16 l184, int16 l185, int16 l186, int16 l187, int16 l188, int16 l189, int16 l190, int16 l191,
         
         int16 l192, int16 l193, int16 l194, int16 l195, int16 l196, int16 l197, int16 l198, int16 l199,
         int16 l200, int16 l201, int16 l202, int16 l203, int16 l204, int16 l205, int16 l206, int16 l207,
         int16 l208, int16 l209, int16 l210, int16 l211, int16 l212, int16 l213, int16 l214, int16 l215,
         int16 l216, int16 l217, int16 l218, int16 l219, int16 l220, int16 l221, int16 l222, int16 l223,
         int16 l224, int16 l225, int16 l226, int16 l227, int16 l228, int16 l229, int16 l230, int16 l231,
         int16 l232, int16 l233, int16 l234, int16 l235, int16 l236, int16 l237, int16 l238, int16 l239,
         int16 l240, int16 l241, int16 l242, int16 l243, int16 l244, int16 l245, int16 l246, int16 l247,
         int16 l248, int16 l249, int16 l250, int16 l251, int16 l252, int16 l253, int16 l254, int16 l255)
.zeroinit
.maxstack       2

         ldloca.s    0x00
         dup
         conv.i2
         stind.i2
         ldloca.s    0x00
         ldind.i2
         ldloca.s    0x00
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x01
         dup
         conv.i2
         stind.i2
         ldloca.s    0x01
         ldind.i2
         ldloca.s    0x01
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x02
         dup
         conv.i2
         stind.i2
         ldloca.s    0x02
         ldind.i2
         ldloca.s    0x02
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x03
         dup
         conv.i2
         stind.i2
         ldloca.s    0x03
         ldind.i2
         ldloca.s    0x03
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x04
         dup
         conv.i2
         stind.i2
         ldloca.s    0x04
         ldind.i2
         ldloca.s    0x04
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x05
         dup
         conv.i2
         stind.i2
         ldloca.s    0x05
         ldind.i2
         ldloca.s    0x05
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x06
         dup
         conv.i2
         stind.i2
         ldloca.s    0x06
         ldind.i2
         ldloca.s    0x06
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x07
         dup
         conv.i2
         stind.i2
         ldloca.s    0x07
         ldind.i2
         ldloca.s    0x07
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x08
         dup
         conv.i2
         stind.i2
         ldloca.s    0x08
         ldind.i2
         ldloca.s    0x08
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x09
         dup
         conv.i2
         stind.i2
         ldloca.s    0x09
         ldind.i2
         ldloca.s    0x09
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0a
         dup
         conv.i2
         stind.i2
         ldloca.s    0x0a
         ldind.i2
         ldloca.s    0x0a
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0b
         dup
         conv.i2
         stind.i2
         ldloca.s    0x0b
         ldind.i2
         ldloca.s    0x0b
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0c
         dup
         conv.i2
         stind.i2
         ldloca.s    0x0c
         ldind.i2
         ldloca.s    0x0c
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0d
         dup
         conv.i2
         stind.i2
         ldloca.s    0x0d
         ldind.i2
         ldloca.s    0x0d
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0e
         dup
         conv.i2
         stind.i2
         ldloca.s    0x0e
         ldind.i2
         ldloca.s    0x0e
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x0f
         dup
         conv.i2
         stind.i2
         ldloca.s    0x0f
         ldind.i2
         ldloca.s    0x0f
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x10
         dup
         conv.i2
         stind.i2
         ldloca.s    0x10
         ldind.i2
         ldloca.s    0x10
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x11
         dup
         conv.i2
         stind.i2
         ldloca.s    0x11
         ldind.i2
         ldloca.s    0x11
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x12
         dup
         conv.i2
         stind.i2
         ldloca.s    0x12
         ldind.i2
         ldloca.s    0x12
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x13
         dup
         conv.i2
         stind.i2
         ldloca.s    0x13
         ldind.i2
         ldloca.s    0x13
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x14
         dup
         conv.i2
         stind.i2
         ldloca.s    0x14
         ldind.i2
         ldloca.s    0x14
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x15
         dup
         conv.i2
         stind.i2
         ldloca.s    0x15
         ldind.i2
         ldloca.s    0x15
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x16
         dup
         conv.i2
         stind.i2
         ldloca.s    0x16
         ldind.i2
         ldloca.s    0x16
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x17
         dup
         conv.i2
         stind.i2
         ldloca.s    0x17
         ldind.i2
         ldloca.s    0x17
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x18
         dup
         conv.i2
         stind.i2
         ldloca.s    0x18
         ldind.i2
         ldloca.s    0x18
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x19
         dup
         conv.i2
         stind.i2
         ldloca.s    0x19
         ldind.i2
         ldloca.s    0x19
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1a
         dup
         conv.i2
         stind.i2
         ldloca.s    0x1a
         ldind.i2
         ldloca.s    0x1a
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1b
         dup
         conv.i2
         stind.i2
         ldloca.s    0x1b
         ldind.i2
         ldloca.s    0x1b
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1c
         dup
         conv.i2
         stind.i2
         ldloca.s    0x1c
         ldind.i2
         ldloca.s    0x1c
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1d
         dup
         conv.i2
         stind.i2
         ldloca.s    0x1d
         ldind.i2
         ldloca.s    0x1d
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1e
         dup
         conv.i2
         stind.i2
         ldloca.s    0x1e
         ldind.i2
         ldloca.s    0x1e
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x1f
         dup
         conv.i2
         stind.i2
         ldloca.s    0x1f
         ldind.i2
         ldloca.s    0x1f
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x20
         dup
         conv.i2
         stind.i2
         ldloca.s    0x20
         ldind.i2
         ldloca.s    0x20
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x21
         dup
         conv.i2
         stind.i2
         ldloca.s    0x21
         ldind.i2
         ldloca.s    0x21
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x22
         dup
         conv.i2
         stind.i2
         ldloca.s    0x22
         ldind.i2
         ldloca.s    0x22
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x23
         dup
         conv.i2
         stind.i2
         ldloca.s    0x23
         ldind.i2
         ldloca.s    0x23
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x24
         dup
         conv.i2
         stind.i2
         ldloca.s    0x24
         ldind.i2
         ldloca.s    0x24
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x25
         dup
         conv.i2
         stind.i2
         ldloca.s    0x25
         ldind.i2
         ldloca.s    0x25
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x26
         dup
         conv.i2
         stind.i2
         ldloca.s    0x26
         ldind.i2
         ldloca.s    0x26
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x27
         dup
         conv.i2
         stind.i2
         ldloca.s    0x27
         ldind.i2
         ldloca.s    0x27
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x28
         dup
         conv.i2
         stind.i2
         ldloca.s    0x28
         ldind.i2
         ldloca.s    0x28
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x29
         dup
         conv.i2
         stind.i2
         ldloca.s    0x29
         ldind.i2
         ldloca.s    0x29
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2a
         dup
         conv.i2
         stind.i2
         ldloca.s    0x2a
         ldind.i2
         ldloca.s    0x2a
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2b
         dup
         conv.i2
         stind.i2
         ldloca.s    0x2b
         ldind.i2
         ldloca.s    0x2b
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2c
         dup
         conv.i2
         stind.i2
         ldloca.s    0x2c
         ldind.i2
         ldloca.s    0x2c
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2d
         dup
         conv.i2
         stind.i2
         ldloca.s    0x2d
         ldind.i2
         ldloca.s    0x2d
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2e
         dup
         conv.i2
         stind.i2
         ldloca.s    0x2e
         ldind.i2
         ldloca.s    0x2e
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x2f
         dup
         conv.i2
         stind.i2
         ldloca.s    0x2f
         ldind.i2
         ldloca.s    0x2f
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x30
         dup
         conv.i2
         stind.i2
         ldloca.s    0x30
         ldind.i2
         ldloca.s    0x30
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x31
         dup
         conv.i2
         stind.i2
         ldloca.s    0x31
         ldind.i2
         ldloca.s    0x31
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x32
         dup
         conv.i2
         stind.i2
         ldloca.s    0x32
         ldind.i2
         ldloca.s    0x32
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x33
         dup
         conv.i2
         stind.i2
         ldloca.s    0x33
         ldind.i2
         ldloca.s    0x33
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x34
         dup
         conv.i2
         stind.i2
         ldloca.s    0x34
         ldind.i2
         ldloca.s    0x34
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x35
         dup
         conv.i2
         stind.i2
         ldloca.s    0x35
         ldind.i2
         ldloca.s    0x35
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x36
         dup
         conv.i2
         stind.i2
         ldloca.s    0x36
         ldind.i2
         ldloca.s    0x36
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x37
         dup
         conv.i2
         stind.i2
         ldloca.s    0x37
         ldind.i2
         ldloca.s    0x37
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x38
         dup
         conv.i2
         stind.i2
         ldloca.s    0x38
         ldind.i2
         ldloca.s    0x38
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x39
         dup
         conv.i2
         stind.i2
         ldloca.s    0x39
         ldind.i2
         ldloca.s    0x39
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3a
         dup
         conv.i2
         stind.i2
         ldloca.s    0x3a
         ldind.i2
         ldloca.s    0x3a
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3b
         dup
         conv.i2
         stind.i2
         ldloca.s    0x3b
         ldind.i2
         ldloca.s    0x3b
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3c
         dup
         conv.i2
         stind.i2
         ldloca.s    0x3c
         ldind.i2
         ldloca.s    0x3c
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3d
         dup
         conv.i2
         stind.i2
         ldloca.s    0x3d
         ldind.i2
         ldloca.s    0x3d
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3e
         dup
         conv.i2
         stind.i2
         ldloca.s    0x3e
         ldind.i2
         ldloca.s    0x3e
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x3f
         dup
         conv.i2
         stind.i2
         ldloca.s    0x3f
         ldind.i2
         ldloca.s    0x3f
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x40
         dup
         conv.i2
         stind.i2
         ldloca.s    0x40
         ldind.i2
         ldloca.s    0x40
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x41
         dup
         conv.i2
         stind.i2
         ldloca.s    0x41
         ldind.i2
         ldloca.s    0x41
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x42
         dup
         conv.i2
         stind.i2
         ldloca.s    0x42
         ldind.i2
         ldloca.s    0x42
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x43
         dup
         conv.i2
         stind.i2
         ldloca.s    0x43
         ldind.i2
         ldloca.s    0x43
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x44
         dup
         conv.i2
         stind.i2
         ldloca.s    0x44
         ldind.i2
         ldloca.s    0x44
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x45
         dup
         conv.i2
         stind.i2
         ldloca.s    0x45
         ldind.i2
         ldloca.s    0x45
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x46
         dup
         conv.i2
         stind.i2
         ldloca.s    0x46
         ldind.i2
         ldloca.s    0x46
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x47
         dup
         conv.i2
         stind.i2
         ldloca.s    0x47
         ldind.i2
         ldloca.s    0x47
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x48
         dup
         conv.i2
         stind.i2
         ldloca.s    0x48
         ldind.i2
         ldloca.s    0x48
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x49
         dup
         conv.i2
         stind.i2
         ldloca.s    0x49
         ldind.i2
         ldloca.s    0x49
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4a
         dup
         conv.i2
         stind.i2
         ldloca.s    0x4a
         ldind.i2
         ldloca.s    0x4a
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4b
         dup
         conv.i2
         stind.i2
         ldloca.s    0x4b
         ldind.i2
         ldloca.s    0x4b
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4c
         dup
         conv.i2
         stind.i2
         ldloca.s    0x4c
         ldind.i2
         ldloca.s    0x4c
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4d
         dup
         conv.i2
         stind.i2
         ldloca.s    0x4d
         ldind.i2
         ldloca.s    0x4d
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4e
         dup
         conv.i2
         stind.i2
         ldloca.s    0x4e
         ldind.i2
         ldloca.s    0x4e
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x4f
         dup
         conv.i2
         stind.i2
         ldloca.s    0x4f
         ldind.i2
         ldloca.s    0x4f
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x50
         dup
         conv.i2
         stind.i2
         ldloca.s    0x50
         ldind.i2
         ldloca.s    0x50
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x51
         dup
         conv.i2
         stind.i2
         ldloca.s    0x51
         ldind.i2
         ldloca.s    0x51
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x52
         dup
         conv.i2
         stind.i2
         ldloca.s    0x52
         ldind.i2
         ldloca.s    0x52
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x53
         dup
         conv.i2
         stind.i2
         ldloca.s    0x53
         ldind.i2
         ldloca.s    0x53
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x54
         dup
         conv.i2
         stind.i2
         ldloca.s    0x54
         ldind.i2
         ldloca.s    0x54
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x55
         dup
         conv.i2
         stind.i2
         ldloca.s    0x55
         ldind.i2
         ldloca.s    0x55
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x56
         dup
         conv.i2
         stind.i2
         ldloca.s    0x56
         ldind.i2
         ldloca.s    0x56
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x57
         dup
         conv.i2
         stind.i2
         ldloca.s    0x57
         ldind.i2
         ldloca.s    0x57
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x58
         dup
         conv.i2
         stind.i2
         ldloca.s    0x58
         ldind.i2
         ldloca.s    0x58
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x59
         dup
         conv.i2
         stind.i2
         ldloca.s    0x59
         ldind.i2
         ldloca.s    0x59
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5a
         dup
         conv.i2
         stind.i2
         ldloca.s    0x5a
         ldind.i2
         ldloca.s    0x5a
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5b
         dup
         conv.i2
         stind.i2
         ldloca.s    0x5b
         ldind.i2
         ldloca.s    0x5b
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5c
         dup
         conv.i2
         stind.i2
         ldloca.s    0x5c
         ldind.i2
         ldloca.s    0x5c
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5d
         dup
         conv.i2
         stind.i2
         ldloca.s    0x5d
         ldind.i2
         ldloca.s    0x5d
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5e
         dup
         conv.i2
         stind.i2
         ldloca.s    0x5e
         ldind.i2
         ldloca.s    0x5e
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x5f
         dup
         conv.i2
         stind.i2
         ldloca.s    0x5f
         ldind.i2
         ldloca.s    0x5f
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x60
         dup
         conv.i2
         stind.i2
         ldloca.s    0x60
         ldind.i2
         ldloca.s    0x60
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x61
         dup
         conv.i2
         stind.i2
         ldloca.s    0x61
         ldind.i2
         ldloca.s    0x61
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x62
         dup
         conv.i2
         stind.i2
         ldloca.s    0x62
         ldind.i2
         ldloca.s    0x62
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x63
         dup
         conv.i2
         stind.i2
         ldloca.s    0x63
         ldind.i2
         ldloca.s    0x63
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x64
         dup
         conv.i2
         stind.i2
         ldloca.s    0x64
         ldind.i2
         ldloca.s    0x64
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x65
         dup
         conv.i2
         stind.i2
         ldloca.s    0x65
         ldind.i2
         ldloca.s    0x65
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x66
         dup
         conv.i2
         stind.i2
         ldloca.s    0x66
         ldind.i2
         ldloca.s    0x66
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x67
         dup
         conv.i2
         stind.i2
         ldloca.s    0x67
         ldind.i2
         ldloca.s    0x67
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x68
         dup
         conv.i2
         stind.i2
         ldloca.s    0x68
         ldind.i2
         ldloca.s    0x68
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x69
         dup
         conv.i2
         stind.i2
         ldloca.s    0x69
         ldind.i2
         ldloca.s    0x69
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6a
         dup
         conv.i2
         stind.i2
         ldloca.s    0x6a
         ldind.i2
         ldloca.s    0x6a
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6b
         dup
         conv.i2
         stind.i2
         ldloca.s    0x6b
         ldind.i2
         ldloca.s    0x6b
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6c
         dup
         conv.i2
         stind.i2
         ldloca.s    0x6c
         ldind.i2
         ldloca.s    0x6c
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6d
         dup
         conv.i2
         stind.i2
         ldloca.s    0x6d
         ldind.i2
         ldloca.s    0x6d
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6e
         dup
         conv.i2
         stind.i2
         ldloca.s    0x6e
         ldind.i2
         ldloca.s    0x6e
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x6f
         dup
         conv.i2
         stind.i2
         ldloca.s    0x6f
         ldind.i2
         ldloca.s    0x6f
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x70
         dup
         conv.i2
         stind.i2
         ldloca.s    0x70
         ldind.i2
         ldloca.s    0x70
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x71
         dup
         conv.i2
         stind.i2
         ldloca.s    0x71
         ldind.i2
         ldloca.s    0x71
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x72
         dup
         conv.i2
         stind.i2
         ldloca.s    0x72
         ldind.i2
         ldloca.s    0x72
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x73
         dup
         conv.i2
         stind.i2
         ldloca.s    0x73
         ldind.i2
         ldloca.s    0x73
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x74
         dup
         conv.i2
         stind.i2
         ldloca.s    0x74
         ldind.i2
         ldloca.s    0x74
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x75
         dup
         conv.i2
         stind.i2
         ldloca.s    0x75
         ldind.i2
         ldloca.s    0x75
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x76
         dup
         conv.i2
         stind.i2
         ldloca.s    0x76
         ldind.i2
         ldloca.s    0x76
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x77
         dup
         conv.i2
         stind.i2
         ldloca.s    0x77
         ldind.i2
         ldloca.s    0x77
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x78
         dup
         conv.i2
         stind.i2
         ldloca.s    0x78
         ldind.i2
         ldloca.s    0x78
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x79
         dup
         conv.i2
         stind.i2
         ldloca.s    0x79
         ldind.i2
         ldloca.s    0x79
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7a
         dup
         conv.i2
         stind.i2
         ldloca.s    0x7a
         ldind.i2
         ldloca.s    0x7a
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7b
         dup
         conv.i2
         stind.i2
         ldloca.s    0x7b
         ldind.i2
         ldloca.s    0x7b
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7c
         dup
         conv.i2
         stind.i2
         ldloca.s    0x7c
         ldind.i2
         ldloca.s    0x7c
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7d
         dup
         conv.i2
         stind.i2
         ldloca.s    0x7d
         ldind.i2
         ldloca.s    0x7d
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7e
         dup
         conv.i2
         stind.i2
         ldloca.s    0x7e
         ldind.i2
         ldloca.s    0x7e
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x7f
         dup
         conv.i2
         stind.i2
         ldloca.s    0x7f
         ldind.i2
         ldloca.s    0x7f
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x80
         dup
         conv.i2
         stind.i2
         ldloca.s    0x80
         ldind.i2
         ldloca.s    0x80
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x81
         dup
         conv.i2
         stind.i2
         ldloca.s    0x81
         ldind.i2
         ldloca.s    0x81
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x82
         dup
         conv.i2
         stind.i2
         ldloca.s    0x82
         ldind.i2
         ldloca.s    0x82
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x83
         dup
         conv.i2
         stind.i2
         ldloca.s    0x83
         ldind.i2
         ldloca.s    0x83
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x84
         dup
         conv.i2
         stind.i2
         ldloca.s    0x84
         ldind.i2
         ldloca.s    0x84
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x85
         dup
         conv.i2
         stind.i2
         ldloca.s    0x85
         ldind.i2
         ldloca.s    0x85
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x86
         dup
         conv.i2
         stind.i2
         ldloca.s    0x86
         ldind.i2
         ldloca.s    0x86
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x87
         dup
         conv.i2
         stind.i2
         ldloca.s    0x87
         ldind.i2
         ldloca.s    0x87
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x88
         dup
         conv.i2
         stind.i2
         ldloca.s    0x88
         ldind.i2
         ldloca.s    0x88
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x89
         dup
         conv.i2
         stind.i2
         ldloca.s    0x89
         ldind.i2
         ldloca.s    0x89
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8a
         dup
         conv.i2
         stind.i2
         ldloca.s    0x8a
         ldind.i2
         ldloca.s    0x8a
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8b
         dup
         conv.i2
         stind.i2
         ldloca.s    0x8b
         ldind.i2
         ldloca.s    0x8b
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8c
         dup
         conv.i2
         stind.i2
         ldloca.s    0x8c
         ldind.i2
         ldloca.s    0x8c
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8d
         dup
         conv.i2
         stind.i2
         ldloca.s    0x8d
         ldind.i2
         ldloca.s    0x8d
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8e
         dup
         conv.i2
         stind.i2
         ldloca.s    0x8e
         ldind.i2
         ldloca.s    0x8e
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x8f
         dup
         conv.i2
         stind.i2
         ldloca.s    0x8f
         ldind.i2
         ldloca.s    0x8f
         conv.i2
         ceq
         brfalse    FAIL

         ldloca.s    0x90
         dup
         conv.i2
         stind.i2
         ldloca.s    0x90
         ldind.i2
         ldloca.s    0x90
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x91
         dup
         conv.i2
         stind.i2
         ldloca.s    0x91
         ldind.i2
         ldloca.s    0x91
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x92
         dup
         conv.i2
         stind.i2
         ldloca.s    0x92
         ldind.i2
         ldloca.s    0x92
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x93
         dup
         conv.i2
         stind.i2
         ldloca.s    0x93
         ldind.i2
         ldloca.s    0x93
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x94
         dup
         conv.i2
         stind.i2
         ldloca.s    0x94
         ldind.i2
         ldloca.s    0x94
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x95
         dup
         conv.i2
         stind.i2
         ldloca.s    0x95
         ldind.i2
         ldloca.s    0x95
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x96
         dup
         conv.i2
         stind.i2
         ldloca.s    0x96
         ldind.i2
         ldloca.s    0x96
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x97
         dup
         conv.i2
         stind.i2
         ldloca.s    0x97
         ldind.i2
         ldloca.s    0x97
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x98
         dup
         conv.i2
         stind.i2
         ldloca.s    0x98
         ldind.i2
         ldloca.s    0x98
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x99
         dup
         conv.i2
         stind.i2
         ldloca.s    0x99
         ldind.i2
         ldloca.s    0x99
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9a
         dup
         conv.i2
         stind.i2
         ldloca.s    0x9a
         ldind.i2
         ldloca.s    0x9a
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9b
         dup
         conv.i2
         stind.i2
         ldloca.s    0x9b
         ldind.i2
         ldloca.s    0x9b
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9c
         dup
         conv.i2
         stind.i2
         ldloca.s    0x9c
         ldind.i2
         ldloca.s    0x9c
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9d
         dup
         conv.i2
         stind.i2
         ldloca.s    0x9d
         ldind.i2
         ldloca.s    0x9d
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9e
         dup
         conv.i2
         stind.i2
         ldloca.s    0x9e
         ldind.i2
         ldloca.s    0x9e
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0x9f
         dup
         conv.i2
         stind.i2
         ldloca.s    0x9f
         ldind.i2
         ldloca.s    0x9f
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa0
         dup
         conv.i2
         stind.i2
         ldloca.s    0xa0
         ldind.i2
         ldloca.s    0xa0
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa1
         dup
         conv.i2
         stind.i2
         ldloca.s    0xa1
         ldind.i2
         ldloca.s    0xa1
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa2
         dup
         conv.i2
         stind.i2
         ldloca.s    0xa2
         ldind.i2
         ldloca.s    0xa2
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa3
         dup
         conv.i2
         stind.i2
         ldloca.s    0xa3
         ldind.i2
         ldloca.s    0xa3
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa4
         dup
         conv.i2
         stind.i2
         ldloca.s    0xa4
         ldind.i2
         ldloca.s    0xa4
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa5
         dup
         conv.i2
         stind.i2
         ldloca.s    0xa5
         ldind.i2
         ldloca.s    0xa5
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa6
         dup
         conv.i2
         stind.i2
         ldloca.s    0xa6
         ldind.i2
         ldloca.s    0xa6
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa7
         dup
         conv.i2
         stind.i2
         ldloca.s    0xa7
         ldind.i2
         ldloca.s    0xa7
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa8
         dup
         conv.i2
         stind.i2
         ldloca.s    0xa8
         ldind.i2
         ldloca.s    0xa8
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xa9
         dup
         conv.i2
         stind.i2
         ldloca.s    0xa9
         ldind.i2
         ldloca.s    0xa9
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xaa
         dup
         conv.i2
         stind.i2
         ldloca.s    0xaa
         ldind.i2
         ldloca.s    0xaa
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xab
         dup
         conv.i2
         stind.i2
         ldloca.s    0xab
         ldind.i2
         ldloca.s    0xab
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xac
         dup
         conv.i2
         stind.i2
         ldloca.s    0xac
         ldind.i2
         ldloca.s    0xac
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xad
         dup
         conv.i2
         stind.i2
         ldloca.s    0xad
         ldind.i2
         ldloca.s    0xad
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xae
         dup
         conv.i2
         stind.i2
         ldloca.s    0xae
         ldind.i2
         ldloca.s    0xae
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xaf
         dup
         conv.i2
         stind.i2
         ldloca.s    0xaf
         ldind.i2
         ldloca.s    0xaf
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb0
         dup
         conv.i2
         stind.i2
         ldloca.s    0xb0
         ldind.i2
         ldloca.s    0xb0
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb1
         dup
         conv.i2
         stind.i2
         ldloca.s    0xb1
         ldind.i2
         ldloca.s    0xb1
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb2
         dup
         conv.i2
         stind.i2
         ldloca.s    0xb2
         ldind.i2
         ldloca.s    0xb2
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb3
         dup
         conv.i2
         stind.i2
         ldloca.s    0xb3
         ldind.i2
         ldloca.s    0xb3
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb4
         dup
         conv.i2
         stind.i2
         ldloca.s    0xb4
         ldind.i2
         ldloca.s    0xb4
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb5
         dup
         conv.i2
         stind.i2
         ldloca.s    0xb5
         ldind.i2
         ldloca.s    0xb5
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb6
         dup
         conv.i2
         stind.i2
         ldloca.s    0xb6
         ldind.i2
         ldloca.s    0xb6
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb7
         dup
         conv.i2
         stind.i2
         ldloca.s    0xb7
         ldind.i2
         ldloca.s    0xb7
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb8
         dup
         conv.i2
         stind.i2
         ldloca.s    0xb8
         ldind.i2
         ldloca.s    0xb8
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xb9
         dup
         conv.i2
         stind.i2
         ldloca.s    0xb9
         ldind.i2
         ldloca.s    0xb9
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xba
         dup
         conv.i2
         stind.i2
         ldloca.s    0xba
         ldind.i2
         ldloca.s    0xba
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xbb
         dup
         conv.i2
         stind.i2
         ldloca.s    0xbb
         ldind.i2
         ldloca.s    0xbb
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xbc
         dup
         conv.i2
         stind.i2
         ldloca.s    0xbc
         ldind.i2
         ldloca.s    0xbc
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xbd
         dup
         conv.i2
         stind.i2
         ldloca.s    0xbd
         ldind.i2
         ldloca.s    0xbd
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xbe
         dup
         conv.i2
         stind.i2
         ldloca.s    0xbe
         ldind.i2
         ldloca.s    0xbe
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xbf
         dup
         conv.i2
         stind.i2
         ldloca.s    0xbf
         ldind.i2
         ldloca.s    0xbf
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc0
         dup
         conv.i2
         stind.i2
         ldloca.s    0xc0
         ldind.i2
         ldloca.s    0xc0
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc1
         dup
         conv.i2
         stind.i2
         ldloca.s    0xc1
         ldind.i2
         ldloca.s    0xc1
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc2
         dup
         conv.i2
         stind.i2
         ldloca.s    0xc2
         ldind.i2
         ldloca.s    0xc2
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc3
         dup
         conv.i2
         stind.i2
         ldloca.s    0xc3
         ldind.i2
         ldloca.s    0xc3
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc4
         dup
         conv.i2
         stind.i2
         ldloca.s    0xc4
         ldind.i2
         ldloca.s    0xc4
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc5
         dup
         conv.i2
         stind.i2
         ldloca.s    0xc5
         ldind.i2
         ldloca.s    0xc5
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc6
         dup
         conv.i2
         stind.i2
         ldloca.s    0xc6
         ldind.i2
         ldloca.s    0xc6
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc7
         dup
         conv.i2
         stind.i2
         ldloca.s    0xc7
         ldind.i2
         ldloca.s    0xc7
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc8
         dup
         conv.i2
         stind.i2
         ldloca.s    0xc8
         ldind.i2
         ldloca.s    0xc8
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xc9
         dup
         conv.i2
         stind.i2
         ldloca.s    0xc9
         ldind.i2
         ldloca.s    0xc9
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xca
         dup
         conv.i2
         stind.i2
         ldloca.s    0xca
         ldind.i2
         ldloca.s    0xca
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xcb
         dup
         conv.i2
         stind.i2
         ldloca.s    0xcb
         ldind.i2
         ldloca.s    0xcb
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xcc
         dup
         conv.i2
         stind.i2
         ldloca.s    0xcc
         ldind.i2
         ldloca.s    0xcc
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xcd
         dup
         conv.i2
         stind.i2
         ldloca.s    0xcd
         ldind.i2
         ldloca.s    0xcd
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xce
         dup
         conv.i2
         stind.i2
         ldloca.s    0xce
         ldind.i2
         ldloca.s    0xce
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xcf
         dup
         conv.i2
         stind.i2
         ldloca.s    0xcf
         ldind.i2
         ldloca.s    0xcf
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd0
         dup
         conv.i2
         stind.i2
         ldloca.s    0xd0
         ldind.i2
         ldloca.s    0xd0
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd1
         dup
         conv.i2
         stind.i2
         ldloca.s    0xd1
         ldind.i2
         ldloca.s    0xd1
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd2
         dup
         conv.i2
         stind.i2
         ldloca.s    0xd2
         ldind.i2
         ldloca.s    0xd2
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd3
         dup
         conv.i2
         stind.i2
         ldloca.s    0xd3
         ldind.i2
         ldloca.s    0xd3
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd4
         dup
         conv.i2
         stind.i2
         ldloca.s    0xd4
         ldind.i2
         ldloca.s    0xd4
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd5
         dup
         conv.i2
         stind.i2
         ldloca.s    0xd5
         ldind.i2
         ldloca.s    0xd5
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd6
         dup
         conv.i2
         stind.i2
         ldloca.s    0xd6
         ldind.i2
         ldloca.s    0xd6
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd7
         dup
         conv.i2
         stind.i2
         ldloca.s    0xd7
         ldind.i2
         ldloca.s    0xd7
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd8
         dup
         conv.i2
         stind.i2
         ldloca.s    0xd8
         ldind.i2
         ldloca.s    0xd8
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xd9
         dup
         conv.i2
         stind.i2
         ldloca.s    0xd9
         ldind.i2
         ldloca.s    0xd9
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xda
         dup
         conv.i2
         stind.i2
         ldloca.s    0xda
         ldind.i2
         ldloca.s    0xda
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xdb
         dup
         conv.i2
         stind.i2
         ldloca.s    0xdb
         ldind.i2
         ldloca.s    0xdb
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xdc
         dup
         conv.i2
         stind.i2
         ldloca.s    0xdc
         ldind.i2
         ldloca.s    0xdc
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xdd
         dup
         conv.i2
         stind.i2
         ldloca.s    0xdd
         ldind.i2
         ldloca.s    0xdd
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xde
         dup
         conv.i2
         stind.i2
         ldloca.s    0xde
         ldind.i2
         ldloca.s    0xde
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xdf
         dup
         conv.i2
         stind.i2
         ldloca.s    0xdf
         ldind.i2
         ldloca.s    0xdf
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe0
         dup
         conv.i2
         stind.i2
         ldloca.s    0xe0
         ldind.i2
         ldloca.s    0xe0
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe1
         dup
         conv.i2
         stind.i2
         ldloca.s    0xe1
         ldind.i2
         ldloca.s    0xe1
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe2
         dup
         conv.i2
         stind.i2
         ldloca.s    0xe2
         ldind.i2
         ldloca.s    0xe2
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe3
         dup
         conv.i2
         stind.i2
         ldloca.s    0xe3
         ldind.i2
         ldloca.s    0xe3
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe4
         dup
         conv.i2
         stind.i2
         ldloca.s    0xe4
         ldind.i2
         ldloca.s    0xe4
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe5
         dup
         conv.i2
         stind.i2
         ldloca.s    0xe5
         ldind.i2
         ldloca.s    0xe5
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe6
         dup
         conv.i2
         stind.i2
         ldloca.s    0xe6
         ldind.i2
         ldloca.s    0xe6
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe7
         dup
         conv.i2
         stind.i2
         ldloca.s    0xe7
         ldind.i2
         ldloca.s    0xe7
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe8
         dup
         conv.i2
         stind.i2
         ldloca.s    0xe8
         ldind.i2
         ldloca.s    0xe8
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xe9
         dup
         conv.i2
         stind.i2
         ldloca.s    0xe9
         ldind.i2
         ldloca.s    0xe9
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xea
         dup
         conv.i2
         stind.i2
         ldloca.s    0xea
         ldind.i2
         ldloca.s    0xea
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xeb
         dup
         conv.i2
         stind.i2
         ldloca.s    0xeb
         ldind.i2
         ldloca.s    0xeb
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xec
         dup
         conv.i2
         stind.i2
         ldloca.s    0xec
         ldind.i2
         ldloca.s    0xec
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xed
         dup
         conv.i2
         stind.i2
         ldloca.s    0xed
         ldind.i2
         ldloca.s    0xed
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xee
         dup
         conv.i2
         stind.i2
         ldloca.s    0xee
         ldind.i2
         ldloca.s    0xee
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xef
         dup
         conv.i2
         stind.i2
         ldloca.s    0xef
         ldind.i2
         ldloca.s    0xef
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf0
         dup
         conv.i2
         stind.i2
         ldloca.s    0xf0
         ldind.i2
         ldloca.s    0xf0
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf1
         dup
         conv.i2
         stind.i2
         ldloca.s    0xf1
         ldind.i2
         ldloca.s    0xf1
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf2
         dup
         conv.i2
         stind.i2
         ldloca.s    0xf2
         ldind.i2
         ldloca.s    0xf2
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf3
         dup
         conv.i2
         stind.i2
         ldloca.s    0xf3
         ldind.i2
         ldloca.s    0xf3
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf4
         dup
         conv.i2
         stind.i2
         ldloca.s    0xf4
         ldind.i2
         ldloca.s    0xf4
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf5
         dup
         conv.i2
         stind.i2
         ldloca.s    0xf5
         ldind.i2
         ldloca.s    0xf5
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf6
         dup
         conv.i2
         stind.i2
         ldloca.s    0xf6
         ldind.i2
         ldloca.s    0xf6
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf7
         dup
         conv.i2
         stind.i2
         ldloca.s    0xf7
         ldind.i2
         ldloca.s    0xf7
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf8
         dup
         conv.i2
         stind.i2
         ldloca.s    0xf8
         ldind.i2
         ldloca.s    0xf8
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xf9
         dup
         conv.i2
         stind.i2
         ldloca.s    0xf9
         ldind.i2
         ldloca.s    0xf9
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xfa
         dup
         conv.i2
         stind.i2
         ldloca.s    0xfa
         ldind.i2
         ldloca.s    0xfa
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xfb
         dup
         conv.i2
         stind.i2
         ldloca.s    0xfb
         ldind.i2
         ldloca.s    0xfb
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xfc
         dup
         conv.i2
         stind.i2
         ldloca.s    0xfc
         ldind.i2
         ldloca.s    0xfc
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xfd
         dup
         conv.i2
         stind.i2
         ldloca.s    0xfd
         ldind.i2
         ldloca.s    0xfd
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xfe
         dup
         conv.i2
         stind.i2
         ldloca.s    0xfe
         ldind.i2
         ldloca.s    0xfe
         conv.i2
         ceq
         brfalse    FAIL
         
         ldloca.s    0xff
         dup
         conv.i2
         stind.i2
         ldloca.s    0xff
         ldind.i2
         ldloca.s    0xff
         conv.i2
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
        call            int32 ldloca_s_i2::test_int16()
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
