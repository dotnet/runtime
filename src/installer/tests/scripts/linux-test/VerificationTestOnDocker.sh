#!/usr/bin/env bash

InstallationTestResult="result.txt"
InstallationTestLogFile="logfile.txt"
ImagesFile="images.txt"

current_user=$(whoami)
if [ $current_user != "root" ]; then
    echo "test.sh requires superuser privileges to run"
    exit 1
fi

if [ -e $InstallationTestLogFile ]; then
	rm $InstallationTestLogFile -f
fi

if [[ "$1" == "sdk" ]]; then
	InstallationScript="SdkInstallation.sh"
	echo -e ".NET SDK verification test result\n" > $InstallationTestResult
elif [[ "$1" == "runtime" ]]; then
	InstallationScript="RuntimeInstallation.sh"
	echo -e ".NET Runtime verification test result\n" > $InstallationTestResult
fi

while IFS='' read -r image || [[ -n "$image" ]]; do
	echo $image
	docker run --rm -v $(pwd):/docker -t $image /bin/bash /docker/$InstallationScript $2 $3 $4
done <$ImagesFile

cat $InstallationTestResult >> $InstallationTestLogFile
