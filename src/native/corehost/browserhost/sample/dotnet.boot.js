// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WASM-TODO: this is dummy configuration until we have MSBuild WASM SDK for corehost, which will generate this file

export const config = /*json-start*/{
  "mainAssemblyName": "HelloWorld.dll",
  "virtualWorkingDirectory": "/",
  "resources": {
    "jsModuleNative": [
      {
        "name": "dotnet.native.js"
      }
    ],
    "jsModuleRuntime": [
      {
        "name": "dotnet.runtime.js"
      }
    ],
    "wasmNative": [
      {
        "name": "dotnet.native.wasm",
      }
    ],
    "coreAssembly": [
      {
        "virtualPath": "/System.Private.CoreLib.dll",
        "name": "System.Private.CoreLib.dll"
      },
    ],
    "assembly": [
      {
        "virtualPath": "/System.Runtime.dll",
        "name": "System.Runtime.dll"
      },
      {
        "virtualPath": "/System.Threading.dll",
        "name": "System.Threading.dll"
      },
      {
        "virtualPath": "/System.Runtime.InteropServices.dll",
        "name": "System.Runtime.InteropServices.dll"
      },
      {
        "virtualPath": "/System.Console.dll",
        "name": "System.Console.dll"
      },
      {
        "virtualPath": "/HelloWorld.dll",
        "name": "HelloWorld.dll"
      }
    ]
  }
}/*json-end*/;