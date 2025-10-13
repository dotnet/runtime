# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# this is just a incomplete sample app deployment
# WASM-TODO: implement proper in-tree project via MSBuild and WASM SDK

set(SAMPLE_ASSETS
    index.html
    main.mjs
    dotnet.boot.js
    ${STATIC_LIB_DESTINATION}/dotnet.js
    ${STATIC_LIB_DESTINATION}/dotnet.js.map
    ${STATIC_LIB_DESTINATION}/dotnet.d.ts
    ${STATIC_LIB_DESTINATION}/dotnet.runtime.js
    ${STATIC_LIB_DESTINATION}/dotnet.runtime.js.map

    # Bring your own DLLs and update the dotnet.boot.js to match
        # HelloWorld.dll
        # System.Private.CoreLib.dll
        # System.Console.dll
        # System.Runtime.dll
        # System.Runtime.InteropServices.dll
        # System.Threading.dll

)
install(FILES ${SAMPLE_ASSETS} DESTINATION corehost COMPONENT runtime)
