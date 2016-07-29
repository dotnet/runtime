@call %~dp0run.cmd build-managed -tests -nodoReuse -binclashWindows %*
@exit /b %ERRORLEVEL%