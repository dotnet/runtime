# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# this is just a incomplete sample app deployment
# WASM-TODO: implement proper in-tree project via MSBuild and WASM SDK

set(NATIVE_JAVASCRIPT_BIN
    ${CLR_ARTIFACTS_BIN_DIR}/native/net${CMAKE_NET_CORE_APP_CURRENT_VERSION}-browser-${CMAKE_BUILD_TYPE}-wasm/)


set(SAMPLE_ASSETS
    index.html
    main.mjs
    dotnet.boot.js
    ${NATIVE_JAVASCRIPT_BIN}/dotnet.js
    ${NATIVE_JAVASCRIPT_BIN}/dotnet.js.map
    ${NATIVE_JAVASCRIPT_BIN}/dotnet.d.ts
    ${NATIVE_JAVASCRIPT_BIN}/dotnet.runtime.js
    ${NATIVE_JAVASCRIPT_BIN}/dotnet.runtime.js.map

    # Bring your own DLLs and update the dotnet.boot.js to match
        # HelloWorld.dll
        # System.Private.CoreLib.dll
        # System.Console.dll
        # System.Runtime.dll
        # System.Runtime.InteropServices.dll
        # System.Threading.dll

)
install(FILES ${SAMPLE_ASSETS} DESTINATION corehost COMPONENT runtime)
