# Linked javascript files
They are emcc way how to extend the dotnet.js script during linking, by appending the scripts.
See https://emscripten.org/docs/tools_reference/emcc.html#emcc-pre-js

There are `-extern-pre-js`,`-pre-js`, `-post-js`, `-extern-post-js`.
In `src\mono\wasm\build\WasmApp.Native.targets` we apply them by file naming convention as: `*.extpre.js`,`*.pre.js`, `*.post.js`, `*.extpost.js`

In `src\mono\wasm\runtime\CMakeLists.txt` which links only in-tree, we use same mapping explicitly.
