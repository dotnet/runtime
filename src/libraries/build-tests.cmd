@call %~dp0run.cmd build-managed -tests -nodeReuse -binclashWindows %*
@exit /b %ERRORLEVEL%