// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly ldloca_s_r4.exe {}
.class ldloca_s_r4 {

.method public static int32 test_float32()
{
.locals (float32 l000, float32 l001, float32 l002, float32 l003, float32 l004, float32 l005, float32 l006, float32 l007,
         float32 l008, float32 l009, float32 l010, float32 l011, float32 l012, float32 l013, float32 l014, float32 l015,
         float32 l016, float32 l017, float32 l018, float32 l019, float32 l020, float32 l021, float32 l022, float32 l023,
         float32 l024, float32 l025, float32 l026, float32 l027, float32 l028, float32 l029, float32 l030, float32 l031,
         float32 l032, float32 l033, float32 l034, float32 l035, float32 l036, float32 l037, float32 l038, float32 l039,
         float32 l040, float32 l041, float32 l042, float32 l043, float32 l044, float32 l045, float32 l046, float32 l047,
         float32 l048, float32 l049, float32 l050, float32 l051, float32 l052, float32 l053, float32 l054, float32 l055,
         float32 l056, float32 l057, float32 l058, float32 l059, float32 l060, float32 l061, float32 l062, float32 l063,
         
         float32 l064, float32 l065, float32 l066, float32 l067, float32 l068, float32 l069, float32 l070, float32 l071,
         float32 l072, float32 l073, float32 l074, float32 l075, float32 l076, float32 l077, float32 l078, float32 l079,
         float32 l080, float32 l081, float32 l082, float32 l083, float32 l084, float32 l085, float32 l086, float32 l087,
         float32 l088, float32 l089, float32 l090, float32 l091, float32 l092, float32 l093, float32 l094, float32 l095,
         float32 l096, float32 l097, float32 l098, float32 l099, float32 l100, float32 l101, float32 l102, float32 l103,
         float32 l104, float32 l105, float32 l106, float32 l107, float32 l108, float32 l109, float32 l110, float32 l111,
         float32 l112, float32 l113, float32 l114, float32 l115, float32 l116, float32 l117, float32 l118, float32 l119,
         float32 l120, float32 l121, float32 l122, float32 l123, float32 l124, float32 l125, float32 l126, float32 l127,
         
         float32 l128, float32 l129, float32 l130, float32 l131, float32 l132, float32 l133, float32 l134, float32 l135,
         float32 l136, float32 l137, float32 l138, float32 l139, float32 l140, float32 l141, float32 l142, float32 l143,
         float32 l144, float32 l145, float32 l146, float32 l147, float32 l148, float32 l149, float32 l150, float32 l151,
         float32 l152, float32 l153, float32 l154, float32 l155, float32 l156, float32 l157, float32 l158, float32 l159,
         float32 l160, float32 l161, float32 l162, float32 l163, float32 l164, float32 l165, float32 l166, float32 l167,
         float32 l168, float32 l169, float32 l170, float32 l171, float32 l172, float32 l173, float32 l174, float32 l175,
         float32 l176, float32 l177, float32 l178, float32 l179, float32 l180, float32 l181, float32 l182, float32 l183,
         float32 l184, float32 l185, float32 l186, float32 l187, float32 l188, float32 l189, float32 l190, float32 l191,
         
         float32 l192, float32 l193, float32 l194, float32 l195, float32 l196, float32 l197, float32 l198, float32 l199,
         float32 l200, float32 l201, float32 l202, float32 l203, float32 l204, float32 l205, float32 l206, float32 l207,
         float32 l208, float32 l209, float32 l210, float32 l211, float32 l212, float32 l213, float32 l214, float32 l215,
         float32 l216, float32 l217, float32 l218, float32 l219, float32 l220, float32 l221, float32 l222, float32 l223,
         float32 l224, float32 l225, float32 l226, float32 l227, float32 l228, float32 l229, float32 l230, float32 l231,
         float32 l232, float32 l233, float32 l234, float32 l235, float32 l236, float32 l237, float32 l238, float32 l239,
         float32 l240, float32 l241, float32 l242, float32 l243, float32 l244, float32 l245, float32 l246, float32 l247,
         float32 l248, float32 l249, float32 l250, float32 l251, float32 l252, float32 l253, float32 l254, float32 l255)
.zeroinit
.maxstack       2

         ldloca.s       0x00
         dup
         conv.r4
         stind.r4
         ldloca.s       0x00
         ldind.r4
         ldloca.s       0x00
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x01
         dup
         conv.r4
         stind.r4
         ldloca.s       0x01
         ldind.r4
         ldloca.s       0x01
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x02
         dup
         conv.r4
         stind.r4
         ldloca.s       0x02
         ldind.r4
         ldloca.s       0x02
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x03
         dup
         conv.r4
         stind.r4
         ldloca.s       0x03
         ldind.r4
         ldloca.s       0x03
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x04
         dup
         conv.r4
         stind.r4
         ldloca.s       0x04
         ldind.r4
         ldloca.s       0x04
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x05
         dup
         conv.r4
         stind.r4
         ldloca.s       0x05
         ldind.r4
         ldloca.s       0x05
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x06
         dup
         conv.r4
         stind.r4
         ldloca.s       0x06
         ldind.r4
         ldloca.s       0x06
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x07
         dup
         conv.r4
         stind.r4
         ldloca.s       0x07
         ldind.r4
         ldloca.s       0x07
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x08
         dup
         conv.r4
         stind.r4
         ldloca.s       0x08
         ldind.r4
         ldloca.s       0x08
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x09
         dup
         conv.r4
         stind.r4
         ldloca.s       0x09
         ldind.r4
         ldloca.s       0x09
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0a
         dup
         conv.r4
         stind.r4
         ldloca.s       0x0a
         ldind.r4
         ldloca.s       0x0a
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0b
         dup
         conv.r4
         stind.r4
         ldloca.s       0x0b
         ldind.r4
         ldloca.s       0x0b
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0c
         dup
         conv.r4
         stind.r4
         ldloca.s       0x0c
         ldind.r4
         ldloca.s       0x0c
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0d
         dup
         conv.r4
         stind.r4
         ldloca.s       0x0d
         ldind.r4
         ldloca.s       0x0d
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0e
         dup
         conv.r4
         stind.r4
         ldloca.s       0x0e
         ldind.r4
         ldloca.s       0x0e
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0f
         dup
         conv.r4
         stind.r4
         ldloca.s       0x0f
         ldind.r4
         ldloca.s       0x0f
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x10
         dup
         conv.r4
         stind.r4
         ldloca.s       0x10
         ldind.r4
         ldloca.s       0x10
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x11
         dup
         conv.r4
         stind.r4
         ldloca.s       0x11
         ldind.r4
         ldloca.s       0x11
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x12
         dup
         conv.r4
         stind.r4
         ldloca.s       0x12
         ldind.r4
         ldloca.s       0x12
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x13
         dup
         conv.r4
         stind.r4
         ldloca.s       0x13
         ldind.r4
         ldloca.s       0x13
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x14
         dup
         conv.r4
         stind.r4
         ldloca.s       0x14
         ldind.r4
         ldloca.s       0x14
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x15
         dup
         conv.r4
         stind.r4
         ldloca.s       0x15
         ldind.r4
         ldloca.s       0x15
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8
         dup
         conv.r4
         stind.r4
         ldloca.s       0x8
         ldind.r4
         ldloca.s       0x8
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x17
         dup
         conv.r4
         stind.r4
         ldloca.s       0x17
         ldind.r4
         ldloca.s       0x17
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x18
         dup
         conv.r4
         stind.r4
         ldloca.s       0x18
         ldind.r4
         ldloca.s       0x18
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x19
         dup
         conv.r4
         stind.r4
         ldloca.s       0x19
         ldind.r4
         ldloca.s       0x19
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1a
         dup
         conv.r4
         stind.r4
         ldloca.s       0x1a
         ldind.r4
         ldloca.s       0x1a
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1b
         dup
         conv.r4
         stind.r4
         ldloca.s       0x1b
         ldind.r4
         ldloca.s       0x1b
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1c
         dup
         conv.r4
         stind.r4
         ldloca.s       0x1c
         ldind.r4
         ldloca.s       0x1c
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1d
         dup
         conv.r4
         stind.r4
         ldloca.s       0x1d
         ldind.r4
         ldloca.s       0x1d
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1e
         dup
         conv.r4
         stind.r4
         ldloca.s       0x1e
         ldind.r4
         ldloca.s       0x1e
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1f
         dup
         conv.r4
         stind.r4
         ldloca.s       0x1f
         ldind.r4
         ldloca.s       0x1f
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x20
         dup
         conv.r4
         stind.r4
         ldloca.s       0x20
         ldind.r4
         ldloca.s       0x20
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x21
         dup
         conv.r4
         stind.r4
         ldloca.s       0x21
         ldind.r4
         ldloca.s       0x21
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x22
         dup
         conv.r4
         stind.r4
         ldloca.s       0x22
         ldind.r4
         ldloca.s       0x22
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x23
         dup
         conv.r4
         stind.r4
         ldloca.s       0x23
         ldind.r4
         ldloca.s       0x23
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x24
         dup
         conv.r4
         stind.r4
         ldloca.s       0x24
         ldind.r4
         ldloca.s       0x24
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x25
         dup
         conv.r4
         stind.r4
         ldloca.s       0x25
         ldind.r4
         ldloca.s       0x25
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x26
         dup
         conv.r4
         stind.r4
         ldloca.s       0x26
         ldind.r4
         ldloca.s       0x26
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x27
         dup
         conv.r4
         stind.r4
         ldloca.s       0x27
         ldind.r4
         ldloca.s       0x27
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x28
         dup
         conv.r4
         stind.r4
         ldloca.s       0x28
         ldind.r4
         ldloca.s       0x28
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x29
         dup
         conv.r4
         stind.r4
         ldloca.s       0x29
         ldind.r4
         ldloca.s       0x29
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2a
         dup
         conv.r4
         stind.r4
         ldloca.s       0x2a
         ldind.r4
         ldloca.s       0x2a
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2b
         dup
         conv.r4
         stind.r4
         ldloca.s       0x2b
         ldind.r4
         ldloca.s       0x2b
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2c
         dup
         conv.r4
         stind.r4
         ldloca.s       0x2c
         ldind.r4
         ldloca.s       0x2c
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2d
         dup
         conv.r4
         stind.r4
         ldloca.s       0x2d
         ldind.r4
         ldloca.s       0x2d
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2e
         dup
         conv.r4
         stind.r4
         ldloca.s       0x2e
         ldind.r4
         ldloca.s       0x2e
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2f
         dup
         conv.r4
         stind.r4
         ldloca.s       0x2f
         ldind.r4
         ldloca.s       0x2f
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x30
         dup
         conv.r4
         stind.r4
         ldloca.s       0x30
         ldind.r4
         ldloca.s       0x30
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x31
         dup
         conv.r4
         stind.r4
         ldloca.s       0x31
         ldind.r4
         ldloca.s       0x31
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8
         dup
         conv.r4
         stind.r4
         ldloca.s       0x8
         ldind.r4
         ldloca.s       0x8
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x33
         dup
         conv.r4
         stind.r4
         ldloca.s       0x33
         ldind.r4
         ldloca.s       0x33
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x34
         dup
         conv.r4
         stind.r4
         ldloca.s       0x34
         ldind.r4
         ldloca.s       0x34
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x35
         dup
         conv.r4
         stind.r4
         ldloca.s       0x35
         ldind.r4
         ldloca.s       0x35
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x36
         dup
         conv.r4
         stind.r4
         ldloca.s       0x36
         ldind.r4
         ldloca.s       0x36
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x37
         dup
         conv.r4
         stind.r4
         ldloca.s       0x37
         ldind.r4
         ldloca.s       0x37
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x38
         dup
         conv.r4
         stind.r4
         ldloca.s       0x38
         ldind.r4
         ldloca.s       0x38
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x39
         dup
         conv.r4
         stind.r4
         ldloca.s       0x39
         ldind.r4
         ldloca.s       0x39
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3a
         dup
         conv.r4
         stind.r4
         ldloca.s       0x3a
         ldind.r4
         ldloca.s       0x3a
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3b
         dup
         conv.r4
         stind.r4
         ldloca.s       0x3b
         ldind.r4
         ldloca.s       0x3b
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3c
         dup
         conv.r4
         stind.r4
         ldloca.s       0x3c
         ldind.r4
         ldloca.s       0x3c
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3d
         dup
         conv.r4
         stind.r4
         ldloca.s       0x3d
         ldind.r4
         ldloca.s       0x3d
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3e
         dup
         conv.r4
         stind.r4
         ldloca.s       0x3e
         ldind.r4
         ldloca.s       0x3e
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3f
         dup
         conv.r4
         stind.r4
         ldloca.s       0x3f
         ldind.r4
         ldloca.s       0x3f
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x40
         dup
         conv.r4
         stind.r4
         ldloca.s       0x40
         ldind.r4
         ldloca.s       0x40
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x41
         dup
         conv.r4
         stind.r4
         ldloca.s       0x41
         ldind.r4
         ldloca.s       0x41
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x42
         dup
         conv.r4
         stind.r4
         ldloca.s       0x42
         ldind.r4
         ldloca.s       0x42
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x43
         dup
         conv.r4
         stind.r4
         ldloca.s       0x43
         ldind.r4
         ldloca.s       0x43
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x44
         dup
         conv.r4
         stind.r4
         ldloca.s       0x44
         ldind.r4
         ldloca.s       0x44
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x45
         dup
         conv.r4
         stind.r4
         ldloca.s       0x45
         ldind.r4
         ldloca.s       0x45
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x46
         dup
         conv.r4
         stind.r4
         ldloca.s       0x46
         ldind.r4
         ldloca.s       0x46
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x47
         dup
         conv.r4
         stind.r4
         ldloca.s       0x47
         ldind.r4
         ldloca.s       0x47
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x48
         dup
         conv.r4
         stind.r4
         ldloca.s       0x48
         ldind.r4
         ldloca.s       0x48
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x49
         dup
         conv.r4
         stind.r4
         ldloca.s       0x49
         ldind.r4
         ldloca.s       0x49
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4a
         dup
         conv.r4
         stind.r4
         ldloca.s       0x4a
         ldind.r4
         ldloca.s       0x4a
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4b
         dup
         conv.r4
         stind.r4
         ldloca.s       0x4b
         ldind.r4
         ldloca.s       0x4b
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4c
         dup
         conv.r4
         stind.r4
         ldloca.s       0x4c
         ldind.r4
         ldloca.s       0x4c
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4d
         dup
         conv.r4
         stind.r4
         ldloca.s       0x4d
         ldind.r4
         ldloca.s       0x4d
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4e
         dup
         conv.r4
         stind.r4
         ldloca.s       0x4e
         ldind.r4
         ldloca.s       0x4e
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4f
         dup
         conv.r4
         stind.r4
         ldloca.s       0x4f
         ldind.r4
         ldloca.s       0x4f
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x50
         dup
         conv.r4
         stind.r4
         ldloca.s       0x50
         ldind.r4
         ldloca.s       0x50
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x51
         dup
         conv.r4
         stind.r4
         ldloca.s       0x51
         ldind.r4
         ldloca.s       0x51
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x52
         dup
         conv.r4
         stind.r4
         ldloca.s       0x52
         ldind.r4
         ldloca.s       0x52
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x53
         dup
         conv.r4
         stind.r4
         ldloca.s       0x53
         ldind.r4
         ldloca.s       0x53
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x54
         dup
         conv.r4
         stind.r4
         ldloca.s       0x54
         ldind.r4
         ldloca.s       0x54
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x55
         dup
         conv.r4
         stind.r4
         ldloca.s       0x55
         ldind.r4
         ldloca.s       0x55
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x56
         dup
         conv.r4
         stind.r4
         ldloca.s       0x56
         ldind.r4
         ldloca.s       0x56
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x57
         dup
         conv.r4
         stind.r4
         ldloca.s       0x57
         ldind.r4
         ldloca.s       0x57
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x58
         dup
         conv.r4
         stind.r4
         ldloca.s       0x58
         ldind.r4
         ldloca.s       0x58
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x59
         dup
         conv.r4
         stind.r4
         ldloca.s       0x59
         ldind.r4
         ldloca.s       0x59
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5a
         dup
         conv.r4
         stind.r4
         ldloca.s       0x5a
         ldind.r4
         ldloca.s       0x5a
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5b
         dup
         conv.r4
         stind.r4
         ldloca.s       0x5b
         ldind.r4
         ldloca.s       0x5b
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5c
         dup
         conv.r4
         stind.r4
         ldloca.s       0x5c
         ldind.r4
         ldloca.s       0x5c
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5d
         dup
         conv.r4
         stind.r4
         ldloca.s       0x5d
         ldind.r4
         ldloca.s       0x5d
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5e
         dup
         conv.r4
         stind.r4
         ldloca.s       0x5e
         ldind.r4
         ldloca.s       0x5e
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5f
         dup
         conv.r4
         stind.r4
         ldloca.s       0x5f
         ldind.r4
         ldloca.s       0x5f
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x60
         dup
         conv.r4
         stind.r4
         ldloca.s       0x60
         ldind.r4
         ldloca.s       0x60
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x61
         dup
         conv.r4
         stind.r4
         ldloca.s       0x61
         ldind.r4
         ldloca.s       0x61
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x62
         dup
         conv.r4
         stind.r4
         ldloca.s       0x62
         ldind.r4
         ldloca.s       0x62
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x63
         dup
         conv.r4
         stind.r4
         ldloca.s       0x63
         ldind.r4
         ldloca.s       0x63
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x64
         dup
         conv.r4
         stind.r4
         ldloca.s       0x64
         ldind.r4
         ldloca.s       0x64
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x65
         dup
         conv.r4
         stind.r4
         ldloca.s       0x65
         ldind.r4
         ldloca.s       0x65
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x66
         dup
         conv.r4
         stind.r4
         ldloca.s       0x66
         ldind.r4
         ldloca.s       0x66
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x67
         dup
         conv.r4
         stind.r4
         ldloca.s       0x67
         ldind.r4
         ldloca.s       0x67
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x68
         dup
         conv.r4
         stind.r4
         ldloca.s       0x68
         ldind.r4
         ldloca.s       0x68
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x69
         dup
         conv.r4
         stind.r4
         ldloca.s       0x69
         ldind.r4
         ldloca.s       0x69
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6a
         dup
         conv.r4
         stind.r4
         ldloca.s       0x6a
         ldind.r4
         ldloca.s       0x6a
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6b
         dup
         conv.r4
         stind.r4
         ldloca.s       0x6b
         ldind.r4
         ldloca.s       0x6b
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6c
         dup
         conv.r4
         stind.r4
         ldloca.s       0x6c
         ldind.r4
         ldloca.s       0x6c
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6d
         dup
         conv.r4
         stind.r4
         ldloca.s       0x6d
         ldind.r4
         ldloca.s       0x6d
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6e
         dup
         conv.r4
         stind.r4
         ldloca.s       0x6e
         ldind.r4
         ldloca.s       0x6e
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6f
         dup
         conv.r4
         stind.r4
         ldloca.s       0x6f
         ldind.r4
         ldloca.s       0x6f
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x70
         dup
         conv.r4
         stind.r4
         ldloca.s       0x70
         ldind.r4
         ldloca.s       0x70
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x71
         dup
         conv.r4
         stind.r4
         ldloca.s       0x71
         ldind.r4
         ldloca.s       0x71
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x72
         dup
         conv.r4
         stind.r4
         ldloca.s       0x72
         ldind.r4
         ldloca.s       0x72
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x73
         dup
         conv.r4
         stind.r4
         ldloca.s       0x73
         ldind.r4
         ldloca.s       0x73
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x74
         dup
         conv.r4
         stind.r4
         ldloca.s       0x74
         ldind.r4
         ldloca.s       0x74
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x75
         dup
         conv.r4
         stind.r4
         ldloca.s       0x75
         ldind.r4
         ldloca.s       0x75
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x76
         dup
         conv.r4
         stind.r4
         ldloca.s       0x76
         ldind.r4
         ldloca.s       0x76
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x77
         dup
         conv.r4
         stind.r4
         ldloca.s       0x77
         ldind.r4
         ldloca.s       0x77
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x78
         dup
         conv.r4
         stind.r4
         ldloca.s       0x78
         ldind.r4
         ldloca.s       0x78
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x79
         dup
         conv.r4
         stind.r4
         ldloca.s       0x79
         ldind.r4
         ldloca.s       0x79
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7a
         dup
         conv.r4
         stind.r4
         ldloca.s       0x7a
         ldind.r4
         ldloca.s       0x7a
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7b
         dup
         conv.r4
         stind.r4
         ldloca.s       0x7b
         ldind.r4
         ldloca.s       0x7b
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7c
         dup
         conv.r4
         stind.r4
         ldloca.s       0x7c
         ldind.r4
         ldloca.s       0x7c
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7d
         dup
         conv.r4
         stind.r4
         ldloca.s       0x7d
         ldind.r4
         ldloca.s       0x7d
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7e
         dup
         conv.r4
         stind.r4
         ldloca.s       0x7e
         ldind.r4
         ldloca.s       0x7e
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7f
         dup
         conv.r4
         stind.r4
         ldloca.s       0x7f
         ldind.r4
         ldloca.s       0x7f
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x80
         dup
         conv.r4
         stind.r4
         ldloca.s       0x80
         ldind.r4
         ldloca.s       0x80
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x81
         dup
         conv.r4
         stind.r4
         ldloca.s       0x81
         ldind.r4
         ldloca.s       0x81
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x82
         dup
         conv.r4
         stind.r4
         ldloca.s       0x82
         ldind.r4
         ldloca.s       0x82
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x83
         dup
         conv.r4
         stind.r4
         ldloca.s       0x83
         ldind.r4
         ldloca.s       0x83
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x84
         dup
         conv.r4
         stind.r4
         ldloca.s       0x84
         ldind.r4
         ldloca.s       0x84
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x85
         dup
         conv.r4
         stind.r4
         ldloca.s       0x85
         ldind.r4
         ldloca.s       0x85
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x86
         dup
         conv.r4
         stind.r4
         ldloca.s       0x86
         ldind.r4
         ldloca.s       0x86
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x87
         dup
         conv.r4
         stind.r4
         ldloca.s       0x87
         ldind.r4
         ldloca.s       0x87
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x88
         dup
         conv.r4
         stind.r4
         ldloca.s       0x88
         ldind.r4
         ldloca.s       0x88
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x89
         dup
         conv.r4
         stind.r4
         ldloca.s       0x89
         ldind.r4
         ldloca.s       0x89
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8a
         dup
         conv.r4
         stind.r4
         ldloca.s       0x8a
         ldind.r4
         ldloca.s       0x8a
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8b
         dup
         conv.r4
         stind.r4
         ldloca.s       0x8b
         ldind.r4
         ldloca.s       0x8b
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8c
         dup
         conv.r4
         stind.r4
         ldloca.s       0x8c
         ldind.r4
         ldloca.s       0x8c
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8d
         dup
         conv.r4
         stind.r4
         ldloca.s       0x8d
         ldind.r4
         ldloca.s       0x8d
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8e
         dup
         conv.r4
         stind.r4
         ldloca.s       0x8e
         ldind.r4
         ldloca.s       0x8e
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8f
         dup
         conv.r4
         stind.r4
         ldloca.s       0x8f
         ldind.r4
         ldloca.s       0x8f
         conv.r4
         ceq
         brfalse        FAIL

         ldloca.s       0x90
         dup
         conv.r4
         stind.r4
         ldloca.s       0x90
         ldind.r4
         ldloca.s       0x90
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x91
         dup
         conv.r4
         stind.r4
         ldloca.s       0x91
         ldind.r4
         ldloca.s       0x91
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x92
         dup
         conv.r4
         stind.r4
         ldloca.s       0x92
         ldind.r4
         ldloca.s       0x92
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x93
         dup
         conv.r4
         stind.r4
         ldloca.s       0x93
         ldind.r4
         ldloca.s       0x93
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x94
         dup
         conv.r4
         stind.r4
         ldloca.s       0x94
         ldind.r4
         ldloca.s       0x94
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x95
         dup
         conv.r4
         stind.r4
         ldloca.s       0x95
         ldind.r4
         ldloca.s       0x95
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x96
         dup
         conv.r4
         stind.r4
         ldloca.s       0x96
         ldind.r4
         ldloca.s       0x96
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x97
         dup
         conv.r4
         stind.r4
         ldloca.s       0x97
         ldind.r4
         ldloca.s       0x97
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x98
         dup
         conv.r4
         stind.r4
         ldloca.s       0x98
         ldind.r4
         ldloca.s       0x98
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x99
         dup
         conv.r4
         stind.r4
         ldloca.s       0x99
         ldind.r4
         ldloca.s       0x99
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9a
         dup
         conv.r4
         stind.r4
         ldloca.s       0x9a
         ldind.r4
         ldloca.s       0x9a
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9b
         dup
         conv.r4
         stind.r4
         ldloca.s       0x9b
         ldind.r4
         ldloca.s       0x9b
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9c
         dup
         conv.r4
         stind.r4
         ldloca.s       0x9c
         ldind.r4
         ldloca.s       0x9c
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9d
         dup
         conv.r4
         stind.r4
         ldloca.s       0x9d
         ldind.r4
         ldloca.s       0x9d
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9e
         dup
         conv.r4
         stind.r4
         ldloca.s       0x9e
         ldind.r4
         ldloca.s       0x9e
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9f
         dup
         conv.r4
         stind.r4
         ldloca.s       0x9f
         ldind.r4
         ldloca.s       0x9f
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa0
         dup
         conv.r4
         stind.r4
         ldloca.s       0xa0
         ldind.r4
         ldloca.s       0xa0
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa1
         dup
         conv.r4
         stind.r4
         ldloca.s       0xa1
         ldind.r4
         ldloca.s       0xa1
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa2
         dup
         conv.r4
         stind.r4
         ldloca.s       0xa2
         ldind.r4
         ldloca.s       0xa2
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa3
         dup
         conv.r4
         stind.r4
         ldloca.s       0xa3
         ldind.r4
         ldloca.s       0xa3
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa4
         dup
         conv.r4
         stind.r4
         ldloca.s       0xa4
         ldind.r4
         ldloca.s       0xa4
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa5
         dup
         conv.r4
         stind.r4
         ldloca.s       0xa5
         ldind.r4
         ldloca.s       0xa5
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa6
         dup
         conv.r4
         stind.r4
         ldloca.s       0xa6
         ldind.r4
         ldloca.s       0xa6
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa7
         dup
         conv.r4
         stind.r4
         ldloca.s       0xa7
         ldind.r4
         ldloca.s       0xa7
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa8
         dup
         conv.r4
         stind.r4
         ldloca.s       0xa8
         ldind.r4
         ldloca.s       0xa8
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa9
         dup
         conv.r4
         stind.r4
         ldloca.s       0xa9
         ldind.r4
         ldloca.s       0xa9
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xaa
         dup
         conv.r4
         stind.r4
         ldloca.s       0xaa
         ldind.r4
         ldloca.s       0xaa
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xab
         dup
         conv.r4
         stind.r4
         ldloca.s       0xab
         ldind.r4
         ldloca.s       0xab
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xac
         dup
         conv.r4
         stind.r4
         ldloca.s       0xac
         ldind.r4
         ldloca.s       0xac
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xad
         dup
         conv.r4
         stind.r4
         ldloca.s       0xad
         ldind.r4
         ldloca.s       0xad
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xae
         dup
         conv.r4
         stind.r4
         ldloca.s       0xae
         ldind.r4
         ldloca.s       0xae
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xaf
         dup
         conv.r4
         stind.r4
         ldloca.s       0xaf
         ldind.r4
         ldloca.s       0xaf
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb0
         dup
         conv.r4
         stind.r4
         ldloca.s       0xb0
         ldind.r4
         ldloca.s       0xb0
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb1
         dup
         conv.r4
         stind.r4
         ldloca.s       0xb1
         ldind.r4
         ldloca.s       0xb1
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb2
         dup
         conv.r4
         stind.r4
         ldloca.s       0xb2
         ldind.r4
         ldloca.s       0xb2
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb3
         dup
         conv.r4
         stind.r4
         ldloca.s       0xb3
         ldind.r4
         ldloca.s       0xb3
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb4
         dup
         conv.r4
         stind.r4
         ldloca.s       0xb4
         ldind.r4
         ldloca.s       0xb4
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb5
         dup
         conv.r4
         stind.r4
         ldloca.s       0xb5
         ldind.r4
         ldloca.s       0xb5
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb6
         dup
         conv.r4
         stind.r4
         ldloca.s       0xb6
         ldind.r4
         ldloca.s       0xb6
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb7
         dup
         conv.r4
         stind.r4
         ldloca.s       0xb7
         ldind.r4
         ldloca.s       0xb7
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb8
         dup
         conv.r4
         stind.r4
         ldloca.s       0xb8
         ldind.r4
         ldloca.s       0xb8
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb9
         dup
         conv.r4
         stind.r4
         ldloca.s       0xb9
         ldind.r4
         ldloca.s       0xb9
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xba
         dup
         conv.r4
         stind.r4
         ldloca.s       0xba
         ldind.r4
         ldloca.s       0xba
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xbb
         dup
         conv.r4
         stind.r4
         ldloca.s       0xbb
         ldind.r4
         ldloca.s       0xbb
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xbc
         dup
         conv.r4
         stind.r4
         ldloca.s       0xbc
         ldind.r4
         ldloca.s       0xbc
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xbd
         dup
         conv.r4
         stind.r4
         ldloca.s       0xbd
         ldind.r4
         ldloca.s       0xbd
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xbe
         dup
         conv.r4
         stind.r4
         ldloca.s       0xbe
         ldind.r4
         ldloca.s       0xbe
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xbf
         dup
         conv.r4
         stind.r4
         ldloca.s       0xbf
         ldind.r4
         ldloca.s       0xbf
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc0
         dup
         conv.r4
         stind.r4
         ldloca.s       0xc0
         ldind.r4
         ldloca.s       0xc0
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc1
         dup
         conv.r4
         stind.r4
         ldloca.s       0xc1
         ldind.r4
         ldloca.s       0xc1
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc2
         dup
         conv.r4
         stind.r4
         ldloca.s       0xc2
         ldind.r4
         ldloca.s       0xc2
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc3
         dup
         conv.r4
         stind.r4
         ldloca.s       0xc3
         ldind.r4
         ldloca.s       0xc3
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc4
         dup
         conv.r4
         stind.r4
         ldloca.s       0xc4
         ldind.r4
         ldloca.s       0xc4
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc5
         dup
         conv.r4
         stind.r4
         ldloca.s       0xc5
         ldind.r4
         ldloca.s       0xc5
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc6
         dup
         conv.r4
         stind.r4
         ldloca.s       0xc6
         ldind.r4
         ldloca.s       0xc6
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc7
         dup
         conv.r4
         stind.r4
         ldloca.s       0xc7
         ldind.r4
         ldloca.s       0xc7
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc8
         dup
         conv.r4
         stind.r4
         ldloca.s       0xc8
         ldind.r4
         ldloca.s       0xc8
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc9
         dup
         conv.r4
         stind.r4
         ldloca.s       0xc9
         ldind.r4
         ldloca.s       0xc9
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xca
         dup
         conv.r4
         stind.r4
         ldloca.s       0xca
         ldind.r4
         ldloca.s       0xca
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xcb
         dup
         conv.r4
         stind.r4
         ldloca.s       0xcb
         ldind.r4
         ldloca.s       0xcb
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xcc
         dup
         conv.r4
         stind.r4
         ldloca.s       0xcc
         ldind.r4
         ldloca.s       0xcc
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xcd
         dup
         conv.r4
         stind.r4
         ldloca.s       0xcd
         ldind.r4
         ldloca.s       0xcd
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xce
         dup
         conv.r4
         stind.r4
         ldloca.s       0xce
         ldind.r4
         ldloca.s       0xce
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xcf
         dup
         conv.r4
         stind.r4
         ldloca.s       0xcf
         ldind.r4
         ldloca.s       0xcf
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd0
         dup
         conv.r4
         stind.r4
         ldloca.s       0xd0
         ldind.r4
         ldloca.s       0xd0
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd1
         dup
         conv.r4
         stind.r4
         ldloca.s       0xd1
         ldind.r4
         ldloca.s       0xd1
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd2
         dup
         conv.r4
         stind.r4
         ldloca.s       0xd2
         ldind.r4
         ldloca.s       0xd2
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd3
         dup
         conv.r4
         stind.r4
         ldloca.s       0xd3
         ldind.r4
         ldloca.s       0xd3
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd4
         dup
         conv.r4
         stind.r4
         ldloca.s       0xd4
         ldind.r4
         ldloca.s       0xd4
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd5
         dup
         conv.r4
         stind.r4
         ldloca.s       0xd5
         ldind.r4
         ldloca.s       0xd5
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd6
         dup
         conv.r4
         stind.r4
         ldloca.s       0xd6
         ldind.r4
         ldloca.s       0xd6
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd7
         dup
         conv.r4
         stind.r4
         ldloca.s       0xd7
         ldind.r4
         ldloca.s       0xd7
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd8
         dup
         conv.r4
         stind.r4
         ldloca.s       0xd8
         ldind.r4
         ldloca.s       0xd8
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd9
         dup
         conv.r4
         stind.r4
         ldloca.s       0xd9
         ldind.r4
         ldloca.s       0xd9
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xda
         dup
         conv.r4
         stind.r4
         ldloca.s       0xda
         ldind.r4
         ldloca.s       0xda
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xdb
         dup
         conv.r4
         stind.r4
         ldloca.s       0xdb
         ldind.r4
         ldloca.s       0xdb
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xdc
         dup
         conv.r4
         stind.r4
         ldloca.s       0xdc
         ldind.r4
         ldloca.s       0xdc
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xdd
         dup
         conv.r4
         stind.r4
         ldloca.s       0xdd
         ldind.r4
         ldloca.s       0xdd
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xde
         dup
         conv.r4
         stind.r4
         ldloca.s       0xde
         ldind.r4
         ldloca.s       0xde
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xdf
         dup
         conv.r4
         stind.r4
         ldloca.s       0xdf
         ldind.r4
         ldloca.s       0xdf
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe0
         dup
         conv.r4
         stind.r4
         ldloca.s       0xe0
         ldind.r4
         ldloca.s       0xe0
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe1
         dup
         conv.r4
         stind.r4
         ldloca.s       0xe1
         ldind.r4
         ldloca.s       0xe1
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe2
         dup
         conv.r4
         stind.r4
         ldloca.s       0xe2
         ldind.r4
         ldloca.s       0xe2
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe3
         dup
         conv.r4
         stind.r4
         ldloca.s       0xe3
         ldind.r4
         ldloca.s       0xe3
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe4
         dup
         conv.r4
         stind.r4
         ldloca.s       0xe4
         ldind.r4
         ldloca.s       0xe4
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe5
         dup
         conv.r4
         stind.r4
         ldloca.s       0xe5
         ldind.r4
         ldloca.s       0xe5
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe6
         dup
         conv.r4
         stind.r4
         ldloca.s       0xe6
         ldind.r4
         ldloca.s       0xe6
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe7
         dup
         conv.r4
         stind.r4
         ldloca.s       0xe7
         ldind.r4
         ldloca.s       0xe7
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe8
         dup
         conv.r4
         stind.r4
         ldloca.s       0xe8
         ldind.r4
         ldloca.s       0xe8
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe9
         dup
         conv.r4
         stind.r4
         ldloca.s       0xe9
         ldind.r4
         ldloca.s       0xe9
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xea
         dup
         conv.r4
         stind.r4
         ldloca.s       0xea
         ldind.r4
         ldloca.s       0xea
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xeb
         dup
         conv.r4
         stind.r4
         ldloca.s       0xeb
         ldind.r4
         ldloca.s       0xeb
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xec
         dup
         conv.r4
         stind.r4
         ldloca.s       0xec
         ldind.r4
         ldloca.s       0xec
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xed
         dup
         conv.r4
         stind.r4
         ldloca.s       0xed
         ldind.r4
         ldloca.s       0xed
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xee
         dup
         conv.r4
         stind.r4
         ldloca.s       0xee
         ldind.r4
         ldloca.s       0xee
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xef
         dup
         conv.r4
         stind.r4
         ldloca.s       0xef
         ldind.r4
         ldloca.s       0xef
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf0
         dup
         conv.r4
         stind.r4
         ldloca.s       0xf0
         ldind.r4
         ldloca.s       0xf0
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf1
         dup
         conv.r4
         stind.r4
         ldloca.s       0xf1
         ldind.r4
         ldloca.s       0xf1
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf2
         dup
         conv.r4
         stind.r4
         ldloca.s       0xf2
         ldind.r4
         ldloca.s       0xf2
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf3
         dup
         conv.r4
         stind.r4
         ldloca.s       0xf3
         ldind.r4
         ldloca.s       0xf3
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf4
         dup
         conv.r4
         stind.r4
         ldloca.s       0xf4
         ldind.r4
         ldloca.s       0xf4
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf5
         dup
         conv.r4
         stind.r4
         ldloca.s       0xf5
         ldind.r4
         ldloca.s       0xf5
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf6
         dup
         conv.r4
         stind.r4
         ldloca.s       0xf6
         ldind.r4
         ldloca.s       0xf6
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf7
         dup
         conv.r4
         stind.r4
         ldloca.s       0xf7
         ldind.r4
         ldloca.s       0xf7
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf8
         dup
         conv.r4
         stind.r4
         ldloca.s       0xf8
         ldind.r4
         ldloca.s       0xf8
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf9
         dup
         conv.r4
         stind.r4
         ldloca.s       0xf9
         ldind.r4
         ldloca.s       0xf9
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xfa
         dup
         conv.r4
         stind.r4
         ldloca.s       0xfa
         ldind.r4
         ldloca.s       0xfa
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xfb
         dup
         conv.r4
         stind.r4
         ldloca.s       0xfb
         ldind.r4
         ldloca.s       0xfb
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xfc
         dup
         conv.r4
         stind.r4
         ldloca.s       0xfc
         ldind.r4
         ldloca.s       0xfc
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xfd
         dup
         conv.r4
         stind.r4
         ldloca.s       0xfd
         ldind.r4
         ldloca.s       0xfd
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xfe
         dup
         conv.r4
         stind.r4
         ldloca.s       0xfe
         ldind.r4
         ldloca.s       0xfe
         conv.r4
         ceq
         brfalse        FAIL
         
         ldloca.s       0xff
         dup
         conv.r4
         stind.r4
         ldloca.s       0xff
         ldind.r4
         ldloca.s       0xff
         conv.r4
         ceq
         brfalse        FAIL
         
         ldc.i4.1
         ret
         
FAIL:
        ldloc       0
	call	int32 [mscorlib]System.Convert::ToInt32(float32)
        ret         
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack       2
        call            int32 ldloca_s_r4::test_float32()
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
