# Mobile runtimeconfig.json host configuration

## Motivation

`runtimeconfig.json` is a file used by .NET 5+ applications to pass arbitrary configuration parameters via the application host to the .NET runtime.  On mobile and WebAssembly, we would like to support passing configuration properties from the Mono embedder to the runtime.

To minimize the impact on the app startup time, the design constraints are as follows:
1. Don’t parse JSON at runtime during application startup
2. Don’t need to support shared framework configuration parameters
3. Don't need to support the desktop host environment variables, startup hooks, or other configuration mechanisms that do not make sense on mobile.
4. This is separate from the key/value set passed to `monovm_initialize`/`coreclr_initialize`  (ie we will not pass the additional properties via `monovm_initialize` - just using this new mechanism). Things like the TPA list, additional probing paths, PINVOKE_OVERRIDE, etc will be addressed by an evolution of `monovm_initialize` - https://github.com/dotnet/runtime/issues/48416

## Design Overview

We break up runtimeconfig.json loading into two parts:
1. A new MSBuild task called `RuntimeConfigParser` will run after the `runtimeconfig.json` is created by the `dotnet build` process. The task will convert the properties and their values into a binary blob format. The resulting `runtimeconfig.bin` file will be bundled with the application.
2. The runtime will expose a new API entrypoint `monovm_runtimeconfig_initialize` that gets either a path to pass to `mono_file_map_open` or a pointer to the blob in memory. When called, the runtime will read the binary data and populate the managed AppContext with the properties.

We will only use the `runtimeOptions→configProperties` json key. Its content is a JSON dictionary with string keys and string/bool/numeric values.  We convert the values to strings when we store them in the binary runtimeconfig.bin, which is the same way they are treated by the default host.

The runtime assumes that the properties passed via `monovm_initialize` and `monovm_runtimeconfig_initialize` will be different. To ensure this, the provided MSBuild task will be passed a list of property names that the embedder promises it will pass to `monovm_initialize`. The MSBuild task will check that `runtimeconfig.json` does not set any of those same properties. If there is a duplicate, error out.

All properties set in either the `runtimeconfig.json` or set via `monovm_initialize` will be propagated to the managed `AppContext`. Mono will also check for properties it supports in the runtime itself and make use of them as appropriate.

## Design Details

### Encoded file generation

 The runtime pack will provide an MSBuild task called RuntimeConfigParserTask to generate the encoded file. The generator checks for duplicate property keys (by comparing the keys in the json file with an input list of properties that the embedder promises to pass to `monovm_initialize`).

#### Task Contract:

The task will take 3 input arguments:
1. The path to the `runtimeconfig.json` file.
2. The name of the destination file to be written in the encoded format.
3. An item list (`ITaskItem[]`) that is the name of the properties that the embedder will set on `monovm_initialize`.

The task should:
1. Parse the given input file and create a dictionary from the configProperties key.
2. Compare the keys from the input file with the names given in the item list. If there are any duplicates, return an MSBuild Error.
3. Generate the output file.

#### Example of the usage of the task:

 ```
 <UsingTask TaskName="RuntimeConfigParserTask"
            AssemblyFile="$(RuntimeConfigParserTasksAssemblyPath)" />

<Target Name="BundleTestAndroidApp">
  <RuntimeConfigParserTask
      RuntimeConfigFile="$(Path_to_runtimeconfig.json_file)"
      OutputFile="$(Path_to_generated_binary_file)"
      RuntimeConfigReservedProperties="@(runtime_properties_reserved_by_host)">
  </RuntimeConfigParserTask>
</Target>
 ```

### The encoded runtimeconfig format

The format is:
1. There will be an (1- to 4-byte) ECMA-335 II.23.2 compressed unsigned integer count = N, indicating the number of key-value pairs.
2. It will have 2xN  ECMA-335 SerString UTF8 strings (that is, each string is preceded by its length stored in the compressed unsigned integer format): Each key followed by its value.

Sample input
```
{
    "runtimeOptions": {
        "configProperties": {
            "key1": "value1",
            "key2": "value2"
        }
    }
}
```

Sample output (as hexdump -C bytes)
```
00000000  02 04 6b 65 79 31 06 76  61 6c 75 65 31 04 6b 65  |..key1.value1.ke|
00000010  79 32 06 76 61 6c 75 65  32                       |y2.value2|
00000019
```

### New embedding API entrypoint

We want at least 2 ways to pass data in: either ask the runtime to open the file, or give it a pointer to the data in memory. Also, we need some way to cleanup.

```
struct MonovmRuntimeConfigArguments {
  uint32_t kind; // 0 = Path of runtimeconfig.bin file, 1 = pointer to the blob data, >= 2 reserved
  union {
    struct {
      const char *path; // null terminated absolute path
    } name;
    struct {
      const char *data;
      uint32_t data_len;
    } data;
  } runtimeconfig;
};

typedef void (*MonovmRuntimeConfigArgumentsCleanup)(MonovmRuntimeConfigArguments *args, void* user_data);

MONO_API void
monovm_runtimeconfig_initialize (MonovmRuntimeConfigArguments *args, MonovmRuntimeConfigArgumentsCleanup cleanup_fn, void* user_data);
```

This declaration should live in the unstable header. `monovm_runtimeconfig_initialize` should be called before `monovm_initialize`.

#### Example of the usage of `monovm_runtimeconfig_initialize`

```
void
cleanup_runtime_config (MonovmRuntimeConfigArguments *args, void *user_data)
{
    free (args);
    free (user_data); // This may not be needed, depending on if there is anything needs to be freed.
}

int
mono_droid_runtime_init (const char* executable, int managed_argc, char* managed_argv[])
{
  
  ......

  MonovmRuntimeConfigArguments *arg = (MonovmRuntimeConfigArguments *)malloc (sizeof (MonovmRuntimeConfigArguments));
  arg->kind = 0;
  arg->runtimeconfig.name.path = "path_to_generated_binary_file";
  monovm_runtimeconfig_initialize (arg, cleanup_runtime_config, NULL);

  monovm_initialize(......);
  ......

}
```

### Register and install runtime properties

`monovm_runtimeconfig_initialize` will register the type `MonovmRuntimeConfigArguments` variable with the runtime. If given the path of the runtimeconfig.bin file, the runtime will read the binary data from the file, otherwise, it will parse the binary data from memory. The properties will be combined with the ones registered by `monovm_initialize` and used to initialize System.AppContext.

### Cleanup function

The `MonovmRuntimeConfigArguments*` will be stored in the runtime between `monovm_runtimeconfig_initialize` and `mono_jit_init_version`. The embedder should not dispose of the arguments after calling `monovm_runtimeconfig_initialize`. Instead the runtime will call the `cleanup_fn` that is passed to `monovm_runtimeconfig_initialize` at soon after it has initialized the managed property list in `System.AppContext` (which happens as part of `mono_jit_init_version`).
