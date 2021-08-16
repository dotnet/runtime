## SslStress

Provides stress testing scenaria for System.Net.Security.SslStream.

### Running the suite locally

Using the command line,

```bash
$ dotnet run -- <stress suite args>
```

To get the full list of available parameters:

```bash
$ dotnet run -- -help
```

### Running with local runtime builds

Note that the stress suite will test the sdk available in the environment,
that is to say it will not necessarily test the implementation of the local runtime repo.
To achieve this, we will first need to build a new sdk from source. This can be done [using docker](https://github.com/dotnet/runtime/blob/main/eng/docker/Readme.md).

### Running using docker-compose

The preferred way of running the stress suite is using docker-compose,
which can be used to target both linux and windows containers.
Docker and compose-compose are required for this step (both included in [docker for windows](https://docs.docker.com/docker-for-windows/)).

#### Using Linux containers

From the stress folder on powershell:

```powershell
PS> .\run-docker-compose.ps1 -b
```

This will build the libraries and stress suite to a linux docker image and initialize a stress run using docker-compose.

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
