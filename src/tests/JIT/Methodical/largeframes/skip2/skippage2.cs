// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Consider a case of potentially skipping probing a page on ARM64.
// 1. Have a function with a frame just under a page size, so it doesn't
//    require a probing loop. Most of the frame size is outgoing argument
//    space, which is untouched.
// 2. Call a function that doesn't force touching the outgoing argument
//    space.
// 3. The called function has a frame size <=504 bytes with no outgoing
//    arguments. Then, the first page touch will be at [sp-504] when
//    FP/LR is saved, and SP is set.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace BigFrames_skippage2
{

    [StructLayout(LayoutKind.Explicit)]
    public struct Struct420
    {
        [FieldOffset(0)]
        public int i1;
        [FieldOffset(416)]
        public int i2;
    }

    public class Test
    {
        public static int iret = 100;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void BigArgSpace(
            long i1, long i2, long i3, long i4, long i5, long i6, long i7, long i8, long i9, long i10,
            long i11, long i12, long i13, long i14, long i15, long i16, long i17, long i18, long i19, long i20,
            long i21, long i22, long i23, long i24, long i25, long i26, long i27, long i28, long i29, long i30,
            long i31, long i32, long i33, long i34, long i35, long i36, long i37, long i38, long i39, long i40,
            long i41, long i42, long i43, long i44, long i45, long i46, long i47, long i48, long i49, long i50,
            long i51, long i52, long i53, long i54, long i55, long i56, long i57, long i58, long i59, long i60,
            long i61, long i62, long i63, long i64, long i65, long i66, long i67, long i68, long i69, long i70,
            long i71, long i72, long i73, long i74, long i75, long i76, long i77, long i78, long i79, long i80,
            long i81, long i82, long i83, long i84, long i85, long i86, long i87, long i88, long i89, long i90,
            long i91, long i92, long i93, long i94, long i95, long i96, long i97, long i98, long i99, long i100,
            long i101, long i102, long i103, long i104, long i105, long i106, long i107, long i108, long i109, long i110,
            long i111, long i112, long i113, long i114, long i115, long i116, long i117, long i118, long i119, long i120,
            long i121, long i122, long i123, long i124, long i125, long i126, long i127, long i128, long i129, long i130,
            long i131, long i132, long i133, long i134, long i135, long i136, long i137, long i138, long i139, long i140,
            long i141, long i142, long i143, long i144, long i145, long i146, long i147, long i148, long i149, long i150,
            long i151, long i152, long i153, long i154, long i155, long i156, long i157, long i158, long i159, long i160,
            long i161, long i162, long i163, long i164, long i165, long i166, long i167, long i168, long i169, long i170,
            long i171, long i172, long i173, long i174, long i175, long i176, long i177, long i178, long i179, long i180,
            long i181, long i182, long i183, long i184, long i185, long i186, long i187, long i188, long i189, long i190,
            long i191, long i192, long i193, long i194, long i195, long i196, long i197, long i198, long i199, long i200,
            long i201, long i202, long i203, long i204, long i205, long i206, long i207, long i208, long i209, long i210,
            long i211, long i212, long i213, long i214, long i215, long i216, long i217, long i218, long i219, long i220,
            long i221, long i222, long i223, long i224, long i225, long i226, long i227, long i228, long i229, long i230,
            long i231, long i232, long i233, long i234, long i235, long i236, long i237, long i238, long i239, long i240,
            long i241, long i242, long i243, long i244, long i245, long i246, long i247, long i248, long i249, long i250,
            long i251, long i252, long i253, long i254, long i255, long i256, long i257, long i258, long i259, long i260,
            long i261, long i262, long i263, long i264, long i265, long i266, long i267, long i268, long i269, long i270,
            long i271, long i272, long i273, long i274, long i275, long i276, long i277, long i278, long i279, long i280,
            long i281, long i282, long i283, long i284, long i285, long i286, long i287, long i288, long i289, long i290,
            long i291, long i292, long i293, long i294, long i295, long i296, long i297, long i298, long i299, long i300,
            long i301, long i302, long i303, long i304, long i305, long i306, long i307, long i308, long i309, long i310,
            long i311, long i312, long i313, long i314, long i315, long i316, long i317, long i318, long i319, long i320,
            long i321, long i322, long i323, long i324, long i325, long i326, long i327, long i328, long i329, long i330,
            long i331, long i332, long i333, long i334, long i335, long i336, long i337, long i338, long i339, long i340,
            long i341, long i342, long i343, long i344, long i345, long i346, long i347, long i348, long i349, long i350,
            long i351, long i352, long i353, long i354, long i355, long i356, long i357, long i358, long i359, long i360,
            long i361, long i362, long i363, long i364, long i365, long i366, long i367, long i368, long i369, long i370,
            long i371, long i372, long i373, long i374, long i375, long i376, long i377, long i378, long i379, long i380,
            long i381, long i382, long i383, long i384, long i385, long i386, long i387, long i388, long i389, long i390,
            long i391, long i392, long i393, long i394, long i395, long i396, long i397, long i398, long i399, long i400,
            long i401, long i402, long i403, long i404, long i405, long i406, long i407, long i408, long i409, long i410,
            long i411, long i412, long i413, long i414, long i415, long i416, long i417, long i418, long i419, long i420,
            long i421, long i422, long i423, long i424, long i425, long i426, long i427, long i428, long i429, long i430,
            long i431, long i432, long i433, long i434, long i435, long i436, long i437, long i438, long i439, long i440,
            long i441, long i442, long i443, long i444, long i445, long i446, long i447, long i448, long i449, long i450,
            long i451, long i452, long i453, long i454, long i455, long i456, long i457, long i458, long i459, long i460,
            long i461, long i462, long i463, long i464, long i465, long i466, long i467, long i468, long i469, long i470,
            long i471, long i472, long i473, long i474, long i475, long i476, long i477, long i478, long i479, long i480,
            long i481, long i482, long i483, long i484, long i485, long i486, long i487, long i488, long i489, long i490,
            long i491, long i492, long i493, long i494, long i495, long i496, long i497, long i498, long i499, long i500
                )
        {
            long result =
            i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8 + i9 + i10 +
            i11 + i12 + i13 + i14 + i15 + i16 + i17 + i18 + i19 + i20 +
            i21 + i22 + i23 + i24 + i25 + i26 + i27 + i28 + i29 + i30 +
            i31 + i32 + i33 + i34 + i35 + i36 + i37 + i38 + i39 + i40 +
            i41 + i42 + i43 + i44 + i45 + i46 + i47 + i48 + i49 + i50 +
            i51 + i52 + i53 + i54 + i55 + i56 + i57 + i58 + i59 + i60 +
            i61 + i62 + i63 + i64 + i65 + i66 + i67 + i68 + i69 + i70 +
            i71 + i72 + i73 + i74 + i75 + i76 + i77 + i78 + i79 + i80 +
            i81 + i82 + i83 + i84 + i85 + i86 + i87 + i88 + i89 + i90 +
            i91 + i92 + i93 + i94 + i95 + i96 + i97 + i98 + i99 + i100 +
            i101 + i102 + i103 + i104 + i105 + i106 + i107 + i108 + i109 + i110 +
            i111 + i112 + i113 + i114 + i115 + i116 + i117 + i118 + i119 + i120 +
            i121 + i122 + i123 + i124 + i125 + i126 + i127 + i128 + i129 + i130 +
            i131 + i132 + i133 + i134 + i135 + i136 + i137 + i138 + i139 + i140 +
            i141 + i142 + i143 + i144 + i145 + i146 + i147 + i148 + i149 + i150 +
            i151 + i152 + i153 + i154 + i155 + i156 + i157 + i158 + i159 + i160 +
            i161 + i162 + i163 + i164 + i165 + i166 + i167 + i168 + i169 + i170 +
            i171 + i172 + i173 + i174 + i175 + i176 + i177 + i178 + i179 + i180 +
            i181 + i182 + i183 + i184 + i185 + i186 + i187 + i188 + i189 + i190 +
            i191 + i192 + i193 + i194 + i195 + i196 + i197 + i198 + i199 + i200 +
            i201 + i202 + i203 + i204 + i205 + i206 + i207 + i208 + i209 + i210 +
            i211 + i212 + i213 + i214 + i215 + i216 + i217 + i218 + i219 + i220 +
            i221 + i222 + i223 + i224 + i225 + i226 + i227 + i228 + i229 + i230 +
            i231 + i232 + i233 + i234 + i235 + i236 + i237 + i238 + i239 + i240 +
            i241 + i242 + i243 + i244 + i245 + i246 + i247 + i248 + i249 + i250 +
            i251 + i252 + i253 + i254 + i255 + i256 + i257 + i258 + i259 + i260 +
            i261 + i262 + i263 + i264 + i265 + i266 + i267 + i268 + i269 + i270 +
            i271 + i272 + i273 + i274 + i275 + i276 + i277 + i278 + i279 + i280 +
            i281 + i282 + i283 + i284 + i285 + i286 + i287 + i288 + i289 + i290 +
            i291 + i292 + i293 + i294 + i295 + i296 + i297 + i298 + i299 + i300 +
            i301 + i302 + i303 + i304 + i305 + i306 + i307 + i308 + i309 + i310 +
            i311 + i312 + i313 + i314 + i315 + i316 + i317 + i318 + i319 + i320 +
            i321 + i322 + i323 + i324 + i325 + i326 + i327 + i328 + i329 + i330 +
            i331 + i332 + i333 + i334 + i335 + i336 + i337 + i338 + i339 + i340 +
            i341 + i342 + i343 + i344 + i345 + i346 + i347 + i348 + i349 + i350 +
            i351 + i352 + i353 + i354 + i355 + i356 + i357 + i358 + i359 + i360 +
            i361 + i362 + i363 + i364 + i365 + i366 + i367 + i368 + i369 + i370 +
            i371 + i372 + i373 + i374 + i375 + i376 + i377 + i378 + i379 + i380 +
            i381 + i382 + i383 + i384 + i385 + i386 + i387 + i388 + i389 + i390 +
            i391 + i392 + i393 + i394 + i395 + i396 + i397 + i398 + i399 + i400 +
            i401 + i402 + i403 + i404 + i405 + i406 + i407 + i408 + i409 + i410 +
            i411 + i412 + i413 + i414 + i415 + i416 + i417 + i418 + i419 + i420 +
            i421 + i422 + i423 + i424 + i425 + i426 + i427 + i428 + i429 + i430 +
            i431 + i432 + i433 + i434 + i435 + i436 + i437 + i438 + i439 + i440 +
            i441 + i442 + i443 + i444 + i445 + i446 + i447 + i448 + i449 + i450 +
            i451 + i452 + i453 + i454 + i455 + i456 + i457 + i458 + i459 + i460 +
            i461 + i462 + i463 + i464 + i465 + i466 + i467 + i468 + i469 + i470 +
            i471 + i472 + i473 + i474 + i475 + i476 + i477 + i478 + i479 + i480 +
            i481 + i482 + i483 + i484 + i485 + i486 + i487 + i488 + i489 + i490 +
            i491 + i492 + i493 + i494 + i495 + i496 + i497 + i498 + i499 + i500
            ;

            Console.WriteLine(result);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void KeepStruct420Alive(ref Struct420 s)
        {
            Console.WriteLine(s.i2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void SmallFrameSize()
        {
            Struct420 s = new Struct420();
            s.i2 = 7;
            KeepStruct420Alive(ref s);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1(bool call_struct_function)
        {
            if (call_struct_function)
            {
                BigArgSpace(
                    1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
                    11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                    21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
                    31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
                    41, 42, 43, 44, 45, 46, 47, 48, 49, 50,
                    51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
                    61, 62, 63, 64, 65, 66, 67, 68, 69, 70,
                    71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
                    81, 82, 83, 84, 85, 86, 87, 88, 89, 90,
                    91, 92, 93, 94, 95, 96, 97, 98, 99, 100,
                    101, 102, 103, 104, 105, 106, 107, 108, 109, 110,
                    111, 112, 113, 114, 115, 116, 117, 118, 119, 120,
                    121, 122, 123, 124, 125, 126, 127, 128, 129, 130,
                    131, 132, 133, 134, 135, 136, 137, 138, 139, 140,
                    141, 142, 143, 144, 145, 146, 147, 148, 149, 150,
                    151, 152, 153, 154, 155, 156, 157, 158, 159, 160,
                    161, 162, 163, 164, 165, 166, 167, 168, 169, 170,
                    171, 172, 173, 174, 175, 176, 177, 178, 179, 180,
                    181, 182, 183, 184, 185, 186, 187, 188, 189, 190,
                    191, 192, 193, 194, 195, 196, 197, 198, 199, 200,
                    201, 202, 203, 204, 205, 206, 207, 208, 209, 210,
                    211, 212, 213, 214, 215, 216, 217, 218, 219, 220,
                    221, 222, 223, 224, 225, 226, 227, 228, 229, 230,
                    231, 232, 233, 234, 235, 236, 237, 238, 239, 240,
                    241, 242, 243, 244, 245, 246, 247, 248, 249, 250,
                    251, 252, 253, 254, 255, 256, 257, 258, 259, 260,
                    261, 262, 263, 264, 265, 266, 267, 268, 269, 270,
                    271, 272, 273, 274, 275, 276, 277, 278, 279, 280,
                    281, 282, 283, 284, 285, 286, 287, 288, 289, 290,
                    291, 292, 293, 294, 295, 296, 297, 298, 299, 300,
                    301, 302, 303, 304, 305, 306, 307, 308, 309, 310,
                    311, 312, 313, 314, 315, 316, 317, 318, 319, 320,
                    321, 322, 323, 324, 325, 326, 327, 328, 329, 330,
                    331, 332, 333, 334, 335, 336, 337, 338, 339, 340,
                    341, 342, 343, 344, 345, 346, 347, 348, 349, 350,
                    351, 352, 353, 354, 355, 356, 357, 358, 359, 360,
                    361, 362, 363, 364, 365, 366, 367, 368, 369, 370,
                    371, 372, 373, 374, 375, 376, 377, 378, 379, 380,
                    381, 382, 383, 384, 385, 386, 387, 388, 389, 390,
                    391, 392, 393, 394, 395, 396, 397, 398, 399, 400,
                    401, 402, 403, 404, 405, 406, 407, 408, 409, 410,
                    411, 412, 413, 414, 415, 416, 417, 418, 419, 420,
                    421, 422, 423, 424, 425, 426, 427, 428, 429, 430,
                    431, 432, 433, 434, 435, 436, 437, 438, 439, 440,
                    441, 442, 443, 444, 445, 446, 447, 448, 449, 450,
                    451, 452, 453, 454, 455, 456, 457, 458, 459, 460,
                    461, 462, 463, 464, 465, 466, 467, 468, 469, 470,
                    471, 472, 473, 474, 475, 476, 477, 478, 479, 480,
                    481, 482, 483, 484, 485, 486, 487, 488, 489, 490,
                    491, 492, 493, 494, 495, 496, 497, 498, 499, 500
                        );
            }
            else
            {
                SmallFrameSize();
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            SmallFrameSize(); // Make sure this is JITted first, so the call from Test1() is not to the prestub.

            Test1(false);

            if (iret == 100)
            {
                Console.WriteLine("TEST PASSED");
            }
            else
            {
                Console.WriteLine("TEST FAILED");
            }
            return iret;
        }
    }
}
