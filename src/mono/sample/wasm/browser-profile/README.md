## How to run a sample app with AOT profiling enabled

### Setting up a project with profiling

1. Define a `write_at` method. By default it is:

```
[JSExport]
[MethodImpl(MethodImplOptions.NoInlining)]
public static void StopProfile(){}
```

2. Initialize the profiler in the main javascript (e.g. main.js)

```
await dotnet
    .withConfig({
        aotProfilerOptions: {
            writeAt: "<Namespace.Class::StopProfile>",
            sendTo: "System.Runtime.InteropServices.JavaScript.JavaScriptExports::DumpAotProfileData"
        }
    })
    .create();
```

3. Call the `write_at` method at the end of the app, either in C# or in JS. To call the `write_at` method in JS, make use of bindings:

```
const exports = await getAssemblyExports("<ProjectName>");
exports.<Namespace.Class.StopProfile>();
```

When the `write_at` method is called, the `send_to` method `DumpAotProfileData` stores the profile data into `INTERNAL.aotProfileData`

4. Download `INTERNAL.aotProfileData` in JS, using something similar to:

```
function saveProfile() {
  var a = document.createElement('a');
  var blob = new Blob([INTERNAL.aotProfileData]);
  a.href = URL.createObjectURL(blob);
  a.download = "data.aotprofile";
  // Append anchor to body.
  document.body.appendChild(a);
  a.click();

  // Remove anchor from body
  document.body.removeChild(a);
}
```

### Build and Run a project with profiling
1. To enable profiling during a build, we need to make use of WasmApp.InTree.targets/props by importing into the project file:

`<Import Project="$(BrowserProjectRoot)build\WasmApp.InTree.targets" />` <br/>
`<Import Project="$(BrowserProjectRoot)build\WasmApp.InTree.props" />`

For more information on how to utilize WasmApp.InTree.targets/props consult the wasm build directory [README.md](../../../wasm/README.md)

2. To get the profile data, run:

`make get-aot-profile`

Which will build and run the current project with AOT disabled and the AOT profiler enabled.

3. Go to localhost:8000 and the profile will automatically download.

4. To use the profile data in the project, run:

`make use-aot-profile PROFILE_PATH=<path to profile file>`
