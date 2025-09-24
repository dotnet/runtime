# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# this is just a incomplete sample app deployment
# WASM-TODO: implement proper in-tree project via MSBuild and WASM SDK
# WASM-TODO: deploy *.js.map when available
set(SAMPLE_ASSETS
    index.html
    main.mjs
    dotnet.boot.js
    ${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/dotnet.js
    ${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/dotnet.d.ts
    ${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js

    # Bring your own DLLs and update the dotnet.boot.js to match
        # HelloWorld.dll
        # System.Private.CoreLib.dll
        # System.Console.dll
        # System.Runtime.dll
        # System.Runtime.InteropServices.dll
        # System.Threading.dll

)
install(FILES ${SAMPLE_ASSETS} DESTINATION corehost COMPONENT runtime)
