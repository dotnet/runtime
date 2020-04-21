setlocal 
set TESTBATCHROOT=%~dp0
call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %1 all a b crossboundarytest d

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %1 ad a d

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %1 abd a b d

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %1 a_crossboundarytest_d a crossboundarytest d

call %TESTBATCHROOT%\runindividualtest.cmd %TESTBATCHROOT% %1 a_b_crossboundarytest a b crossboundarytest

pushd %TESTBATCHROOT%