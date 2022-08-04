# download 7-zip
curl https://public-stevedore.unity3d.com/r/public/7za-mac-x64/e6c75fb7ffda_5bd76652986a0e3756d1cfd7e84ce056a9e1dbfc5f70f0514a001f724c0fbad2.zip --output artifacts/7za-mac-x64.zip
unzip artifacts/7za-mac-x64.zip -d artifacts/7za-mac-x64
# run the liponator script
mkdir -p ./artifacts/bin/microsoft.netcore.app.runtime.osx-x64arm64/Release/runtimes/osx-x64arm64
perl .yamato/scripts/the_liponator.pl ./artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Release/runtimes/osx-x64 ./artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Release/runtimes/osx-arm64 ./artifacts/bin/microsoft.netcore.app.runtime.osx-x64arm64/Release/runtimes/osx-x64arm64
cp LICENSE.md artifacts/bin/microsoft.netcore.app.runtime.osx-x64arm64/Release/runtimes/osx-x64arm64/.
artifacts/7za-mac-x64/7za a artifacts/unity/$ARTIFACT_FILENAME ./artifacts/bin/microsoft.netcore.app.runtime.osx-x64arm64/Release/runtimes/osx-x64arm64/*
