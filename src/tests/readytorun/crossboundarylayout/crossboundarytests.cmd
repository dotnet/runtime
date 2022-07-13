echo off
setlocal
set TESTDIR=%~dp0\..\..\..\..\..\..\artifacts\tests\coreclr\windows.x64.Debug\readytorun\crossboundarylayout\crossboundarytest\crossboundarytest
set TESTBATCHROOT=%~dp0

call :testSpecificCompositeScenarios
call :testAllCombinationsOfCompositeAndR2R
call :testCG2SingleInputBubbleCompiledWithoutReferenceToBCE
call :testCG2SingleMixedInputBubble
call :testCG2SingleInputBubbleAll
call :testCG2All
call :testCG1All

goto done

:testSpecificCompositeScenarios
echo Test some of the interesting composite scenarios first

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C2D2E1 a e  CG2Composite b crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% all a b crossboundarytest d
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% ad a d
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% abd a b d
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% a_crossboundarytest_d a crossboundarytest d
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% a_b_crossboundarytest a b crossboundarytest

goto done

:testAllCombinationsOfCompositeAndR2R
echo testing all possible combinations of 1 regular R2R image mixed with the rest of the files being in a single composite image or alone
echo Also test all possible arrangements of 2 composite images
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C0D0E0 a b  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C1D0E0 a crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C1D0E0 b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C1D0E0 a b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C1D0E0 b crossboundarytest  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C1D0E0 a crossboundarytest  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C1D0E0 crossboundarytest  CG2Single a b  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C0D1E0 a d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C0D1E0 b d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C0D1E0 a b d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C0D1E0 b d  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C0D1E0 a d  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C0D1E0 d  CG2Single a b  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B0C1D1E0 crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C1D1E0 a crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B0C1D1E0 crossboundarytest d  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C1D1E0 b crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C1D1E0 a b crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C1D1E0 b crossboundarytest d  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B2C1D1E0 crossboundarytest d  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C1D1E0 a crossboundarytest d  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C1D1E0 crossboundarytest d  CG2Composite a b  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C2D1E0 a d  CG2Composite crossboundarytest  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B0C2D1E0 d  CG2Single a crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C2D1E0 b d  CG2Composite crossboundarytest  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C2D1E0 a b d  CG2Composite crossboundarytest  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C2D1E0 b d  CG2Composite a crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B2C2D1E0 d  CG2Single b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C2D1E0 a d  CG2Composite b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C2D1E0 d  CG2Single a b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C0D0E1 a e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C0D0E1 b e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C0D0E1 a b e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C0D0E1 b e  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C0D0E1 a e  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C0D0E1 e  CG2Single a b  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B0C1D0E1 crossboundarytest e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C1D0E1 a crossboundarytest e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B0C1D0E1 crossboundarytest e  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C1D0E1 b crossboundarytest e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C1D0E1 a b crossboundarytest e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C1D0E1 b crossboundarytest e  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B2C1D0E1 crossboundarytest e  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C1D0E1 a crossboundarytest e  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C1D0E1 crossboundarytest e  CG2Composite a b  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C2D0E1 a e  CG2Composite crossboundarytest  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B0C2D0E1 e  CG2Single a crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C2D0E1 b e  CG2Composite crossboundarytest  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C2D0E1 a b e  CG2Composite crossboundarytest  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C2D0E1 b e  CG2Composite a crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B2C2D0E1 e  CG2Single b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C2D0E1 a e  CG2Composite b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C2D0E1 e  CG2Single a b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B0C0D1E1 d e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C0D1E1 a d e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B0C0D1E1 d e  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C0D1E1 b d e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C0D1E1 a b d e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C0D1E1 b d e  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B2C0D1E1 d e  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C0D1E1 a d e  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C0D1E1 d e  CG2Composite a b  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B0C1D1E1 crossboundarytest d e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C1D1E1 a crossboundarytest d e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B0C1D1E1 crossboundarytest d e  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C1D1E1 b crossboundarytest d e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C1D1E1 a b crossboundarytest d e  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C1D1E1 b crossboundarytest d e  CG2Composite a  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B2C1D1E1 crossboundarytest d e  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C1D1E1 a crossboundarytest d e  CG2Composite b  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C1D1E1 crossboundarytest d e  CG2Composite a b  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B0C2D1E1 d e  CG2Composite crossboundarytest  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C2D1E1 a d e  CG2Composite crossboundarytest  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B0C2D1E1 d e  CG2Composite a crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C2D1E1 b d e  CG2Composite crossboundarytest  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C2D1E1 a b d e  CG2Composite crossboundarytest  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C2D1E1 b d e  CG2Composite a crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B2C2D1E1 d e  CG2Composite b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C2D1E1 a d e  CG2Composite b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C2D1E1 d e  CG2Composite a b crossboundarytest  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C0D2E1 a e  CG2Composite d  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B0C0D2E1 e  CG2Single a d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C0D2E1 b e  CG2Composite d  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C0D2E1 a b e  CG2Composite d  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C0D2E1 b e  CG2Composite a d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B2C0D2E1 e  CG2Single b d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C0D2E1 a e  CG2Composite b d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C0D2E1 e  CG2Single a b d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B0C1D2E1 crossboundarytest e  CG2Composite d  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C1D2E1 a crossboundarytest e  CG2Composite d  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B0C1D2E1 crossboundarytest e  CG2Composite a d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C1D2E1 b crossboundarytest e  CG2Composite d  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C1D2E1 a b crossboundarytest e  CG2Composite d  CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C1D2E1 b crossboundarytest e  CG2Composite a d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B2C1D2E1 crossboundarytest e  CG2Composite b d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C1D2E1 a crossboundarytest e  CG2Composite b d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C1D2E1 crossboundarytest e  CG2Composite a b d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B0C2D2E1 e  CG2Single crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B0C2D2E1 a e  CG2Composite crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B0C2D2E1 e  CG2Single a crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B1C2D2E1 b e  CG2Composite crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B1C2D2E1 a b e  CG2Composite crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B1C2D2E1 b e  CG2Composite a crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA0B2C2D2E1 e  CG2Single b crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA1B2C2D2E1 a e  CG2Composite b crossboundarytest d  CG2Composite
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% compositeA2B2C2D2E1 e  CG2Single a b crossboundarytest d  CG2Composite

