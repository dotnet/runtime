dump\_helper\_resource.bin in this folder is a text file with a single 0 byte appended on the end using a hex-editor. It is unlikely it will need to be modified frequently if at all,
but if that changes we can always create a little nicer tooling for it.

dump\_helper\_resource.bin is used to populate the DUMP\_HELPER resource inside coreclr.dll on Windows. When an application crashes, Windows MinidumpWriteDump is planning to scan
modules looking for this resource. The content of the resource is expected to be the name of a dll in the same folder, encoded in UTF8, null terminated, that implements the
CLRDataCreateInterface function. For OS security purposes MinidumpWriteDump will do an authenticode signing check before loading the indicated binary, however if your build isn't
signed you can get around this limitation by registering it at HKLM\Software\Microsoft\WindowsNT\CurrentVersion\MiniDumpAuxilliaryDlls.
