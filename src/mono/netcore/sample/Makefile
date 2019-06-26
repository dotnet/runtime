all: build-sample

build-sample:
	dotnet build HelloWorld

# COREHOST_TRACE=1 
VERSION:=3.0.0-preview4-27514-06

run:
	COMPlus_DebugWriteToStdErr=1 ../dotnet --fx-version "$(VERSION)" HelloWorld/bin/Debug/netcoreapp3.0/HelloWorld.dll

clean:
	rm -rf sdk shared host dotnet LICENCE.txt ThirdPartyNotices.txt
