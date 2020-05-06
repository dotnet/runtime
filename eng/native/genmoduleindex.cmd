@echo off
REM Generate module index header

if [%1]==[] goto :Usage
if [%2]==[] goto :Usage

setlocal
for /f "tokens=1" %%i in ('dumpbin /HEADERS %1 ^| findstr /c:"size of image"') do set imagesize=%%i
REM Pad the extracted size to 8 hex digits
set imagesize=00000000%imagesize%
set imagesize=%imagesize:~-8%

for /f "tokens=1" %%i in ('dumpbin /HEADERS %1 ^| findstr /c:"time date"') do set timestamp=%%i
REM Pad the extracted time stamp to 8 hex digits
set timestamp=00000000%timestamp%
set timestamp=%timestamp:~-8%

echo 0x08, 0x%timestamp:~6,2%, 0x%timestamp:~4,2%, 0x%timestamp:~2,2%, 0x%timestamp:~0,2%, 0x%imagesize:~6,2%, 0x%imagesize:~4,2%, 0x%imagesize:~2,2%, 0x%imagesize:~0,2%, > %2

endlocal
exit /b 0

:Usage
echo Usage: genmoduleindex.cmd ModuleBinaryFile IndexHeaderFile
exit /b 1
