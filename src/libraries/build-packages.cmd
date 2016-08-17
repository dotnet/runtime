@call %~dp0run.cmd build-managed -packages -nodeReuse -binclashWindows %*
@exit /b %ERRORLEVEL%