goto done

:testCG2SingleInputBubbleAll

echo TEST All combinations of the 5 dlls compiled with Crossgen2 with input bubble enabled and all assemblies passed as reference inputs to crossgen2
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A____ a CG2SingleInputBubble d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__B___ b CG2SingleInputBubble a CG2NoMethods  d CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_AB___ a CG2SingleInputBubble b CG2SingleInputBubble d CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble___C__ crossboundarytest CG2SingleInputBubble a CG2NoMethods d CG2NoMethods b CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A_C__ a CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2NoMethods b CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__BC__ b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble a CG2NoMethods d CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_ABC__ a CG2SingleInputBubble b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble____D_ d CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A__D_ a CG2SingleInputBubble d CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__B_D_ b CG2SingleInputBubble d CG2SingleInputBubble a CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_AB_D_ a CG2SingleInputBubble b CG2SingleInputBubble d CG2SingleInputBubble e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble___CD_ crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble a CG2NoMethods b CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A_CD_ a CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble b CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__BCD_ b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble a CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_ABCD_ a CG2SingleInputBubble b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_____E e CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A___E a CG2SingleInputBubble e CG2SingleInputBubble d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__B__E b CG2SingleInputBubble e CG2SingleInputBubble a CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_AB__E a CG2SingleInputBubble b CG2SingleInputBubble e CG2SingleInputBubble d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble___C_E crossboundarytest CG2SingleInputBubble e CG2SingleInputBubble a CG2NoMethods b CG2NoMethods d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A_C_E a CG2SingleInputBubble crossboundarytest CG2SingleInputBubble e CG2SingleInputBubble d CG2NoMethods b CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__BC_E b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble e CG2SingleInputBubble a CG2NoMethods d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_ABC_E a CG2SingleInputBubble b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble e CG2SingleInputBubble d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble____DE d CG2SingleInputBubble e CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A__DE a CG2SingleInputBubble d CG2SingleInputBubble e CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__B_DE b CG2SingleInputBubble d CG2SingleInputBubble e CG2SingleInputBubble a CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_AB_DE a CG2SingleInputBubble b CG2SingleInputBubble d CG2SingleInputBubble e CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble___CDE crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble e CG2SingleInputBubble a CG2NoMethods b CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A_CDE a CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble e CG2SingleInputBubble b CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__BCDE b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble e CG2SingleInputBubble a CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_ABCDE a CG2SingleInputBubble b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble e CG2SingleInputBubble

goto done

