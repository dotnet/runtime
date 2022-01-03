#!/bin/bash

cp artifacts/packages/Debug/Shipping/Microsoft.NETCore.App.Ref.7.0.0-dev.nupkg ./tmp
cd tmp
unzip Microsoft.NETCore.App.Ref.7.0.0-dev.nupkg 
chmod 777 ref/net7.0/*
cp ref/net7.0/System.Runtime.Intrinsics.* ~/Projects/dotnet/runtime-nightly/packs/Microsoft.NETCore.App.Ref/7.0.0-alpha.1.21567.1/ref/net7.0/ 
