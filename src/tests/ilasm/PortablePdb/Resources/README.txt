In order to verify whether ilasm produces correct portable pdb format one can do the following:

1. Copy 'TestMethodDebugInformation' directory with its contents to:
    - WINDOWS: C:\\tmp\\
    - LINUX:   /tmp/
2. Run IlasmPortablePdb tests which reference .cs source files from 'TestMethodDebugInformation':
    .\..\TestFiles\TestMethodDebugInformation_*.il
3. On successful completion load the generated dlls from tests output directory:
    TestMethodDebugInformation_*.dll
    TestMethodDebugInformation_*.pdb
   with a debugger and step through the .cs code to verify correctness of the portable PDB format

NOTE: this should work only if 'TestMethodDebugInformation' directory is properly placed in paths
specified in 1.
