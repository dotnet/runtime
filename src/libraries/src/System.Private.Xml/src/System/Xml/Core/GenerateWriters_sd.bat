@echo off

pushd %_NTDRIVE%%_NTROOT%\ndp\fx\src\Xml\System\Xml\Core

call GenerateCoreWriters.bat

echo. 
echo Checking files out..
echo.

sd edit HtmlUtf8RawTextWriter.cs
sd edit HtmlEncodedRawTextWriter.cs
sd edit TextUtf8RawTextWriter.cs
sd edit TextEncodedRawTextWriter.cs

rem Make read-only in case user is offline and sd edit fails

attrib -r HtmlUtf8RawTextWriter.cs
attrib -r HtmlEncodedRawTextWriter.cs
attrib -r TextUtf8RawTextWriter.cs
attrib -r TextEncodedRawTextWriter.cs

echo.
echo Generating writers..
echo.

cl.exe /C /EP /D _HTML_UTF8_TEXT_WRITER HtmlRawTextWriterGenerator.cxx > HtmlUtf8RawTextWriter.cs
cl.exe /C /EP /D _HTML_ENCODED_TEXT_WRITER HtmlRawTextWriterGenerator.cxx > HtmlEncodedRawTextWriter.cs
cl.exe /C /EP /D _UTF8_TEXT_WRITER TextRawTextWriterGenerator.cxx > TextUtf8RawTextWriter.cs
cl.exe /C /EP /D _ENCODED_TEXT_WRITER TextRawTextWriterGenerator.cxx > TextEncodedRawTextWriter.cs

popd
