[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$uri = "https://runtimesigningtest.blob.core.windows.net/runtimesigningtest/IntermediateUnsignedArtifacts.zip";
$zipLocation = "$env:AGENT_TEMPDIRECTORY\IntermediateUnsignedArtifacts.zip";
Invoke-WebRequest -Uri $uri -OutFile $zipLocation;
Expand-Archive -Path $zipLocation -DestinationPath "$env:BUILD_SOURCESDIRECTORY\artifacts\PackageDownload";