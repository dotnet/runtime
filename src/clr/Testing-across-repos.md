#How to run CoreFx tests on top of CoreCLR binaries
If you want to validate the changes you have made to the CoreCLR repo you can do that by running the CoreFx tests on top of the newly build binaries (from the CoreCLR repo)

In order to do this you need to create a file called `localpublish.props` under the `<repo root>\packages` folder.
The contents of the file should look like this (make sure to update the version to the current version of the CoreCLR package used by CoreFx):

	<Project ToolsVersion="12.0" DefaultTargets="Build" 
		     xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
      <ItemGroup>
    	<LocalPackages Include="$(PackagesBinDir)">
          <PackageName>Microsoft.DotNet.CoreCLR</PackageName>
          <PackageVersion>1.0.2-prerelease</PackageVersion>
          <InstallLocation><corefx repo root>\packages</InstallLocation>
        </LocalPackages>
      </ItemGroup>
    </Project>

Once this file is there, subsequent builds of the CoreCLR repo are going to install the CoreCLR package into the location specified by `InstallLocation`.

To run tests, follow the procedure for running tests in CoreFx