## SslStress

Provides stress testing scenaria for System.Net.Security.SslStream.

### Running the suite locally

Prerequisite: the runtime and the libraries should be [live-built](https://github.com/dotnet/runtime/tree/main/docs/workflow/building/libraries) with `build.cmd`/`build.sh`.

Use the script `build-local.sh` / `Build-Local.ps1` to build the stress project against the live-built runtime.

```bash
$ build-local.sh [StressConfiguration] [LibrariesConfiguration]
```

The build script will also generate the runscript that runs the stress suite using the locally built testhost in the form of `run-stress-<StressConfiguration>-<LirariesConfiguration>.sh`. To run the tests with the script, assuming that both the stress project and the libraries have been built against Release configuration:

```bash
$ run-stress-Release-Release.sh [stress suite args]
```

To get the full list of available parameters:

```bash
$ run-stress-Release-Release.sh.sh -help
```

### Building and running with Docker

A docker image containing the live-built runtime bits and the latest daily SDK is created with the [`build-docker-sdk.sh/ps1` scripts](/eng/docker/Readme.md).

It's possible to manually `docker build` a docker image containing the stress project based on the docker image created with `build-docker-sdk.sh/ps1`, however the preferred way is to use docker-compose, which can be used to target both linux and windows containers.

Docker and compose-compose are required for this step (both included in [docker for windows](https://docs.docker.com/docker-for-windows/)).

#### Using Linux containers

From the stress folder:

```powershell
PS> .\run-docker-compose.ps1 -b
```

```bash
$ ./run-docker-compose.sh -b
```

This will build libraries and stress suite to a linux docker image and initialize a stress run using docker-compose.

#### Using Windows containers

Before we get started, please see
[docker documentation](https://docs.docker.com/docker-for-windows/#switch-between-windows-and-linux-containers)
on how windows containers can be enabled on your machine.
Once ready, simply run:

```powershell
PS> .\run-docker-compose.ps1 -b -w
```

For more details on how the `run-docker-compose.ps1` script can be used:

```powershell
Get-Help .\run-docker-compose.ps1
```