# Troubleshooting NativeAOT compiler

Sometimes you want to have more information how NativeAOT works. The compiler provides several switches for that:

* `<IlcGenerateMetadataLog>true</IlcGenerateMetadataLog>`: Enable generation of metadata log. This class is CSV format with following structure: `Handle, Kind, Name, Children`.
* `<IlcGenerateDgmlFile>true</IlcGenerateDgmlFile>`: Generates log files `ProjectName.codegen.dgml.xml` and `ProjectName.scan.dgml.xml` in DGML format.
* `<IlcGenerateMapFile>true</IlcGenerateMapFile>`: Generates log files `ProjectName.map.xml` which describe layout of objects.
* `<IlcSingleThreaded>true</IlcSingleThreaded>`: Perform compilation on single thread.
* `<IlcDumpGeneratedIL>true</IlcDumpGeneratedIL>`: Dump IL for method bodies the compiler generated on the fly into `ProjectName.il`. This can be helpful when debugging IL generation - e.g. marshalling. The compiler maps debug information to this file, so it's possible to step in it and set breakpoints.
* `<IlcGenerateMstatFile>true</IlcGenerateMstatFile>`: Generation of mstat file which can be parsed using. The mstat file is a binary file which can be read by a tool like: https://gist.github.com/MichalStrehovsky/2c7cb3d623c7f8901541914dab04238d. Just change the file path on line 12 to your app's.
