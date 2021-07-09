## How to run a sample app with AOT profiling enabled

### Setting up a project with profiling

1. Define a `write_at` method. By default it is:

```
[MethodImpl(MethodImplOptions.NoInlining)]
 public static void StopProfile(){}
```

2. Initialize the profiler in the main javascript (e.g. runtime.js)

```
var Module = {
  onRuntimeInitialized: function () {
    ...

    if (config.enable_profiler)
    {
      config.aot_profiler_options = {
        write_at: "<Namespace.Class::StopProfile>",
        send_to: "System.Runtime.InteropServices.JavaScript.Runtime::DumpAotProfileData"
    }
  }
```

3. Call the `write_at` method at the end of the app, either in C# or in JS. To call the `write_at` method in JS, make use of bindings:

`BINDING.call_static_method("<[ProjectName] Namespace.Class::StopProfile">, []);`

When the `write_at` method is called, the `send_to` method `DumpAotProfileData` stores the profile data into `Module.aot_profile_data`

4. Download `Module.aot_profile_data` in JS, using something similar to:

```
function saveProfile() {
  var a = document.createElement('a');
  var blob = new Blob([Module.aot_profile_data]);
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

`<Import Project="$(MonoProjectRoot)\wasm\build\WasmApp.InTree.targets" />` <br/>
`<Import Project="$(MonoProjectRoot)wasm\build\WasmApp.InTree.props" />`

For more information on how to utilize WasmApp.InTree.targets/props consult the wasm build directory [README.md](../../../wasm/README.md)

2. To get the profile data, run:

`make get-aot-profile`

Which will build and run the current project with AOT disabled and the AOT profiler enabled.

3. Go to localhost:8000 and the profile will automatically download.

4. To use the profile data in the project, run:

`make use-aot-profile PROFILE_PATH=<path to profile file>`
