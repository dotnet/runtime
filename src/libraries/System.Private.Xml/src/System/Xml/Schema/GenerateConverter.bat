%DD_NdpInstallPath%\csc.exe GenerateConverter.cs /debug /reference:system.dll,system.xml.dll
if ERRORLEVEL = 0 GenerateConverter.exe