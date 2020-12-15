// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly ldloca_s_r8.exe {}
.class ldloca_s_r8 {

.method public static int32 test_float64()
{
.locals (float64 l000, float64 l001, float64 l002, float64 l003, float64 l004, float64 l005, float64 l006, float64 l007,
         float64 l008, float64 l009, float64 l010, float64 l011, float64 l012, float64 l013, float64 l014, float64 l015,
         float64 l016, float64 l017, float64 l018, float64 l019, float64 l020, float64 l021, float64 l022, float64 l023,
         float64 l024, float64 l025, float64 l026, float64 l027, float64 l028, float64 l029, float64 l030, float64 l031,
         float64 l032, float64 l033, float64 l034, float64 l035, float64 l036, float64 l037, float64 l038, float64 l039,
         float64 l040, float64 l041, float64 l042, float64 l043, float64 l044, float64 l045, float64 l046, float64 l047,
         float64 l048, float64 l049, float64 l050, float64 l051, float64 l052, float64 l053, float64 l054, float64 l055,
         float64 l056, float64 l057, float64 l058, float64 l059, float64 l060, float64 l061, float64 l062, float64 l063,
         
         float64 l064, float64 l065, float64 l066, float64 l067, float64 l068, float64 l069, float64 l070, float64 l071,
         float64 l072, float64 l073, float64 l074, float64 l075, float64 l076, float64 l077, float64 l078, float64 l079,
         float64 l080, float64 l081, float64 l082, float64 l083, float64 l084, float64 l085, float64 l086, float64 l087,
         float64 l088, float64 l089, float64 l090, float64 l091, float64 l092, float64 l093, float64 l094, float64 l095,
         float64 l096, float64 l097, float64 l098, float64 l099, float64 l100, float64 l101, float64 l102, float64 l103,
         float64 l104, float64 l105, float64 l106, float64 l107, float64 l108, float64 l109, float64 l110, float64 l111,
         float64 l112, float64 l113, float64 l114, float64 l115, float64 l116, float64 l117, float64 l118, float64 l119,
         float64 l120, float64 l121, float64 l122, float64 l123, float64 l124, float64 l125, float64 l126, float64 l127,
         
         float64 l128, float64 l129, float64 l130, float64 l131, float64 l132, float64 l133, float64 l134, float64 l135,
         float64 l136, float64 l137, float64 l138, float64 l139, float64 l140, float64 l141, float64 l142, float64 l143,
         float64 l144, float64 l145, float64 l146, float64 l147, float64 l148, float64 l149, float64 l150, float64 l151,
         float64 l152, float64 l153, float64 l154, float64 l155, float64 l156, float64 l157, float64 l158, float64 l159,
         float64 l160, float64 l161, float64 l162, float64 l163, float64 l164, float64 l165, float64 l166, float64 l167,
         float64 l168, float64 l169, float64 l170, float64 l171, float64 l172, float64 l173, float64 l174, float64 l175,
         float64 l176, float64 l177, float64 l178, float64 l179, float64 l180, float64 l181, float64 l182, float64 l183,
         float64 l184, float64 l185, float64 l186, float64 l187, float64 l188, float64 l189, float64 l190, float64 l191,
         
         float64 l192, float64 l193, float64 l194, float64 l195, float64 l196, float64 l197, float64 l198, float64 l199,
         float64 l200, float64 l201, float64 l202, float64 l203, float64 l204, float64 l205, float64 l206, float64 l207,
         float64 l208, float64 l209, float64 l210, float64 l211, float64 l212, float64 l213, float64 l214, float64 l215,
         float64 l216, float64 l217, float64 l218, float64 l219, float64 l220, float64 l221, float64 l222, float64 l223,
         float64 l224, float64 l225, float64 l226, float64 l227, float64 l228, float64 l229, float64 l230, float64 l231,
         float64 l232, float64 l233, float64 l234, float64 l235, float64 l236, float64 l237, float64 l238, float64 l239,
         float64 l240, float64 l241, float64 l242, float64 l243, float64 l244, float64 l245, float64 l246, float64 l247,
         float64 l248, float64 l249, float64 l250, float64 l251, float64 l252, float64 l253, float64 l254, float64 l255)
.zeroinit
.maxstack       2

         ldloca.s       0x00
         dup
         conv.r8
         stind.r8
         ldloca.s       0x00
         ldind.r8
         ldloca.s       0x00
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x01
         dup
         conv.r8
         stind.r8
         ldloca.s       0x01
         ldind.r8
         ldloca.s       0x01
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x02
         dup
         conv.r8
         stind.r8
         ldloca.s       0x02
         ldind.r8
         ldloca.s       0x02
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x03
         dup
         conv.r8
         stind.r8
         ldloca.s       0x03
         ldind.r8
         ldloca.s       0x03
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x04
         dup
         conv.r8
         stind.r8
         ldloca.s       0x04
         ldind.r8
         ldloca.s       0x04
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x05
         dup
         conv.r8
         stind.r8
         ldloca.s       0x05
         ldind.r8
         ldloca.s       0x05
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x06
         dup
         conv.r8
         stind.r8
         ldloca.s       0x06
         ldind.r8
         ldloca.s       0x06
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x07
         dup
         conv.r8
         stind.r8
         ldloca.s       0x07
         ldind.r8
         ldloca.s       0x07
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x08
         dup
         conv.r8
         stind.r8
         ldloca.s       0x08
         ldind.r8
         ldloca.s       0x08
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x09
         dup
         conv.r8
         stind.r8
         ldloca.s       0x09
         ldind.r8
         ldloca.s       0x09
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0a
         dup
         conv.r8
         stind.r8
         ldloca.s       0x0a
         ldind.r8
         ldloca.s       0x0a
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0b
         dup
         conv.r8
         stind.r8
         ldloca.s       0x0b
         ldind.r8
         ldloca.s       0x0b
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0c
         dup
         conv.r8
         stind.r8
         ldloca.s       0x0c
         ldind.r8
         ldloca.s       0x0c
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0d
         dup
         conv.r8
         stind.r8
         ldloca.s       0x0d
         ldind.r8
         ldloca.s       0x0d
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0e
         dup
         conv.r8
         stind.r8
         ldloca.s       0x0e
         ldind.r8
         ldloca.s       0x0e
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x0f
         dup
         conv.r8
         stind.r8
         ldloca.s       0x0f
         ldind.r8
         ldloca.s       0x0f
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x10
         dup
         conv.r8
         stind.r8
         ldloca.s       0x10
         ldind.r8
         ldloca.s       0x10
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x11
         dup
         conv.r8
         stind.r8
         ldloca.s       0x11
         ldind.r8
         ldloca.s       0x11
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x12
         dup
         conv.r8
         stind.r8
         ldloca.s       0x12
         ldind.r8
         ldloca.s       0x12
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x13
         dup
         conv.r8
         stind.r8
         ldloca.s       0x13
         ldind.r8
         ldloca.s       0x13
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x14
         dup
         conv.r8
         stind.r8
         ldloca.s       0x14
         ldind.r8
         ldloca.s       0x14
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x15
         dup
         conv.r8
         stind.r8
         ldloca.s       0x15
         ldind.r8
         ldloca.s       0x15
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8
         dup
         conv.r8
         stind.r8
         ldloca.s       0x8
         ldind.r8
         ldloca.s       0x8
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x17
         dup
         conv.r8
         stind.r8
         ldloca.s       0x17
         ldind.r8
         ldloca.s       0x17
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x18
         dup
         conv.r8
         stind.r8
         ldloca.s       0x18
         ldind.r8
         ldloca.s       0x18
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x19
         dup
         conv.r8
         stind.r8
         ldloca.s       0x19
         ldind.r8
         ldloca.s       0x19
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1a
         dup
         conv.r8
         stind.r8
         ldloca.s       0x1a
         ldind.r8
         ldloca.s       0x1a
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1b
         dup
         conv.r8
         stind.r8
         ldloca.s       0x1b
         ldind.r8
         ldloca.s       0x1b
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1c
         dup
         conv.r8
         stind.r8
         ldloca.s       0x1c
         ldind.r8
         ldloca.s       0x1c
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1d
         dup
         conv.r8
         stind.r8
         ldloca.s       0x1d
         ldind.r8
         ldloca.s       0x1d
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1e
         dup
         conv.r8
         stind.r8
         ldloca.s       0x1e
         ldind.r8
         ldloca.s       0x1e
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x1f
         dup
         conv.r8
         stind.r8
         ldloca.s       0x1f
         ldind.r8
         ldloca.s       0x1f
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x20
         dup
         conv.r8
         stind.r8
         ldloca.s       0x20
         ldind.r8
         ldloca.s       0x20
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x21
         dup
         conv.r8
         stind.r8
         ldloca.s       0x21
         ldind.r8
         ldloca.s       0x21
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x22
         dup
         conv.r8
         stind.r8
         ldloca.s       0x22
         ldind.r8
         ldloca.s       0x22
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x23
         dup
         conv.r8
         stind.r8
         ldloca.s       0x23
         ldind.r8
         ldloca.s       0x23
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x24
         dup
         conv.r8
         stind.r8
         ldloca.s       0x24
         ldind.r8
         ldloca.s       0x24
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x25
         dup
         conv.r8
         stind.r8
         ldloca.s       0x25
         ldind.r8
         ldloca.s       0x25
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x26
         dup
         conv.r8
         stind.r8
         ldloca.s       0x26
         ldind.r8
         ldloca.s       0x26
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x27
         dup
         conv.r8
         stind.r8
         ldloca.s       0x27
         ldind.r8
         ldloca.s       0x27
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x28
         dup
         conv.r8
         stind.r8
         ldloca.s       0x28
         ldind.r8
         ldloca.s       0x28
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x29
         dup
         conv.r8
         stind.r8
         ldloca.s       0x29
         ldind.r8
         ldloca.s       0x29
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2a
         dup
         conv.r8
         stind.r8
         ldloca.s       0x2a
         ldind.r8
         ldloca.s       0x2a
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2b
         dup
         conv.r8
         stind.r8
         ldloca.s       0x2b
         ldind.r8
         ldloca.s       0x2b
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2c
         dup
         conv.r8
         stind.r8
         ldloca.s       0x2c
         ldind.r8
         ldloca.s       0x2c
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2d
         dup
         conv.r8
         stind.r8
         ldloca.s       0x2d
         ldind.r8
         ldloca.s       0x2d
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2e
         dup
         conv.r8
         stind.r8
         ldloca.s       0x2e
         ldind.r8
         ldloca.s       0x2e
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x2f
         dup
         conv.r8
         stind.r8
         ldloca.s       0x2f
         ldind.r8
         ldloca.s       0x2f
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x30
         dup
         conv.r8
         stind.r8
         ldloca.s       0x30
         ldind.r8
         ldloca.s       0x30
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x31
         dup
         conv.r8
         stind.r8
         ldloca.s       0x31
         ldind.r8
         ldloca.s       0x31
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8
         dup
         conv.r8
         stind.r8
         ldloca.s       0x8
         ldind.r8
         ldloca.s       0x8
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x33
         dup
         conv.r8
         stind.r8
         ldloca.s       0x33
         ldind.r8
         ldloca.s       0x33
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x34
         dup
         conv.r8
         stind.r8
         ldloca.s       0x34
         ldind.r8
         ldloca.s       0x34
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x35
         dup
         conv.r8
         stind.r8
         ldloca.s       0x35
         ldind.r8
         ldloca.s       0x35
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x36
         dup
         conv.r8
         stind.r8
         ldloca.s       0x36
         ldind.r8
         ldloca.s       0x36
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x37
         dup
         conv.r8
         stind.r8
         ldloca.s       0x37
         ldind.r8
         ldloca.s       0x37
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x38
         dup
         conv.r8
         stind.r8
         ldloca.s       0x38
         ldind.r8
         ldloca.s       0x38
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x39
         dup
         conv.r8
         stind.r8
         ldloca.s       0x39
         ldind.r8
         ldloca.s       0x39
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3a
         dup
         conv.r8
         stind.r8
         ldloca.s       0x3a
         ldind.r8
         ldloca.s       0x3a
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3b
         dup
         conv.r8
         stind.r8
         ldloca.s       0x3b
         ldind.r8
         ldloca.s       0x3b
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3c
         dup
         conv.r8
         stind.r8
         ldloca.s       0x3c
         ldind.r8
         ldloca.s       0x3c
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3d
         dup
         conv.r8
         stind.r8
         ldloca.s       0x3d
         ldind.r8
         ldloca.s       0x3d
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3e
         dup
         conv.r8
         stind.r8
         ldloca.s       0x3e
         ldind.r8
         ldloca.s       0x3e
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x3f
         dup
         conv.r8
         stind.r8
         ldloca.s       0x3f
         ldind.r8
         ldloca.s       0x3f
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x40
         dup
         conv.r8
         stind.r8
         ldloca.s       0x40
         ldind.r8
         ldloca.s       0x40
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x41
         dup
         conv.r8
         stind.r8
         ldloca.s       0x41
         ldind.r8
         ldloca.s       0x41
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x42
         dup
         conv.r8
         stind.r8
         ldloca.s       0x42
         ldind.r8
         ldloca.s       0x42
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x43
         dup
         conv.r8
         stind.r8
         ldloca.s       0x43
         ldind.r8
         ldloca.s       0x43
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x44
         dup
         conv.r8
         stind.r8
         ldloca.s       0x44
         ldind.r8
         ldloca.s       0x44
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x45
         dup
         conv.r8
         stind.r8
         ldloca.s       0x45
         ldind.r8
         ldloca.s       0x45
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x46
         dup
         conv.r8
         stind.r8
         ldloca.s       0x46
         ldind.r8
         ldloca.s       0x46
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x47
         dup
         conv.r8
         stind.r8
         ldloca.s       0x47
         ldind.r8
         ldloca.s       0x47
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x48
         dup
         conv.r8
         stind.r8
         ldloca.s       0x48
         ldind.r8
         ldloca.s       0x48
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x49
         dup
         conv.r8
         stind.r8
         ldloca.s       0x49
         ldind.r8
         ldloca.s       0x49
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4a
         dup
         conv.r8
         stind.r8
         ldloca.s       0x4a
         ldind.r8
         ldloca.s       0x4a
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4b
         dup
         conv.r8
         stind.r8
         ldloca.s       0x4b
         ldind.r8
         ldloca.s       0x4b
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4c
         dup
         conv.r8
         stind.r8
         ldloca.s       0x4c
         ldind.r8
         ldloca.s       0x4c
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4d
         dup
         conv.r8
         stind.r8
         ldloca.s       0x4d
         ldind.r8
         ldloca.s       0x4d
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4e
         dup
         conv.r8
         stind.r8
         ldloca.s       0x4e
         ldind.r8
         ldloca.s       0x4e
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x4f
         dup
         conv.r8
         stind.r8
         ldloca.s       0x4f
         ldind.r8
         ldloca.s       0x4f
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x50
         dup
         conv.r8
         stind.r8
         ldloca.s       0x50
         ldind.r8
         ldloca.s       0x50
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x51
         dup
         conv.r8
         stind.r8
         ldloca.s       0x51
         ldind.r8
         ldloca.s       0x51
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x52
         dup
         conv.r8
         stind.r8
         ldloca.s       0x52
         ldind.r8
         ldloca.s       0x52
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x53
         dup
         conv.r8
         stind.r8
         ldloca.s       0x53
         ldind.r8
         ldloca.s       0x53
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x54
         dup
         conv.r8
         stind.r8
         ldloca.s       0x54
         ldind.r8
         ldloca.s       0x54
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x55
         dup
         conv.r8
         stind.r8
         ldloca.s       0x55
         ldind.r8
         ldloca.s       0x55
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x56
         dup
         conv.r8
         stind.r8
         ldloca.s       0x56
         ldind.r8
         ldloca.s       0x56
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x57
         dup
         conv.r8
         stind.r8
         ldloca.s       0x57
         ldind.r8
         ldloca.s       0x57
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x58
         dup
         conv.r8
         stind.r8
         ldloca.s       0x58
         ldind.r8
         ldloca.s       0x58
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x59
         dup
         conv.r8
         stind.r8
         ldloca.s       0x59
         ldind.r8
         ldloca.s       0x59
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5a
         dup
         conv.r8
         stind.r8
         ldloca.s       0x5a
         ldind.r8
         ldloca.s       0x5a
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5b
         dup
         conv.r8
         stind.r8
         ldloca.s       0x5b
         ldind.r8
         ldloca.s       0x5b
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5c
         dup
         conv.r8
         stind.r8
         ldloca.s       0x5c
         ldind.r8
         ldloca.s       0x5c
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5d
         dup
         conv.r8
         stind.r8
         ldloca.s       0x5d
         ldind.r8
         ldloca.s       0x5d
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5e
         dup
         conv.r8
         stind.r8
         ldloca.s       0x5e
         ldind.r8
         ldloca.s       0x5e
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x5f
         dup
         conv.r8
         stind.r8
         ldloca.s       0x5f
         ldind.r8
         ldloca.s       0x5f
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x60
         dup
         conv.r8
         stind.r8
         ldloca.s       0x60
         ldind.r8
         ldloca.s       0x60
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x61
         dup
         conv.r8
         stind.r8
         ldloca.s       0x61
         ldind.r8
         ldloca.s       0x61
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x62
         dup
         conv.r8
         stind.r8
         ldloca.s       0x62
         ldind.r8
         ldloca.s       0x62
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x63
         dup
         conv.r8
         stind.r8
         ldloca.s       0x63
         ldind.r8
         ldloca.s       0x63
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x64
         dup
         conv.r8
         stind.r8
         ldloca.s       0x64
         ldind.r8
         ldloca.s       0x64
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x65
         dup
         conv.r8
         stind.r8
         ldloca.s       0x65
         ldind.r8
         ldloca.s       0x65
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x66
         dup
         conv.r8
         stind.r8
         ldloca.s       0x66
         ldind.r8
         ldloca.s       0x66
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x67
         dup
         conv.r8
         stind.r8
         ldloca.s       0x67
         ldind.r8
         ldloca.s       0x67
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x68
         dup
         conv.r8
         stind.r8
         ldloca.s       0x68
         ldind.r8
         ldloca.s       0x68
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x69
         dup
         conv.r8
         stind.r8
         ldloca.s       0x69
         ldind.r8
         ldloca.s       0x69
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6a
         dup
         conv.r8
         stind.r8
         ldloca.s       0x6a
         ldind.r8
         ldloca.s       0x6a
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6b
         dup
         conv.r8
         stind.r8
         ldloca.s       0x6b
         ldind.r8
         ldloca.s       0x6b
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6c
         dup
         conv.r8
         stind.r8
         ldloca.s       0x6c
         ldind.r8
         ldloca.s       0x6c
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6d
         dup
         conv.r8
         stind.r8
         ldloca.s       0x6d
         ldind.r8
         ldloca.s       0x6d
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6e
         dup
         conv.r8
         stind.r8
         ldloca.s       0x6e
         ldind.r8
         ldloca.s       0x6e
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x6f
         dup
         conv.r8
         stind.r8
         ldloca.s       0x6f
         ldind.r8
         ldloca.s       0x6f
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x70
         dup
         conv.r8
         stind.r8
         ldloca.s       0x70
         ldind.r8
         ldloca.s       0x70
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x71
         dup
         conv.r8
         stind.r8
         ldloca.s       0x71
         ldind.r8
         ldloca.s       0x71
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x72
         dup
         conv.r8
         stind.r8
         ldloca.s       0x72
         ldind.r8
         ldloca.s       0x72
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x73
         dup
         conv.r8
         stind.r8
         ldloca.s       0x73
         ldind.r8
         ldloca.s       0x73
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x74
         dup
         conv.r8
         stind.r8
         ldloca.s       0x74
         ldind.r8
         ldloca.s       0x74
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x75
         dup
         conv.r8
         stind.r8
         ldloca.s       0x75
         ldind.r8
         ldloca.s       0x75
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x76
         dup
         conv.r8
         stind.r8
         ldloca.s       0x76
         ldind.r8
         ldloca.s       0x76
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x77
         dup
         conv.r8
         stind.r8
         ldloca.s       0x77
         ldind.r8
         ldloca.s       0x77
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x78
         dup
         conv.r8
         stind.r8
         ldloca.s       0x78
         ldind.r8
         ldloca.s       0x78
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x79
         dup
         conv.r8
         stind.r8
         ldloca.s       0x79
         ldind.r8
         ldloca.s       0x79
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7a
         dup
         conv.r8
         stind.r8
         ldloca.s       0x7a
         ldind.r8
         ldloca.s       0x7a
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7b
         dup
         conv.r8
         stind.r8
         ldloca.s       0x7b
         ldind.r8
         ldloca.s       0x7b
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7c
         dup
         conv.r8
         stind.r8
         ldloca.s       0x7c
         ldind.r8
         ldloca.s       0x7c
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7d
         dup
         conv.r8
         stind.r8
         ldloca.s       0x7d
         ldind.r8
         ldloca.s       0x7d
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7e
         dup
         conv.r8
         stind.r8
         ldloca.s       0x7e
         ldind.r8
         ldloca.s       0x7e
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x7f
         dup
         conv.r8
         stind.r8
         ldloca.s       0x7f
         ldind.r8
         ldloca.s       0x7f
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x80
         dup
         conv.r8
         stind.r8
         ldloca.s       0x80
         ldind.r8
         ldloca.s       0x80
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x81
         dup
         conv.r8
         stind.r8
         ldloca.s       0x81
         ldind.r8
         ldloca.s       0x81
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x82
         dup
         conv.r8
         stind.r8
         ldloca.s       0x82
         ldind.r8
         ldloca.s       0x82
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x83
         dup
         conv.r8
         stind.r8
         ldloca.s       0x83
         ldind.r8
         ldloca.s       0x83
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x84
         dup
         conv.r8
         stind.r8
         ldloca.s       0x84
         ldind.r8
         ldloca.s       0x84
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x85
         dup
         conv.r8
         stind.r8
         ldloca.s       0x85
         ldind.r8
         ldloca.s       0x85
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x86
         dup
         conv.r8
         stind.r8
         ldloca.s       0x86
         ldind.r8
         ldloca.s       0x86
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x87
         dup
         conv.r8
         stind.r8
         ldloca.s       0x87
         ldind.r8
         ldloca.s       0x87
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x88
         dup
         conv.r8
         stind.r8
         ldloca.s       0x88
         ldind.r8
         ldloca.s       0x88
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x89
         dup
         conv.r8
         stind.r8
         ldloca.s       0x89
         ldind.r8
         ldloca.s       0x89
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8a
         dup
         conv.r8
         stind.r8
         ldloca.s       0x8a
         ldind.r8
         ldloca.s       0x8a
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8b
         dup
         conv.r8
         stind.r8
         ldloca.s       0x8b
         ldind.r8
         ldloca.s       0x8b
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8c
         dup
         conv.r8
         stind.r8
         ldloca.s       0x8c
         ldind.r8
         ldloca.s       0x8c
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8d
         dup
         conv.r8
         stind.r8
         ldloca.s       0x8d
         ldind.r8
         ldloca.s       0x8d
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8e
         dup
         conv.r8
         stind.r8
         ldloca.s       0x8e
         ldind.r8
         ldloca.s       0x8e
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x8f
         dup
         conv.r8
         stind.r8
         ldloca.s       0x8f
         ldind.r8
         ldloca.s       0x8f
         conv.r8
         ceq
         brfalse        FAIL

         ldloca.s       0x90
         dup
         conv.r8
         stind.r8
         ldloca.s       0x90
         ldind.r8
         ldloca.s       0x90
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x91
         dup
         conv.r8
         stind.r8
         ldloca.s       0x91
         ldind.r8
         ldloca.s       0x91
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x92
         dup
         conv.r8
         stind.r8
         ldloca.s       0x92
         ldind.r8
         ldloca.s       0x92
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x93
         dup
         conv.r8
         stind.r8
         ldloca.s       0x93
         ldind.r8
         ldloca.s       0x93
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x94
         dup
         conv.r8
         stind.r8
         ldloca.s       0x94
         ldind.r8
         ldloca.s       0x94
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x95
         dup
         conv.r8
         stind.r8
         ldloca.s       0x95
         ldind.r8
         ldloca.s       0x95
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x96
         dup
         conv.r8
         stind.r8
         ldloca.s       0x96
         ldind.r8
         ldloca.s       0x96
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x97
         dup
         conv.r8
         stind.r8
         ldloca.s       0x97
         ldind.r8
         ldloca.s       0x97
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x98
         dup
         conv.r8
         stind.r8
         ldloca.s       0x98
         ldind.r8
         ldloca.s       0x98
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x99
         dup
         conv.r8
         stind.r8
         ldloca.s       0x99
         ldind.r8
         ldloca.s       0x99
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9a
         dup
         conv.r8
         stind.r8
         ldloca.s       0x9a
         ldind.r8
         ldloca.s       0x9a
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9b
         dup
         conv.r8
         stind.r8
         ldloca.s       0x9b
         ldind.r8
         ldloca.s       0x9b
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9c
         dup
         conv.r8
         stind.r8
         ldloca.s       0x9c
         ldind.r8
         ldloca.s       0x9c
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9d
         dup
         conv.r8
         stind.r8
         ldloca.s       0x9d
         ldind.r8
         ldloca.s       0x9d
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9e
         dup
         conv.r8
         stind.r8
         ldloca.s       0x9e
         ldind.r8
         ldloca.s       0x9e
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0x9f
         dup
         conv.r8
         stind.r8
         ldloca.s       0x9f
         ldind.r8
         ldloca.s       0x9f
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa0
         dup
         conv.r8
         stind.r8
         ldloca.s       0xa0
         ldind.r8
         ldloca.s       0xa0
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa1
         dup
         conv.r8
         stind.r8
         ldloca.s       0xa1
         ldind.r8
         ldloca.s       0xa1
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa2
         dup
         conv.r8
         stind.r8
         ldloca.s       0xa2
         ldind.r8
         ldloca.s       0xa2
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa3
         dup
         conv.r8
         stind.r8
         ldloca.s       0xa3
         ldind.r8
         ldloca.s       0xa3
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa4
         dup
         conv.r8
         stind.r8
         ldloca.s       0xa4
         ldind.r8
         ldloca.s       0xa4
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa5
         dup
         conv.r8
         stind.r8
         ldloca.s       0xa5
         ldind.r8
         ldloca.s       0xa5
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa6
         dup
         conv.r8
         stind.r8
         ldloca.s       0xa6
         ldind.r8
         ldloca.s       0xa6
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa7
         dup
         conv.r8
         stind.r8
         ldloca.s       0xa7
         ldind.r8
         ldloca.s       0xa7
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa8
         dup
         conv.r8
         stind.r8
         ldloca.s       0xa8
         ldind.r8
         ldloca.s       0xa8
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xa9
         dup
         conv.r8
         stind.r8
         ldloca.s       0xa9
         ldind.r8
         ldloca.s       0xa9
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xaa
         dup
         conv.r8
         stind.r8
         ldloca.s       0xaa
         ldind.r8
         ldloca.s       0xaa
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xab
         dup
         conv.r8
         stind.r8
         ldloca.s       0xab
         ldind.r8
         ldloca.s       0xab
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xac
         dup
         conv.r8
         stind.r8
         ldloca.s       0xac
         ldind.r8
         ldloca.s       0xac
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xad
         dup
         conv.r8
         stind.r8
         ldloca.s       0xad
         ldind.r8
         ldloca.s       0xad
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xae
         dup
         conv.r8
         stind.r8
         ldloca.s       0xae
         ldind.r8
         ldloca.s       0xae
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xaf
         dup
         conv.r8
         stind.r8
         ldloca.s       0xaf
         ldind.r8
         ldloca.s       0xaf
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb0
         dup
         conv.r8
         stind.r8
         ldloca.s       0xb0
         ldind.r8
         ldloca.s       0xb0
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb1
         dup
         conv.r8
         stind.r8
         ldloca.s       0xb1
         ldind.r8
         ldloca.s       0xb1
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb2
         dup
         conv.r8
         stind.r8
         ldloca.s       0xb2
         ldind.r8
         ldloca.s       0xb2
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb3
         dup
         conv.r8
         stind.r8
         ldloca.s       0xb3
         ldind.r8
         ldloca.s       0xb3
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb4
         dup
         conv.r8
         stind.r8
         ldloca.s       0xb4
         ldind.r8
         ldloca.s       0xb4
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb5
         dup
         conv.r8
         stind.r8
         ldloca.s       0xb5
         ldind.r8
         ldloca.s       0xb5
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb6
         dup
         conv.r8
         stind.r8
         ldloca.s       0xb6
         ldind.r8
         ldloca.s       0xb6
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb7
         dup
         conv.r8
         stind.r8
         ldloca.s       0xb7
         ldind.r8
         ldloca.s       0xb7
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb8
         dup
         conv.r8
         stind.r8
         ldloca.s       0xb8
         ldind.r8
         ldloca.s       0xb8
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xb9
         dup
         conv.r8
         stind.r8
         ldloca.s       0xb9
         ldind.r8
         ldloca.s       0xb9
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xba
         dup
         conv.r8
         stind.r8
         ldloca.s       0xba
         ldind.r8
         ldloca.s       0xba
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xbb
         dup
         conv.r8
         stind.r8
         ldloca.s       0xbb
         ldind.r8
         ldloca.s       0xbb
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xbc
         dup
         conv.r8
         stind.r8
         ldloca.s       0xbc
         ldind.r8
         ldloca.s       0xbc
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xbd
         dup
         conv.r8
         stind.r8
         ldloca.s       0xbd
         ldind.r8
         ldloca.s       0xbd
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xbe
         dup
         conv.r8
         stind.r8
         ldloca.s       0xbe
         ldind.r8
         ldloca.s       0xbe
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xbf
         dup
         conv.r8
         stind.r8
         ldloca.s       0xbf
         ldind.r8
         ldloca.s       0xbf
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc0
         dup
         conv.r8
         stind.r8
         ldloca.s       0xc0
         ldind.r8
         ldloca.s       0xc0
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc1
         dup
         conv.r8
         stind.r8
         ldloca.s       0xc1
         ldind.r8
         ldloca.s       0xc1
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc2
         dup
         conv.r8
         stind.r8
         ldloca.s       0xc2
         ldind.r8
         ldloca.s       0xc2
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc3
         dup
         conv.r8
         stind.r8
         ldloca.s       0xc3
         ldind.r8
         ldloca.s       0xc3
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc4
         dup
         conv.r8
         stind.r8
         ldloca.s       0xc4
         ldind.r8
         ldloca.s       0xc4
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc5
         dup
         conv.r8
         stind.r8
         ldloca.s       0xc5
         ldind.r8
         ldloca.s       0xc5
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc6
         dup
         conv.r8
         stind.r8
         ldloca.s       0xc6
         ldind.r8
         ldloca.s       0xc6
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc7
         dup
         conv.r8
         stind.r8
         ldloca.s       0xc7
         ldind.r8
         ldloca.s       0xc7
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc8
         dup
         conv.r8
         stind.r8
         ldloca.s       0xc8
         ldind.r8
         ldloca.s       0xc8
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xc9
         dup
         conv.r8
         stind.r8
         ldloca.s       0xc9
         ldind.r8
         ldloca.s       0xc9
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xca
         dup
         conv.r8
         stind.r8
         ldloca.s       0xca
         ldind.r8
         ldloca.s       0xca
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xcb
         dup
         conv.r8
         stind.r8
         ldloca.s       0xcb
         ldind.r8
         ldloca.s       0xcb
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xcc
         dup
         conv.r8
         stind.r8
         ldloca.s       0xcc
         ldind.r8
         ldloca.s       0xcc
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xcd
         dup
         conv.r8
         stind.r8
         ldloca.s       0xcd
         ldind.r8
         ldloca.s       0xcd
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xce
         dup
         conv.r8
         stind.r8
         ldloca.s       0xce
         ldind.r8
         ldloca.s       0xce
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xcf
         dup
         conv.r8
         stind.r8
         ldloca.s       0xcf
         ldind.r8
         ldloca.s       0xcf
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd0
         dup
         conv.r8
         stind.r8
         ldloca.s       0xd0
         ldind.r8
         ldloca.s       0xd0
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd1
         dup
         conv.r8
         stind.r8
         ldloca.s       0xd1
         ldind.r8
         ldloca.s       0xd1
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd2
         dup
         conv.r8
         stind.r8
         ldloca.s       0xd2
         ldind.r8
         ldloca.s       0xd2
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd3
         dup
         conv.r8
         stind.r8
         ldloca.s       0xd3
         ldind.r8
         ldloca.s       0xd3
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd4
         dup
         conv.r8
         stind.r8
         ldloca.s       0xd4
         ldind.r8
         ldloca.s       0xd4
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd5
         dup
         conv.r8
         stind.r8
         ldloca.s       0xd5
         ldind.r8
         ldloca.s       0xd5
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd6
         dup
         conv.r8
         stind.r8
         ldloca.s       0xd6
         ldind.r8
         ldloca.s       0xd6
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd7
         dup
         conv.r8
         stind.r8
         ldloca.s       0xd7
         ldind.r8
         ldloca.s       0xd7
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd8
         dup
         conv.r8
         stind.r8
         ldloca.s       0xd8
         ldind.r8
         ldloca.s       0xd8
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xd9
         dup
         conv.r8
         stind.r8
         ldloca.s       0xd9
         ldind.r8
         ldloca.s       0xd9
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xda
         dup
         conv.r8
         stind.r8
         ldloca.s       0xda
         ldind.r8
         ldloca.s       0xda
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xdb
         dup
         conv.r8
         stind.r8
         ldloca.s       0xdb
         ldind.r8
         ldloca.s       0xdb
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xdc
         dup
         conv.r8
         stind.r8
         ldloca.s       0xdc
         ldind.r8
         ldloca.s       0xdc
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xdd
         dup
         conv.r8
         stind.r8
         ldloca.s       0xdd
         ldind.r8
         ldloca.s       0xdd
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xde
         dup
         conv.r8
         stind.r8
         ldloca.s       0xde
         ldind.r8
         ldloca.s       0xde
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xdf
         dup
         conv.r8
         stind.r8
         ldloca.s       0xdf
         ldind.r8
         ldloca.s       0xdf
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe0
         dup
         conv.r8
         stind.r8
         ldloca.s       0xe0
         ldind.r8
         ldloca.s       0xe0
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe1
         dup
         conv.r8
         stind.r8
         ldloca.s       0xe1
         ldind.r8
         ldloca.s       0xe1
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe2
         dup
         conv.r8
         stind.r8
         ldloca.s       0xe2
         ldind.r8
         ldloca.s       0xe2
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe3
         dup
         conv.r8
         stind.r8
         ldloca.s       0xe3
         ldind.r8
         ldloca.s       0xe3
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe4
         dup
         conv.r8
         stind.r8
         ldloca.s       0xe4
         ldind.r8
         ldloca.s       0xe4
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe5
         dup
         conv.r8
         stind.r8
         ldloca.s       0xe5
         ldind.r8
         ldloca.s       0xe5
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe6
         dup
         conv.r8
         stind.r8
         ldloca.s       0xe6
         ldind.r8
         ldloca.s       0xe6
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe7
         dup
         conv.r8
         stind.r8
         ldloca.s       0xe7
         ldind.r8
         ldloca.s       0xe7
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe8
         dup
         conv.r8
         stind.r8
         ldloca.s       0xe8
         ldind.r8
         ldloca.s       0xe8
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xe9
         dup
         conv.r8
         stind.r8
         ldloca.s       0xe9
         ldind.r8
         ldloca.s       0xe9
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xea
         dup
         conv.r8
         stind.r8
         ldloca.s       0xea
         ldind.r8
         ldloca.s       0xea
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xeb
         dup
         conv.r8
         stind.r8
         ldloca.s       0xeb
         ldind.r8
         ldloca.s       0xeb
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xec
         dup
         conv.r8
         stind.r8
         ldloca.s       0xec
         ldind.r8
         ldloca.s       0xec
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xed
         dup
         conv.r8
         stind.r8
         ldloca.s       0xed
         ldind.r8
         ldloca.s       0xed
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xee
         dup
         conv.r8
         stind.r8
         ldloca.s       0xee
         ldind.r8
         ldloca.s       0xee
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xef
         dup
         conv.r8
         stind.r8
         ldloca.s       0xef
         ldind.r8
         ldloca.s       0xef
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf0
         dup
         conv.r8
         stind.r8
         ldloca.s       0xf0
         ldind.r8
         ldloca.s       0xf0
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf1
         dup
         conv.r8
         stind.r8
         ldloca.s       0xf1
         ldind.r8
         ldloca.s       0xf1
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf2
         dup
         conv.r8
         stind.r8
         ldloca.s       0xf2
         ldind.r8
         ldloca.s       0xf2
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf3
         dup
         conv.r8
         stind.r8
         ldloca.s       0xf3
         ldind.r8
         ldloca.s       0xf3
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf4
         dup
         conv.r8
         stind.r8
         ldloca.s       0xf4
         ldind.r8
         ldloca.s       0xf4
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf5
         dup
         conv.r8
         stind.r8
         ldloca.s       0xf5
         ldind.r8
         ldloca.s       0xf5
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf6
         dup
         conv.r8
         stind.r8
         ldloca.s       0xf6
         ldind.r8
         ldloca.s       0xf6
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf7
         dup
         conv.r8
         stind.r8
         ldloca.s       0xf7
         ldind.r8
         ldloca.s       0xf7
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf8
         dup
         conv.r8
         stind.r8
         ldloca.s       0xf8
         ldind.r8
         ldloca.s       0xf8
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xf9
         dup
         conv.r8
         stind.r8
         ldloca.s       0xf9
         ldind.r8
         ldloca.s       0xf9
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xfa
         dup
         conv.r8
         stind.r8
         ldloca.s       0xfa
         ldind.r8
         ldloca.s       0xfa
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xfb
         dup
         conv.r8
         stind.r8
         ldloca.s       0xfb
         ldind.r8
         ldloca.s       0xfb
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xfc
         dup
         conv.r8
         stind.r8
         ldloca.s       0xfc
         ldind.r8
         ldloca.s       0xfc
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xfd
         dup
         conv.r8
         stind.r8
         ldloca.s       0xfd
         ldind.r8
         ldloca.s       0xfd
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xfe
         dup
         conv.r8
         stind.r8
         ldloca.s       0xfe
         ldind.r8
         ldloca.s       0xfe
         conv.r8
         ceq
         brfalse        FAIL
         
         ldloca.s       0xff
         dup
         conv.r8
         stind.r8
         ldloca.s       0xff
         ldind.r8
         ldloca.s       0xff
         conv.r8
         ceq
         brfalse        FAIL
         
         ldc.i4.1
         ret
         
FAIL:
        ldloc       0
	call	int32 [mscorlib]System.Convert::ToInt32(float64)
        ret         
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack       2
        call            int32 ldloca_s_r8::test_float64()
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
