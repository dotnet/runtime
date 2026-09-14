# cDAC memory enumeration provider known issues

## SOS tests requiring DBI

Some Windows x64 SOS tests fail when they attempt to create an
`ICorDebugProcess`. The in-box `mscordaccore.dll` is the dump collector and
does not export `DacDbiInterfaceInstance`, but SOS currently passes that
library to DBI. This produces `DBI OpenVirtualProcessImpl2 FAILED
0x8007007F`.

Heap dump creation and SOS commands that use `IXCLRDataProcess` are not
affected. The failing tests require SOS to load DBI with the full cDAC.
[dotnet/diagnostics#5980](https://github.com/dotnet/diagnostics/pull/5980)
implements that activation path.

## CI infrastructure failures

Seven cDAC dump and stress test jobs completed their tests successfully but
were reported as failed because publishing the test results failed with an
Azure DevOps `TF10216` error or timed out. These failures do not indicate a
dump collector or test failure.
