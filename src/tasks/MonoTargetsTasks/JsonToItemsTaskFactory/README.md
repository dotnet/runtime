# JsonToItemsTaskFactory

A utility for reading json blobs into MSBuild items and properties.

## Json blob format

The json data must be a single toplevel dictionary with a `"properties"` or an `"items"` key (both are optional).

The `"properties"` value must be a dictionary with more string values.  The keys are case-insensitive and duplicates are not allowed.

The `"items"` value must be an array of either string or dictionary elements (or a mix of both).
String elements use the string value as the `Identity`.
Dictionary elements must have strings as values, and must include an `"Identity"` key, and as many other metadata key/value pairs as desired.  This dictionary is also case-insensitive and duplicate metadata keys are also not allowed.

#### Example

```json
{
    "properties": {
        "x1": "val1",
        "X2": "val2",
    },
    "items" : {
        "FunFiles": ["funFile1.txt", "funFile2.txt"],
        "FilesWithMeta": [{"identity": "funFile3.txt", "TargetPath": "bin/fun3"},
                        "funFile3.and.a.half.txt",
                        {"identity": "funFile4.txt", "TargetPath": "bin/fun4"}]
    }
}
```

## UsingTask and Writing Targets

To use the task, you need to reference the assembly and add the task to the project, as well as declare the task parameters that correspond to the properties and items you want to retrieve from the json blob.

```xml
<UsingTask TaskName="MyJsonReader" AssemblyFile="..\JsonToItemsTaskFactory\bin\Debug\net6.0\JsonToItemsTaskFactory.dll"
        TaskFactory="JsonToItemsTaskFactory.JsonToItemsTaskFactory">
    <ParameterGroup>
        <X1 ParameterType="System.String" Required="false" Output="true" />
        <FunFiles ParameterType="Microsoft.Build.Framework.ITaskItem[]" Required="false" Output="true" />
        <FilesWithMeta ParameterType="Microsoft.Build.Framework.ITaskItem[]" Required="false" Output="true" />
    </ParameterGroup>
</UsingTask>
```

The parameter group parameters are all optional. They must be non-required outputs of type `System.String` or `Microsoft.Build.Framework.ITaskItem[]`.  The former declares properties to capture from the file, while the latter declares item lists.

The above declares a task `MyJsonReader` which will be used to retries the `X1` property and the `FunFiles` and `FilesWithMeta` items.

To use the task, a `JsonFilePath` attribute specifies the file to read.

```xml
<Target Name="RunMe">
    <MyJsonReader JsonFilePath="$(MSBuildThisFileDirectory)\example.jsonc">
        <Output TaskParameter="X1" PropertyName="FromJsonX1" />
        <Output TaskParameter="FunFiles" ItemName="FromJsonFunFiles" />
        <Output TaskParameter="FilesWithMeta" ItemName="FromJsonWithMeta" />
    </MyJsonReader> 
    <Message Importance="High" Text="X1 = $(FromJsonX1)" />
    <Message Importance="High" Text="FunFiles = @(FromJsonFunFiles)" />
    <ItemGroup>
        <FromJsonWithMetaWithTP Include="@(FromJsonWithMeta->HasMetadata('TargetPath'))" />
        <FromJsonWithMetaWithoutTP Include="@(FromJsonWithMeta)" Exclude="@(FromJsonWithMetaWithTP)" />
    </ItemGroup>
    <Message Importance="High" Text="FilesWithMeta = @(FromJsonWithMetaWithTP)  TargetPath='%(TargetPath)'"/>
    <Message Importance="High" Text="FilesWithMeta = @(FromJsonWithMetaWithoutTP)  (No TargetPath)" />
</Target>
```

When the target `RunMe` runs, the task will read the json file and populate the outputs.  Running the target, the output will be:

```console
$ dotnet build Example
  X1 = val1
  FunFiles = funFile1.txt;funFile2.txt
  FilesWithMeta = funFile3.txt  TargetPath='bin/fun3'
  FilesWithMeta = funFile4.txt  TargetPath='bin/fun4'
  FilesWithMeta = funFile3.and.a.half.txt  (No TargetPath)

Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:00.15
```

