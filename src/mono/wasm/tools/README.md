# Install wasm workload packs 

`$ dotnet build src/mono/wasm/tools/create-pack.proj /p:DotnetRoot=/usr/local/share/dotnet /p:DotnetVersion=6.0.0-dev /p:RuntimeConfig=Release`

# building with workloads:

build with `MSBuildEnableWorkloadResolver=true DOTNETSDK_WORKLOAD_MANIFEST_ROOT=</path/to/sdk-manifests>/<version>`
