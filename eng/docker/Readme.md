# Docker Build Infrastructure

Provides reusable docker build infrastructure for the dotnet/runtime repo.

## libraries-sdk Dockerfiles

The `libraries-sdk` Dockerfiles can be used to build dotnet sdk docker images
that contain the current libraries built from source.
These images can be used to build dockerized dotnet services that target the current libraries.
Currently, debian and windows nanoserver sdk's are supported.

### Building the images

To build the linux image locally

```powershell
PS> .\build-docker-sdk.ps1 -t dotnet-linux-sdk-current
```

and for Windows:

```powershell
PS> .\build-docker-sdk.ps1 -w -t dotnet-nanoserver-sdk-current
```

To use Debug builds:

```powershell
PS> .\build-docker-sdk.ps1 -c Debug -t dotnet-sdk-current
```
