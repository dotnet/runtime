Set-PSDebug -Trace 1

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$uri = "https://runtimesigningtest.blob.core.windows.net/runtimesigningtest/IntermediateUnsignedArtifacts_minimal.zip";
$zipLocation = "$env:AGENT_TEMPDIRECTORY\IntermediateUnsignedArtifacts.zip";
Invoke-WebRequest -Uri $uri -OutFile $zipLocation;
Expand-Archive -Path $zipLocation -DestinationPath "$env:BUILD_SOURCESDIRECTORY\artifacts\PackageDownload";