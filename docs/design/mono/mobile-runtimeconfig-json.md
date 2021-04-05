# Mobile runtimeconfig.json host configuration

## Motivation

`runtimeconfig.json` is a file used by .NET 5+ applications to pass arbitrary configuration parameters via the application host to the .NET runtime.  On mobile and WebAssembly, we would like to support passing configuration properties from the Mono embedder to the runtime.

To minimize the impact of the app startup time, the design should preserve these contraints:
1. Don’t parse JSON file at runtime at application startup
2. Don’t need to support full set of well-known properties, just the ones that make sense on mobile
3. Don’t need to support shared framework configuration parameters
4. Don't need to support the desktop host environment variables, startup hooks, or other configuration mechanisms that do not make sense on mobile.
5. This is separate from the key/value set passed to `monovm_initialize`/`coreclr_initialize`  (ie we will not pass the additional properties via `monovm_initialize` - just using this new mechanism). Things like the TPA list, additional probing paths, PINVOKE_OVERRIDE, etc will be addressed by an evolution of `monovm_initialize` - https://github.com/dotnet/runtime/issues/48416

## Design Overview

This feature could be achived by mainly two parts:
1. A new MSBuild task called `RuntimeConfigParser` will run after the `runtimeconfig.json` is created by the dotnet build process. The task will extract properties keys and values into a binary blob format. The resulting `runtimeconfig.blob` file will be bundled with the application.
2. The runtime will expose a new API entrypoint `monovm_runtimeconfig_initialize` that gets either a path to pass to `mono_file_map_open` or a pointer to the blob in memory. Then, the runtime will read the binary data and populate the managed AppContext with the properties.

We will only take the `runtimeOptions→configProperties` json key. Its content is a flat key-value string/boolean dictionary.

The runtime will assume that the properties passed via `monovm_initialize` and `monovm_runtimeconfig_initialize` will be different. To ensure this, the MSBuild task that we will provide will be given a list of property names that the embedder promises it will pass to `monovm_initialize`. The MSBuild task will check that `runtimeconfig.json` does not set any of those same properties. If there is a duplicate, error out.

We take everything and pass all the properties to the managed AppContext. For the well-known standard properties the mono runtime will read and propagate the ones it cares about and ignore the rest.

## Design Details

### Encoded file generation

We should be able to generate the encoded file from C# as an MSBuild task, using System.Text.Json and System.Reflection.Metadata. This generator should also do duplicate checking (by comparing the keys in the json file with an input list of properties that the embedder promises to pass to `monovm_initialize`).

#### Task Contract:

The task will take 3 input arguments:
1. The path to the `runtimeconfig.json` file.
2. The name of the destination file to be written in the encoded format.
3. An item list (`ITaskItem[]`) that is the name of the properties that the embedder will set on `monovm_initialize`.

The task should:
1. Parse the given input file and create a dictionary from the configProperties key.
2. Compare the keys from the input file with the names given in the item list. If there are any duplicates, return an MSBuild Error.
3. Generate the output file.

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

This declaration should live in the unstable header. `monovm_runtimeconfig_initialize` should be called after `monovm_initialize` but before starting the runtime. The cleanup function should be called in `mono_runtime_install_appctx_properties`.

### Register and install runtime properties

`monovm_runtimeconfig_initialize` will register the type `MonovmRuntimeConfigArguments` variable as it is. Processing and installing the properties will be done inside `mono_runtime_install_appctx_properties`. If given the path of runtimeconfig.blob file, read the binary data from the file first. Otherwise, parse the binary data and combine the properties with the ones registered by `monovm_initialize`.
