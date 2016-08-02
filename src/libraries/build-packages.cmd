@call %~dp0run.cmd build-managed -packages -nodoReuse -binclashWindows %*
@exit /b %ERRORLEVEL%