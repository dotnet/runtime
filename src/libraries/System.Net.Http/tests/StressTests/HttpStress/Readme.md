## HttpStress

Provides stress testing scenaria for System.Net.HttpClient, with emphasis on the HTTP/2 implementation of SocketsHttpHandler.

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

Note that the stress suite will test the sdk build available in the available,
that is to say it will not necessarily test the implementation of the local runtime repo.
To achieve this, you will need to point your environment to the [`testhost` build](https://github.com/dotnet/runtime/blob/master/docs/coreclr/building/testing-with-corefx.md).

Using powershell on windows:

```powershell
# Build runtime libraries from source
PS> .\libraries.sh -c Release
# Load the testhost sdk in the current environment, must match build configuration
PS> . .\src\libraries\System.Net.Http\tests\StressTests\HttpStress\load-corefx-testhost.ps1 -c Release
# run the stress suite with the new bits
PS> cd .\src\libraries\System.Net.Http\tests\StressTests\HttpStress ; dotnet run -c Release -- <stress args>
```

Equivalently using bash on linux:

```bash
# Build runtime libraries from source
$ ./libraries.sh -c Release
# Load the testhost sdk in the current environment, must match build configuration
$ source src/libraries/System.Net.Http/tests/StressTests/HttpStress/load-corefx-testhost.sh -c Release
# run the stress suite with the new bits
$ cd src/libraries/System.Net.Http/tests/StressTests/HttpStress && dotnet run -- <stress args>
```

### Running with docker

To run the stress suite in docker:

```bash
$ cd src/libraries/System.Net.Http/tests/StressTests/HttpStress
$ docker build -t httpstress .
$ docker run --rm httpstress
```

This will build the stress suite using the `mcr.microsoft.com/dotnet/core/sdk` base image,
however that can be overriden using the `SDK_BASE_IMAGE` build argument:

```bash
$ docker build -t httpstress \
    --build-arg SDK_BASE_IMAGE=my-sdk-3.1.100-preview1 \
    .
```

This should work with any base image with a dotnet sdk supporting `netcoreapp3.0`.

#### Using runtime bits

To containerize httpstress using current runtime source code, from the root of the runtime repo do:
```bash
$ docker build -t sdk-corefx-current \
    --build-arg BUILD_CONFIGURATION=Debug \
    -f src/libraries/System.Net.Http/tests/StressTests/HttpStress/corefx.Dockerfile \
    .
```
Then as before build the stress suite using the image we just built as our base image:
```bash
$ cd src/libraries/System.Net.Http/tests/StressTests/HttpStress/
$ docker build -t httpstress \
    --build-arg SDK_BASE_IMAGE=sdk-corefx-current \
    .
```

### Orchestrating with docker-compose

Once the httpstress image has been built successfully, 
it is possible to orchestrate stress runs with client and server deployed to separate containers
using docker-compose. 
To do this, from the stress folder simply run
```bash
$ docker-compose up
```
Parameters of the stress run can be tuned by setting environment variables:
```bash
$ export HTTPSTRESS_CLIENT_ARGS='-maxExecutionTime 20'
$ export HTTPSTRESS_SERVER_ARGS='-aspnetlog'
$ docker-compose up
```
