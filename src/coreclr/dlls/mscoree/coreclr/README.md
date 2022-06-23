dump\_helper\_resource.rc is used to populate the MINIDUMP\_AUXILIARY\_PROVIDER resource inside coreclr.dll on Windows. When an application crashes, Windows MinidumpWriteDump is planning to scan
modules looking for this resource. The content of the resource is expected to be the name of a dll in the same folder, encoded in UTF8, null terminated, that implements the
CLRDataCreateInterface function. For OS security purposes MinidumpWriteDump will do an authenticode signing check before loading the indicated binary, however if your build isn't
signed you can get around this limitation by registering it at HKLM\Software\Microsoft\WindowsNT\CurrentVersion\MiniDumpAuxilliaryDlls.
