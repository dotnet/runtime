// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly Test1 {}
.assembly extern mscorlib{auto}
.class FullProof {
.method static int32 Test() {
.maxstack 500
IL_1:    ldc.i4 -2147483648			//i
IL_2:     ldc.i4 -2147483648			//ii
IL_3:     nop			//ii
IL_4:    rem.un			//i
IL_5:     ldc.r4 55.5			//ri
IL_6:      ldc.r8 -0.0			//rri
IL_7:     pop			//ri
IL_8:     conv.ovf.i8			//Ii
IL_9:      ldnull			//oIi
IL_10:      nop			//oIi
IL_11:     pop			//Ii
IL_12:     conv.i2			//ii
IL_13:      ldc.r8 -0.0			//rii
IL_14:      nop			//rii
IL_15:      conv.ovf.i1.un			//iii
IL_16:      nop			//iii
IL_17:     sub.ovf			//ii
IL_18:      ldc.r8 -0.0			//rii
IL_19:       ldc.r4 55.5			//rrii
IL_20:      pop			//rii
IL_21:      conv.ovf.u8.un			//Iii
IL_22:       ldc.i8 100			//IIii
IL_23:     beq IL_336			//ii
IL_24:    clt			//i
IL_25:     ldc.i4.s 1			//ii
IL_26:     nop			//ii
IL_27:     conv.ovf.i8.un			//Ii
IL_28:      ldc.i4 0			//iIi
IL_29:      conv.ovf.i8.un			//IIi
IL_30:       ldc.i4 -2147483648			//iIIi
IL_31:        ldc.i4 0			//iiIIi
IL_32:         ldc.i8 1			//IiiIIi
IL_33:        pop			//iiIIi
IL_34:       and			//iIIi
IL_35:        ldc.i8 -9223372036854775808			//IiIIi
IL_36:         ldc.i8 -9223372036854775808			//IIiIIi
IL_37:         nop			//IIiIIi
IL_38:       beq IL_416			//iIIi
IL_39:      shl			//IIi
IL_40:      nop			//IIi
IL_41:     clt			//ii
IL_42:     nop			//ii
IL_43:      ldc.i8 100			//Iii
IL_44:       ldc.i8 -1			//IIii
IL_45:       nop			//IIii
IL_46:     beq IL_336			//ii
IL_47:     conv.u4			//ii
IL_48:      ldc.i4.s 1			//iii
IL_49:      nop			//iii
IL_50:      conv.u2			//iii
IL_51:       ldc.i4 0			//iiii
IL_52:        ldc.r8 3.402823E+38			//riiii
IL_53:        nop			//riiii
IL_54:        conv.ovf.u8.un			//Iiiii
IL_55:         ldc.r8 -55.5			//rIiiii
IL_56:         nop			//rIiiii
IL_57:         conv.ovf.u2			//iIiiii
IL_58:         nop			//iIiiii
IL_59:        shl			//Iiiii
IL_60:       pop			//iiii
IL_61:      shr			//iii
IL_62:       ldc.i4.s 0			//iiii
IL_63:        ldc.i4 -2147483648			//iiiii
IL_64:        nop			//iiiii
IL_65:       add.ovf			//iiii
IL_66:        ldc.i4 2147483647			//iiiii
IL_67:       div			//iiii
IL_68:       nop			//iiii
IL_69:       conv.u2			//iiii
IL_70:      shr.un			//iii
IL_71:       ldc.i8 9223372036854775807			//Iiii
IL_72:        ldc.i4 2147483647			//iIiii
IL_73:         ldc.r8 -0.0			//riIiii
IL_74:          ldc.r8 55.5			//rriIiii
IL_75:           ldc.i4.s 1			//irriIiii
IL_76:            ldc.i4 100			//iirriIiii
IL_77:            nop			//iirriIiii
IL_78:          beq IL_93			//rriIiii
IL_79:         ceq			//iiIiii
IL_80:        shr.un			//iIiii
IL_81:         ldc.i8 1			//IiIiii
IL_82:         nop			//IiIiii
IL_83:         conv.r4			//riIiii
IL_84:          ldc.r8 -55.5			//rriIiii
IL_85:           ldc.r8 55.5			//rrriIiii
IL_86:           nop			//rrriIiii
IL_87:          div			//rriIiii
IL_88:           ldc.r4 55.5			//rrriIiii
IL_89:            ldc.r4 -55.5			//rrrriIiii
IL_90:             ldc.r8 3.402823E+38			//rrrrriIiii
IL_91:           beq IL_94			//rrriIiii
IL_92:          mul			//rriIiii
IL_93:           ldc.r4 3.402823E+38			//rrriIiii
IL_94:         beq IL_95			//riIiii
IL_95:         conv.u8			//IiIiii
IL_96:          ldc.i8 -1			//IIiIiii
IL_97:          nop			//IIiIiii
IL_98:           ldc.r8 -3.402823E+38			//rIIiIiii
IL_99:           conv.i8			//IIIiIiii
IL_100:           conv.ovf.u8.un			//IIIiIiii
IL_101:          sub			//IIiIiii
IL_102:           ldc.i8 100			//IIIiIiii
IL_103:          pop			//IIiIiii
IL_104:        beq IL_139			//iIiii
IL_105:       shr.un			//Iiii
IL_106:        ldc.i4 -2147483648			//iIiii
IL_107:         ldc.i4 -1			//iiIiii
IL_108:        clt			//iIiii
IL_109:        conv.r.un			//rIiii
IL_110:         ldc.r8 -0.0			//rrIiii
IL_111:          ldc.i4.s 1			//irrIiii
IL_112:          nop			//irrIiii
IL_113:          conv.r4			//rrrIiii
IL_114:         rem			//rrIiii
IL_115:       beq IL_289			//Iiii
IL_116:       conv.ovf.i4.un			//iiii
IL_117:      shl			//iii
IL_118:     shl			//ii
IL_119:      ldc.i4 -100			//iii
IL_120:     pop			//ii
IL_121:    xor			//i
IL_122:     ldc.r4 0.0			//ri
IL_123:     conv.r.un			//ri
IL_124:      newobj     instance void [mscorlib]System.Object::.ctor()			//ori
IL_125:     pop			//ri
IL_126:     conv.r.un			//ri
IL_127:     nop			//ri
IL_128:     conv.u1			//ii
IL_129:      ldc.i4 -1			//iii
IL_130:       ldc.i4 -1			//iiii
IL_131:        ldc.i4.s 1			//iiiii
IL_132:        nop			//iiiii
IL_133:       cgt			//iiii
IL_134:      add			//iii
IL_135:      conv.ovf.i2			//iii
IL_136:       ldc.i8 -1			//Iiii
IL_137:        ldc.i4.s 0			//iIiii
IL_138:        nop			//iIiii
IL_139:       pop			//Iiii
IL_140:       conv.i8			//Iiii
IL_141:       conv.ovf.u8			//Iiii
IL_142:       conv.i2			//iiii
IL_143:      shl			//iii
IL_144:       ldc.i4 -2147483648			//iiii
IL_145:        ldc.i8 -9223372036854775808			//Iiiii
IL_146:         ldc.i8 1			//IIiiii
IL_147:        cgt			//iiiii
IL_148:        conv.u2			//iiiii
IL_149:        nop			//iiiii
IL_150:       cgt.un			//iiii
IL_151:       nop			//iiii
IL_152:        ldc.i4 -100			//iiiii
IL_153:         ldc.i4 -100			//iiiiii
IL_154:        div.un			//iiiii
IL_155:         ldc.i4.s 0			//iiiiii
IL_156:          ldc.i4 -1			//iiiiiii
IL_157:         shr.un			//iiiiii
IL_158:        rem			//iiiii
IL_159:         ldnull			//oiiiii
IL_160:          ldc.i8 100			//Ioiiiii
IL_161:          conv.ovf.u8.un			//Ioiiiii
IL_162:           ldc.r4 55.5			//rIoiiiii
IL_163:            ldc.i8 -9223372036854775808			//IrIoiiiii
IL_164:           pop			//rIoiiiii
IL_165:           conv.ovf.u8			//IIoiiiii
IL_166:         beq IL_167			//oiiiii
IL_167:        pop			//iiiii
IL_168:       pop			//iiii
IL_169:      xor			//iii
IL_170:     sub.ovf.un			//ii
IL_171:      ldc.r4 -3.402823E+38			//rii
IL_172:      conv.r.un			//rii
IL_173:       ldc.i4 -100			//irii
IL_174:        ldc.i4 0			//iirii
IL_175:       shr			//irii
IL_176:       conv.r.un			//rrii
IL_177:      mul			//rii
IL_178:      conv.ovf.u4			//iii
IL_179:      nop			//iii
IL_180:       ldc.i8 100			//Iiii
IL_181:       conv.ovf.i1			//iiii
IL_182:      sub.ovf			//iii
IL_183:       ldc.i8 -1			//Iiii
IL_184:        ldc.i8 0			//IIiii
IL_185:       mul			//Iiii
IL_186:        ldc.i8 0			//IIiii
IL_187:         ldc.r4 55.5			//rIIiii
IL_188:        pop			//IIiii
IL_189:      beq IL_315			//iii
IL_190:     mul.ovf			//ii
IL_191:      ldc.i8 0			//Iii
IL_192:      nop			//Iii
IL_193:      conv.ovf.i1			//iii
IL_194:     div			//ii
IL_195:    add.ovf.un			//i
IL_196:     ldc.r4 -0.0			//ri
IL_197:     conv.r4			//ri
IL_198:     conv.r.un			//ri
IL_199:      ldc.r8 55.5			//rri
IL_200:      nop			//rri
IL_201:     clt.un			//ii
IL_202:      ldc.i4 100			//iii
IL_203:     pop			//ii
IL_204:      ldc.i4 0			//iii
IL_205:     shr			//ii
IL_206:      ldc.i8 0			//Iii
IL_207:      nop			//Iii
IL_208:      conv.ovf.i8.un			//Iii
IL_209:       ldc.i4 -1			//iIii
IL_210:      shr			//Iii
IL_211:      nop			//Iii
IL_212:     pop			//ii
IL_213:      ldc.i4.s 0			//iii
IL_214:      conv.ovf.i8			//Iii
IL_215:      conv.ovf.u1.un			//iii
IL_216:       ldc.i4 -1			//iiii
IL_217:        ldc.r4 -0.0			//riiii
IL_218:        conv.r.un			//riiii
IL_219:        conv.ovf.u2.un			//iiiii
IL_220:        nop			//iiiii
IL_221:       clt			//iiii
IL_222:       conv.ovf.u8			//Iiii
IL_223:        ldc.i8 0			//IIiii
IL_224:       clt.un			//iiii
IL_225:       nop			//iiii
IL_226:      sub.ovf.un			//iii
IL_227:      nop			//iii
IL_228:       ldc.r4 0.0			//riii
IL_229:        ldc.r8 -3.402823E+38			//rriii
IL_230:        nop			//rriii
IL_231:        conv.r8			//rriii
IL_232:       div			//riii
IL_233:        ldc.i8 100			//Iriii
IL_234:        nop			//Iriii
IL_235:        conv.r4			//rriii
IL_236:       add			//riii
IL_237:        ldc.i8 9223372036854775807			//Iriii
IL_238:         ldc.i4 -100			//iIriii
IL_239:          ldc.i8 9223372036854775807			//IiIriii
IL_240:           ldc.i8 1			//IIiIriii
IL_241:           nop			//IIiIriii
IL_242:          clt			//iiIriii
IL_243:         mul.ovf.un			//iIriii
IL_244:        shr			//Iriii
IL_245:       pop			//riii
IL_246:      pop			//iii
IL_247:     shr			//ii
IL_248:      ldc.i4 0			//iii
IL_249:     mul.ovf			//ii
IL_250:      ldc.i4 0			//iii
IL_251:      nop			//iii
IL_252:      conv.ovf.i8			//Iii
IL_253:      conv.ovf.u8.un			//Iii
IL_254:       ldc.i8 -1			//IIii
IL_255:       nop			//IIii
IL_256:       conv.i8			//IIii
IL_257:        ldc.i8 -9223372036854775808			//IIIii
IL_258:         ldc.i8 9223372036854775807			//IIIIii
IL_259:         nop			//IIIIii
IL_260:        mul			//IIIii
IL_261:        nop			//IIIii
IL_262:       pop			//IIii
IL_263:      mul			//Iii
IL_264:       ldc.i4.s 0			//iIii
IL_265:        ldc.i4.s 0			//iiIii
IL_266:       and			//iIii
IL_267:        ldnull			//oiIii
IL_268:         ldnull			//ooiIii
IL_269:          newobj     instance void [mscorlib]System.Object::.ctor()			//oooiIii
IL_270:          nop			//oooiIii
IL_271:         pop			//ooiIii
IL_272:        pop			//oiIii
IL_273:       pop			//iIii
IL_274:       conv.ovf.u2.un			//iIii
IL_275:       conv.r8			//rIii
IL_276:       conv.ovf.i8.un			//IIii
IL_277:        ldc.i4 100			//iIIii
IL_278:         ldc.r8 -3.402823E+38			//riIIii
IL_279:          ldc.i4 -2147483648			//iriIIii
IL_280:           ldc.i4 100			//iiriIIii
IL_281:           nop			//iiriIIii
IL_282:         beq IL_285			//riIIii
IL_283:         conv.r4			//riIIii
IL_284:         nop			//riIIii
IL_285:        pop			//iIIii
IL_286:       pop			//IIii
IL_287:      cgt			//iii
IL_288:       ldc.i8 -9223372036854775808			//Iiii
IL_289:       conv.i2			//iiii
IL_290:        ldc.i4 -2147483648			//iiiii
IL_291:         ldc.i4.s 1			//iiiiii
IL_292:        cgt.un			//iiiii
IL_293:         ldc.r4 -55.5			//riiiii
IL_294:         nop			//riiiii
IL_295:          ldc.i4 -1			//iriiiii
IL_296:          conv.r8			//rriiiii
IL_297:          nop			//rriiiii
IL_298:          nop			//rriiiii
IL_299:        beq IL_334			//iiiii
IL_300:        conv.u1			//iiiii
IL_301:        nop			//iiiii
IL_302:        conv.ovf.u1.un			//iiiii
IL_303:        nop			//iiiii
IL_304:       cgt.un			//iiii
IL_305:        ldc.i4 -100			//iiiii
IL_306:         ldc.i4 -1			//iiiiii
IL_307:          ldc.i8 0			//Iiiiiii
IL_308:          nop			//Iiiiiii
IL_309:         pop			//iiiiii
IL_310:        pop			//iiiii
IL_311:       pop			//iiii
IL_312:      div.un			//iii
IL_313:     div			//ii
IL_314:      ldc.i4 -2147483648			//iii
IL_315:       ldc.i4 100			//iiii
IL_316:        ldc.i4 100			//iiiii
IL_317:        nop			//iiiii
IL_318:       sub			//iiii
IL_319:        ldnull			//oiiii
IL_320:         ldc.i8 -100			//Ioiiii
IL_321:         nop			//Ioiiii
IL_322:        pop			//oiiii
IL_323:       pop			//iiii
IL_324:       conv.i1			//iiii
IL_325:        ldc.r8 -3.402823E+38			//riiii
IL_326:        conv.r.un			//riiii
IL_327:        nop			//riiii
IL_328:       pop			//iiii
IL_329:        ldc.i4 0			//iiiii
IL_330:         ldc.r4 -0.0			//riiiii
IL_331:         nop			//riiiii
IL_332:        pop			//iiiii
IL_333:        nop			//iiiii
IL_334:       pop			//iiii
IL_335:     beq IL_336			//ii
IL_336:   beq IL_337			//
IL_337:    ldc.i4 2147483647			//i
IL_338:     ldc.i8 -100			//Ii
IL_339:      ldc.i4 -1			//iIi
IL_340:       ldc.i4.s 1			//iiIi
IL_341:       nop			//iiIi
IL_342:      mul.ovf.un			//iIi
IL_343:     shr.un			//Ii
IL_344:      ldc.i8 1			//IIi
IL_345:      nop			//IIi
IL_346:     pop			//Ii
IL_347:     conv.ovf.i8			//Ii
IL_348:      ldc.i8 0			//IIi
IL_349:       ldc.i4 -100			//iIIi
IL_350:      shr			//IIi
IL_351:      conv.ovf.u8.un			//IIi
IL_352:       ldc.i8 1			//IIIi
IL_353:       conv.ovf.u8.un			//IIIi
IL_354:        ldc.i8 100			//IIIIi
IL_355:         ldc.i8 1			//IIIIIi
IL_356:         nop			//IIIIIi
IL_357:       beq IL_417			//IIIi
IL_358:      pop			//IIi
IL_359:     sub			//Ii
IL_360:     conv.ovf.i8.un			//Ii
IL_361:      ldc.i8 100			//IIi
IL_362:      nop			//IIi
IL_363:      conv.ovf.u8.un			//IIi
IL_364:     pop			//Ii
IL_365:      ldc.i8 -100			//IIi
IL_366:       ldc.r8 0.0			//rIIi
IL_367:        ldc.r8 0.0			//rrIIi
IL_368:      beq IL_499			//IIi
IL_369:      conv.ovf.u8.un			//IIi
IL_370:       ldc.i4 0			//iIIi
IL_371:        ldc.i4 0			//iiIIi
IL_372:       shl			//iIIi
IL_373:        ldc.i8 -1			//IiIIi
IL_374:        conv.ovf.i8			//IiIIi
IL_375:         ldc.i4 0			//iIiIIi
IL_376:        shr			//IiIIi
IL_377:         ldc.i4.s 1			//iIiIIi
IL_378:          ldc.i4.s 1			//iiIiIIi
IL_379:           ldc.i8 1			//IiiIiIIi
IL_380:            ldc.i8 -9223372036854775808			//IIiiIiIIi
IL_381:            nop			//IIiiIiIIi
IL_382:          beq IL_383			//iiIiIIi
IL_383:         cgt.un			//iIiIIi
IL_384:         nop			//iIiIIi
IL_385:         conv.i8			//IIiIIi
IL_386:          ldc.i4 -1			//iIIiIIi
IL_387:           ldc.i4 -1			//iiIIiIIi
IL_388:           conv.r.un			//riIIiIIi
IL_389:           nop			//riIIiIIi
IL_390:           conv.ovf.i4			//iiIIiIIi
IL_391:            ldc.r4 -3.402823E+38			//riiIIiIIi
IL_392:            nop			//riiIIiIIi
IL_393:           pop			//iiIIiIIi
IL_394:           nop			//iiIIiIIi
IL_395:          sub.ovf			//iIIiIIi
IL_396:           ldc.r4 0.0			//riIIiIIi
IL_397:           nop			//riIIiIIi
IL_398:            ldc.r4 0.0			//rriIIiIIi
IL_399:            nop			//rriIIiIIi
IL_400:          beq IL_410			//iIIiIIi
IL_401:          conv.u1			//iIIiIIi
IL_402:         shl			//IIiIIi
IL_403:          ldc.i4 -100			//iIIiIIi
IL_404:           ldc.i8 9223372036854775807			//IiIIiIIi
IL_405:           conv.ovf.i8			//IiIIiIIi
IL_406:            ldc.i4 2147483647			//iIiIIiIIi
IL_407:           pop			//IiIIiIIi
IL_408:           conv.u2			//iiIIiIIi
IL_409:          shr			//iIIiIIi
IL_410:           ldc.i4 -1			//iiIIiIIi
IL_411:            ldc.i8 9223372036854775807			//IiiIIiIIi
IL_412:            conv.ovf.i2			//iiiIIiIIi
IL_413:           shr			//iiIIiIIi
IL_414:         beq IL_415			//IIiIIi
IL_415:       beq IL_416			//iIIi
IL_416:       conv.ovf.i8			//IIIi
IL_417:        ldc.i8 1			//IIIIi
IL_418:         ldc.i8 0			//IIIIIi
IL_419:         nop			//IIIIIi
IL_420:         conv.u4			//iIIIIi
IL_421:        shr.un			//IIIIi
IL_422:         ldc.i8 9223372036854775807			//IIIIIi
IL_423:        sub			//IIIIi
IL_424:         ldc.i8 -100			//IIIIIi
IL_425:          ldc.i8 0			//IIIIIIi
IL_426:          nop			//IIIIIIi
IL_427:        beq IL_492			//IIIIi
IL_428:        conv.ovf.u8.un			//IIIIi
IL_429:         ldc.r8 -55.5			//rIIIIi
IL_430:          ldc.r8 -0.0			//rrIIIIi
IL_431:          nop			//rrIIIIi
IL_432:          nop			//rrIIIIi
IL_433:         sub			//rIIIIi
IL_434:         conv.r4			//rIIIIi
IL_435:         nop			//rIIIIi
IL_436:          ldc.r4 55.5			//rrIIIIi
IL_437:           ldc.r4 55.5			//rrrIIIIi
IL_438:            ldc.r8 0.0			//rrrrIIIIi
IL_439:            nop			//rrrrIIIIi
IL_440:          beq IL_457			//rrIIIIi
IL_441:          conv.r8			//rrIIIIi
IL_442:           ldc.r4 0.0			//rrrIIIIi
IL_443:            newobj     instance void [mscorlib]System.Object::.ctor()			//orrrIIIIi
IL_444:            nop			//orrrIIIIi
IL_445:           pop			//rrrIIIIi
IL_446:           conv.r8			//rrrIIIIi
IL_447:            ldc.i4 2147483647			//irrrIIIIi
IL_448:           pop			//rrrIIIIi
IL_449:          div			//rrIIIIi
IL_450:          nop			//rrIIIIi
IL_451:         pop			//rIIIIi
IL_452:        pop			//IIIIi
IL_453:        conv.ovf.u8			//IIIIi
IL_454:         ldc.r8 -55.5			//rIIIIi
IL_455:          ldc.r4 -0.0			//rrIIIIi
IL_456:          nop			//rrIIIIi
IL_457:         add			//rIIIIi
IL_458:          ldc.i8 100			//IrIIIIi
IL_459:           ldc.i8 100			//IIrIIIIi
IL_460:            ldc.i8 -100			//IIIrIIIIi
IL_461:           add			//IIrIIIIi
IL_462:           nop			//IIrIIIIi
IL_463:          mul			//IrIIIIi
IL_464:           ldc.i4.s 1			//iIrIIIIi
IL_465:            ldc.i4 2147483647			//iiIrIIIIi
IL_466:           sub.ovf.un			//iIrIIIIi
IL_467:           conv.ovf.u8.un			//IIrIIIIi
IL_468:            ldc.i4 -100			//iIIrIIIIi
IL_469:            conv.ovf.u8			//IIIrIIIIi
IL_470:             ldc.i8 100			//IIIIrIIIIi
IL_471:              ldc.i4 2147483647			//iIIIIrIIIIi
IL_472:               ldc.i4 -1			//iiIIIIrIIIIi
IL_473:                ldc.i8 -1			//IiiIIIIrIIIIi
IL_474:               pop			//iiIIIIrIIIIi
IL_475:              sub.ovf.un			//iIIIIrIIIIi
IL_476:               ldc.i4 0			//iiIIIIrIIIIi
IL_477:               conv.ovf.u2			//iiIIIIrIIIIi
IL_478:                newobj     instance void [mscorlib]System.Object::.ctor()			//oiiIIIIrIIIIi
IL_479:                nop			//oiiIIIIrIIIIi
IL_480:               pop			//iiIIIIrIIIIi
IL_481:               nop			//iiIIIIrIIIIi
IL_482:             beq IL_483			//IIIIrIIIIi
IL_483:            pop			//IIIrIIIIi
IL_484:          beq IL_486			//IrIIIIi
IL_485:          conv.u8			//IrIIIIi
IL_486:           ldc.i8 9223372036854775807			//IIrIIIIi
IL_487:         beq IL_490			//rIIIIi
IL_488:         conv.r.un			//rIIIIi
IL_489:         nop			//rIIIIi
IL_490:        pop			//IIIIi
IL_491:        conv.ovf.i8.un			//IIIIi
IL_492:      beq IL_499			//IIi
IL_493:       ldc.r8 3.402823E+38			//rIIi
IL_494:        ldnull			//orIIi
IL_495:       pop			//rIIi
IL_496:        ldc.r8 55.5			//rrIIi
IL_497:        nop			//rrIIi
IL_498:      beq IL_499			//IIi
IL_499:    beq IL_500			//i
IL_500:   ret			//
}

.method public static int32  Main()
{
.entrypoint
.maxstack 3
   .try
   {
      call int32 FullProof::Test()
      pop
      leave.s AfterEH
   }
   catch [mscorlib]System.Exception 
   {
      call       void [System.Console]System.Console::WriteLine(object)
      leave.s AfterEH
   }

AfterEH:

   ldstr      "PASS"
   call       void [System.Console]System.Console::WriteLine(string)
   ldc.i4.s   100

   ret
}
}