:testCG2SingleInputBubbleCompiledWithoutReferenceToBCE
echo TEST All combinations of the 5 dlls compiled with Crossgen2 with input bubble enabled and all assemblies passed as
echo reference inputs to crossgen2 when compiled b, crossboundarytest and e. a, and d are compiled with the reference
echo set limited to a and d. This simulates a the model of two different sets of input bubble matched assemblies where there is a
echo root set such as the runtime repo worth of libraries, and a separately compiled application set.

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_A____ a CG2SingleBubbleADOnly d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2__B___ b CG2SingleInputBubble  a CG2NoMethods  d CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_AB___ a CG2SingleBubbleADOnly b CG2SingleInputBubble d CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2___C__ crossboundarytest CG2SingleInputBubble a CG2NoMethods d CG2NoMethods b CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_A_C__ a CG2SingleBubbleADOnly crossboundarytest CG2SingleInputBubble d CG2NoMethods b CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2__BC__ b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble a CG2NoMethods d CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_ABC__ a CG2SingleBubbleADOnly b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2____D_ d CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_A__D_ a CG2SingleBubbleADOnly d CG2SingleBubbleADOnly
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2__B_D_ b CG2SingleInputBubble d CG2SingleBubbleADOnly a CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_AB_D_ a CG2SingleBubbleADOnly b CG2SingleInputBubble d CG2SingleBubbleADOnly e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2___CD_ crossboundarytest CG2SingleInputBubble d CG2SingleBubbleADOnly a CG2NoMethods b CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_A_CD_ a CG2SingleBubbleADOnly crossboundarytest CG2SingleInputBubble d CG2SingleBubbleADOnly b CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2__BCD_ b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleBubbleADOnly a CG2NoMethods e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_ABCD_ a CG2SingleBubbleADOnly b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleBubbleADOnly e CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_____E e CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_A___E a CG2SingleBubbleADOnly e CG2SingleInputBubble d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2__B__E b CG2SingleInputBubble e CG2SingleInputBubble a CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_AB__E a CG2SingleBubbleADOnly b CG2SingleInputBubble e CG2SingleInputBubble d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2___C_E crossboundarytest CG2SingleInputBubble e CG2SingleInputBubble a CG2NoMethods b CG2NoMethods d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_A_C_E a CG2SingleBubbleADOnly crossboundarytest CG2SingleInputBubble e CG2SingleInputBubble b CG2NoMethods d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2__BC_E b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble e CG2SingleInputBubble d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_ABC_E a CG2SingleBubbleADOnly b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble e CG2SingleInputBubble d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2____DE d CG2SingleBubbleADOnly e CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_A__DE a CG2SingleBubbleADOnly d CG2SingleBubbleADOnly e CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2__B_DE b CG2SingleInputBubble d CG2SingleBubbleADOnly e CG2SingleInputBubble a CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_AB_DE a CG2SingleBubbleADOnly b CG2SingleInputBubble d CG2SingleBubbleADOnly e CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2___CDE crossboundarytest CG2SingleInputBubble d CG2SingleBubbleADOnly e CG2SingleInputBubble a CG2NoMethods b CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_A_CDE a CG2SingleBubbleADOnly crossboundarytest CG2SingleInputBubble d CG2SingleBubbleADOnly e CG2SingleInputBubble b CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2__BCDE b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleBubbleADOnly e CG2SingleInputBubble a CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble2_ABCDE a CG2SingleBubbleADOnly b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleBubbleADOnly e CG2SingleInputBubble

goto done

