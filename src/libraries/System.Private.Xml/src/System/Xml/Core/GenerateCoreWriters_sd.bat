@echo off

pushd %_NTDRIVE%%_NTROOT%\ndp\fx\src\Xml\System\Xml\Core
echo. 
echo Checking files out..
echo.

sd edit XmlUtf8RawTextWriter.cs
sd edit XmlEncodedRawTextWriter.cs
sd edit XmlUtf8RawTextWriter_SL.cs
sd edit XmlEncodedRawTextWriter_SL.cs

rem Make read-only in case user is offline and sd edit fails
attrib -r XmlUtf8RawTextWriter.cs
attrib -r XmlEncodedRawTextWriter.cs
attrib -r XmlUtf8RawTextWriter_SL.cs
attrib -r XmlEncodedRawTextWriter_SL.cs


echo.
echo Generating writers..
echo.

cl.exe /C /EP /D _XML_UTF8_TEXT_WRITER XmlRawTextWriterGenerator.cxx > XmlUtf8RawTextWriter.cs
cl.exe /C /EP /D _XML_ENCODED_TEXT_WRITER XmlRawTextWriterGenerator.cxx > XmlEncodedRawTextWriter.cs

cl.exe /C /EP /D _XML_UTF8_TEXT_WRITER /D SILVERLIGHT XmlRawTextWriterGenerator.cxx > XmlUtf8RawTextWriter_SL.cs
cl.exe /C /EP /D _XML_ENCODED_TEXT_WRITER /D SILVERLIGHT XmlRawTextWriterGenerator.cxx > XmlEncodedRawTextWriter_SL.cs

popd