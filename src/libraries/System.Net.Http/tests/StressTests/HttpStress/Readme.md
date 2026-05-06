## HttpStress

Provides stress testing scenaria for System.Net.Http.HttpClient and the underlying SocketsHttpHandler.

### Running the suite locally

Prerequisite: the runtime and the libraries should be [live-built](https://github.com/dotnet/runtime/tree/main/docs/workflow/building/libraries) with `build.cmd`/`build.sh`.

Use the script `build-local.sh` / `build-local.ps1` to build the stress project against the live-built runtime. This will acquire the latest daily SDK, which is TFM-compatible with the live-built runtime.

```bash
$ build-local.sh [StressConfiguration] [LibrariesConfiguration]
```

The build script will also generate the runscript that runs the stress suite using the locally built testhost in the form of `run-stress-<StressConfiguration>-<LirariesConfiguration>.sh`. To run the tests with the script, assuming that both the stress project and the libraries have been built against Release configuration:

```bash
$ run-stress-Release-Release.sh [stress suite args]
```

To get the full list of available parameters:

```bash
$ run-stress-Release-Release.sh -help
```

### Building and running with Docker

A docker image containing the live-built runtime bits and the latest daily SDK is created with the [`build-docker-sdk.sh/ps1` scripts](https://github.com/dotnet/runtime/blob/main/eng/docker/Readme.md).

It's possible to manually `docker build` a docker image containing the stress project based on the docker image created with `build-docker-sdk.sh/ps1`, however the preferred way is to use docker-compose, which can be used to target both linux and windows containers.

Docker and compose-compose are required for this step (both included in [docker for windows](https://docs.docker.com/docker-for-windows/)).

#### Using Linux containers

From the stress folder:

```powershell
PS> .\run-docker-compose.ps1
```

```bash
$ ./run-docker-compose.sh
```

This will build libraries and stress suite to a linux docker image and initialize a stress run using docker-compose.

#### Using Windows containers

Before we get started, please see
[docker documentation](https://docs.docker.com/docker-for-windows/#switch-between-windows-and-linux-containers)
on how windows containers can be enabled on your machine.
Once ready, simply run:

```powershell
PS> .\run-docker-compose.ps1 -w
```

For more details on how the `run-docker-compose.ps1` script can be used:

```powershell
Get-Help .\run-docker-compose.ps1
```

#### Passing arguments to HttpStress

The following will run the stress client and server containers passing the argument `-http 2.0` to both:

```bash
./run-docker-compose.sh -clientstressargs "-http 2.0" -serverstressargs "-http 2.0"
```

```powershell
./run-docker-compose.sh -w -clientStressArgs "-http 2.0" -serverStressArgs "-http 2.0"
```