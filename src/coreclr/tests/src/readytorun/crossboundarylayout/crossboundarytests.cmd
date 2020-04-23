echo off
setlocal 
set TESTDIR=%~dp0\..\..\..\..\..\..\artifacts\tests\coreclr\Windows_NT.x64.Debug\readytorun\crossboundarylayout\crossboundarytest\crossboundarytest
set TESTBATCHROOT=%~dp0

call :testCompositeScenarios
call :testCG2SingleInputBubbleAll
call :testCG2All
call :testCG1All

goto done

:testCompositeScenarios

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% all a b crossboundarytest d

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% ad a d

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% abd a b d

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% a_crossboundarytest_d a crossboundarytest d

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% a_b_crossboundarytest a b crossboundarytest

goto done

:testCG1All

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A___ a CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__B__ b CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_AB__ a CG1Single b CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1___C_ crossboundarytest CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A_C_ a CG1Single crossboundarytest CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__BC_ b CG1Single crossboundarytest CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_ABC_ a CG1Single b CG1Single crossboundarytest CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1____D d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A__D a CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__B_D b CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_AB_D a CG1Single b CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1___CD crossboundarytest CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_A_CD a CG1Single crossboundarytest CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1__BCD b CG1Single crossboundarytest CG1Single d CG1Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg1_ABCD a CG1Single b CG1Single crossboundarytest CG1Single d CG1Single

goto done

:testCG2SingleInputBubbleAll

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A___ a CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__B__ b CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_AB__ a CG2SingleInputBubble b CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble___C_ crossboundarytest CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A_C_ a CG2SingleInputBubble crossboundarytest CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__BC_ b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_ABC_ a CG2SingleInputBubble b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble____D d CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A__D a CG2SingleInputBubble d CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__B_D b CG2SingleInputBubble d CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_AB_D a CG2SingleInputBubble b CG2SingleInputBubble d CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble___CD crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_A_CD a CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble__BCD b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2bubble_ABCD a CG2SingleInputBubble b CG2SingleInputBubble crossboundarytest CG2SingleInputBubble d CG2SingleInputBubble

goto done

:testCG2All

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A___ a CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__B__ b CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_AB__ a CG2Single b CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2___C_ crossboundarytest CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A_C_ a CG2Single crossboundarytest CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__BC_ b CG2Single crossboundarytest CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_ABC_ a CG2Single b CG2Single crossboundarytest CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2____D d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A__D a CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__B_D b CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_AB_D a CG2Single b CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2___CD crossboundarytest CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_A_CD a CG2Single crossboundarytest CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2__BCD b CG2Single crossboundarytest CG2Single d CG2Single
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %TESTDIR% cg2_ABCD a CG2Single b CG2Single crossboundarytest CG2Single d CG2Single

goto done

:done
 