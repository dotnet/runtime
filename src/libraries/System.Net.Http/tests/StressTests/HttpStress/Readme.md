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

### Running with local corefx builds

Note that the stress suite will test the sdk build available in the available,
that is to say it will not necessarily test the implementation of the local corefx repo.
To achieve this, you will need to point your environment to the [`testhost` build of corefx](https://github.com/dotnet/coreclr/blob/master/Documentation/building/testing-with-corefx.md).

Using powershell on windows:

```powershell
# Build corefx from source
PS> .\build.sh -c Release
# Load the testhost sdk in the current environment, must match build configuration
PS> . .\src\System.Net.Http\tests\StressTests\HttpStress\load-corefx-testhost.ps1 -c Release
# run the stress suite with the new bits
PS> cd .\src\System.Net.Http\tests\StressTests\HttpStress ; dotnet run --runtime win10-x64 
```

Note that the `--runtime` argument is necessary because `testhost` 
does not bundle the required aspnetcore runtime dependencies.
This will force the sdk to install the relevant native bits from nuget.

Equivalently using bash on linux:

```bash
# Build corefx from source
$ ./build.sh -c Release
# Load the testhost sdk in the current environment, must match build configuration
$ source src/System.Net.Http/tests/StressTests/HttpStress/load-corefx-testhost.sh -c Release
# run the stress suite with the new bits
$ cd src/System.Net.Http/tests/StressTests/HttpStress && dotnet run -r linux-x64 
```