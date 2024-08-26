setlocal 
set COMPOSITENAME=
set COMPILEARG=
set TESTINITIALBINPATH=%2
set TESTTARGET_DIR=%3
set TESTBATCHROOT=%1


:Loop
if "%4"=="" goto Continue

set COMPILEARG=%COMPILEARG% %TESTINITIALBINPATH%\%4.dll
set COMPOSITENAME=%COMPOSITENAME%%4

if "%5"=="CG2Single" (
  call :CG2Single
  shift
) ELSE (
  if "%5"=="CG2SingleInputBubble" (
    call :CG2SingleInputBubble
    shift
  ) ELSE (
    if "%5"=="CG1Single" (
      call :CG1Single
      shift
    ) ELSE (
      if "%5"=="CG2SingleBubbleADOnly" (
        call :CG2SingleBubbleADOnly
        shift
      ) ELSE (
        if "%5"=="CG2NoMethods" (
          call :CG2NoMethods
          shift
        ) ELSE (
          if "%5"=="CG2Composite" (
            shift
            call :Continue
          )
        )
      )
    )
  )
)

shift
goto Loop
:Continue

if "%COMPOSITENAME%"=="" goto done

set BUILDCMD=%TESTBATCHROOT%\..\..\..\..\..\..\.dotnet\dotnet %CORE_ROOT%\crossgen2\crossgen2.dll -r %CORE_ROOT%\* -r %TESTINITIALBINPATH%\*.dll -o %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%Composite.dll --composite %COMPILEARG%
echo %BUILDCMD% > %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%Composite.dll.log
call %BUILDCMD% >> %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%Composite.dll.log 2>&1
if NOT EXIST %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%Composite.dll del %TESTINITIALBINPATH%\%TESTTARGET_DIR%\a.dll
goto done

:CG2Single
del %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll
set BUILDCMD=%TESTBATCHROOT%\..\..\..\..\..\..\.dotnet\dotnet %CORE_ROOT%\crossgen2\crossgen2.dll -r %CORE_ROOT%\* -r %TESTINITIALBINPATH%\*.dll -o %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll  %TESTINITIALBINPATH%\%COMPOSITENAME%.dll
echo %BUILDCMD% > %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll.log
call %BUILDCMD% >> %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll.log 2>&1
goto done

:CG2NoMethods
del %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll
set BUILDCMD=%TESTBATCHROOT%\..\..\..\..\..\..\.dotnet\dotnet %CORE_ROOT%\crossgen2\crossgen2.dll --compile-no-methods -r %CORE_ROOT%\* -r %TESTINITIALBINPATH%\*.dll -o %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll  %TESTINITIALBINPATH%\%COMPOSITENAME%.dll
echo %BUILDCMD% > %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll.log
call %BUILDCMD% >> %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll.log 2>&1
goto done

:CG2SingleInputBubble
del %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll
set BUILDCMD=%TESTBATCHROOT%\..\..\..\..\..\..\.dotnet\dotnet %CORE_ROOT%\crossgen2\crossgen2.dll -r %CORE_ROOT%\* -r %TESTINITIALBINPATH%\*.dll -o %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll --inputbubble %TESTINITIALBINPATH%\%COMPOSITENAME%.dll
echo %BUILDCMD% > %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll.log
call %BUILDCMD% >> %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll.log 2>&1
goto done

:CG2SingleBubbleADOnly
del %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll
set BUILDCMD=%TESTBATCHROOT%\..\..\..\..\..\..\.dotnet\dotnet %CORE_ROOT%\crossgen2\crossgen2.dll -r %CORE_ROOT%\* -r %TESTINITIALBINPATH%\d.dll -o %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll --inputbubble %TESTINITIALBINPATH%\%COMPOSITENAME%.dll
echo %BUILDCMD% > %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll.log
call %BUILDCMD% >> %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll.log 2>&1
goto done

:CG1Single
del %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll
set BUILDCMD=%CORE_ROOT%\crossgen.exe /Platform_Assemblies_Paths %CORE_ROOT%;%TESTINITIALBINPATH% /out %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll /in %TESTINITIALBINPATH%\%COMPOSITENAME%.dll
echo %BUILDCMD% > %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll.log
call %BUILDCMD% >> %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%.dll.log 2>&1
goto done

:done
shift
set COMPILEARG=
set COMPOSITENAME=