:testCG2SingleMixedInputBubble
echo TEST All combinations of the 5 dlls compiled with Crossgen2 with input bubble enabled for the root set and not
echo for the more derived set of assemblies. b, crossboundarytest, and e are compiled as standard R2R and
echo a, and d are compiled with the reference echo set limited to a and d with input bubble enabled. This simulates a the model
echo of a framework that ships with input bubble enabled, and the application with standard R2R rules.

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_A____ a CG2SingleBubbleADOnly d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_AB___ a CG2SingleBubbleADOnly b CG2Single d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_A_C__ a CG2SingleBubbleADOnly crossboundarytest CG2Single d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_ABC__ a CG2SingleBubbleADOnly b CG2Single crossboundarytest CG2Single d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3____D_ d CG2SingleBubbleADOnly
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_A__D_ a CG2SingleBubbleADOnly d CG2SingleBubbleADOnly
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3__B_D_ b CG2Single d CG2SingleBubbleADOnly
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_AB_D_ a CG2SingleBubbleADOnly b CG2Single d CG2SingleBubbleADOnly
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3___CD_ crossboundarytest CG2Single d CG2SingleBubbleADOnly
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_A_CD_ a CG2SingleBubbleADOnly crossboundarytest CG2Single d CG2SingleBubbleADOnly
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3__BCD_ b CG2Single crossboundarytest CG2Single d CG2SingleBubbleADOnly
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_ABCD_ a CG2SingleBubbleADOnly b CG2Single crossboundarytest CG2Single d CG2SingleBubbleADOnly
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_A___E a CG2SingleBubbleADOnly e CG2Single d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_AB__E a CG2SingleBubbleADOnly b CG2Single e CG2Single d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_A_C_E a CG2SingleBubbleADOnly crossboundarytest CG2Single e CG2Single d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_ABC_E a CG2SingleBubbleADOnly b CG2Single crossboundarytest CG2Single e CG2Single d CG2NoMethods
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3____DE d CG2SingleBubbleADOnly e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_A__DE a CG2SingleBubbleADOnly d CG2SingleBubbleADOnly e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3__B_DE b CG2Single d CG2SingleBubbleADOnly e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_AB_DE a CG2SingleBubbleADOnly b CG2Single d CG2SingleBubbleADOnly e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3___CDE crossboundarytest CG2Single d CG2SingleBubbleADOnly e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_A_CDE a CG2SingleBubbleADOnly crossboundarytest CG2Single d CG2SingleBubbleADOnly e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3__BCDE b CG2Single crossboundarytest CG2Single d CG2SingleBubbleADOnly e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble3_ABCDE a CG2SingleBubbleADOnly b CG2Single crossboundarytest CG2Single d CG2SingleBubbleADOnly e CG2Single

goto done


:testCG2All
echo TEST All combinations of compiling standard R2R with the crossgen2 tool instad of crossgen1
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A____ a CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__B___ b CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_AB___ a CG2Single b CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2___C__ crossboundarytest CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A_C__ a CG2Single crossboundarytest CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__BC__ b CG2Single crossboundarytest CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_ABC__ a CG2Single b CG2Single crossboundarytest CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2____D_ d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A__D_ a CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__B_D_ b CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_AB_D_ a CG2Single b CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2___CD_ crossboundarytest CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A_CD_ a CG2Single crossboundarytest CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__BCD_ b CG2Single crossboundarytest CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_ABCD_ a CG2Single b CG2Single crossboundarytest CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_____E e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A___E a CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__B__E b CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_AB__E a CG2Single b CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2___C_E crossboundarytest CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A_C_E a CG2Single crossboundarytest CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__BC_E b CG2Single crossboundarytest CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_ABC_E a CG2Single b CG2Single crossboundarytest CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2____DE d CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A__DE a CG2Single d CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__B_DE b CG2Single d CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_AB_DE a CG2Single b CG2Single d CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2___CDE crossboundarytest CG2Single d CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A_CDE a CG2Single crossboundarytest CG2Single d CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__BCDE b CG2Single crossboundarytest CG2Single d CG2Single e CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_ABCDE a CG2Single b CG2Single crossboundarytest CG2Single d CG2Single e CG2Single

:testCG1All
echo TEST All combinations of compiling standard R2R with the crossgen tool instad of crossgen2
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A____ a CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__B___ b CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_AB___ a CG1Single b CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1___C__ crossboundarytest CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A_C__ a CG1Single crossboundarytest CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__BC__ b CG1Single crossboundarytest CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_ABC__ a CG1Single b CG1Single crossboundarytest CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1____D_ d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A__D_ a CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__B_D_ b CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_AB_D_ a CG1Single b CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1___CD_ crossboundarytest CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A_CD_ a CG1Single crossboundarytest CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__BCD_ b CG1Single crossboundarytest CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_ABCD_ a CG1Single b CG1Single crossboundarytest CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_____E e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A___E a CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__B__E b CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_AB__E a CG1Single b CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1___C_E crossboundarytest CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A_C_E a CG1Single crossboundarytest CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__BC_E b CG1Single crossboundarytest CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_ABC_E a CG1Single b CG1Single crossboundarytest CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1____DE d CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A__DE a CG1Single d CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__B_DE b CG1Single d CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_AB_DE a CG1Single b CG1Single d CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1___CDE crossboundarytest CG1Single d CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A_CDE a CG1Single crossboundarytest CG1Single d CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__BCDE b CG1Single crossboundarytest CG1Single d CG1Single e CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_ABCDE a CG1Single b CG1Single crossboundarytest CG1Single d CG1Single e CG1Single


goto done

:done
