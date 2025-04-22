@echo off

for /f "delims=" %%A in ('uuidgen.exe -s') do @echo %%A | sed "s/INTERFACENAME/constexpr GUID JITEEVersionIdentifier/"
