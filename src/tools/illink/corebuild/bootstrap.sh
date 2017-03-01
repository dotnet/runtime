#!/usr/bin/env bash

# Stop script on NZEC
# set -e
# Stop script if unbound variable found (use ${var:-} if intentional)
set -u
# By default cmd1 | cmd2 returns exit code of cmd2 regardless of cmd1 success
# This is causing it to fail
set -o pipefail

# Use in the the functions: eval $invocation
invocation='say_verbose "Calling: ${FUNCNAME[0]}"'

# standard output may be used as a return value in the functions
# we need a way to write text on the screen in the functions so that
# it won't interfere with the return value.
# Exposing stream 3 as a pipe to standard output of the script itself
exec 3>&1

say_err() {
    printf "%b\n" "bootstrap: Error: $1" >&2
}

say() {
    # using stream 3 (defined in the beginning) to not interfere with stdout of functions
    # which may be used as return value
    printf "%b\n" "bootstrap: $1" >&3
}

say_verbose() {
    if [ "$verbose" = true ]; then
        say "$1"
    fi
}

machine_has() {
    eval $invocation
    
    hash "$1" > /dev/null 2>&1
    return $?
}

check_min_reqs() {
    if ! machine_has "curl"; then
        say_err "curl is required to download dotnet. Install curl to proceed."
        return 1
    fi
    
    return 0
}

# args:
# remote_path - $1
# [out_path] - $2 - stdout if not provided
download() {
    eval $invocation
    
    local remote_path=$1
    local out_path=${2:-}

    local failed=false
    if [ -z "$out_path" ]; then
        curl --retry 10 -sSL --create-dirs $remote_path || failed=true
    else
        curl --retry 10 -sSL --create-dirs -o $out_path $remote_path || failed=true
    fi
    
    if [ "$failed" = true ]; then
        say_err "Download failed"
        return 1
    fi
}

verbose=false
repoRoot=`pwd`
toolsLocalPath="<auto>"
cliLocalPath="<auto>"
symlinkPath="<auto>"
sharedFxVersion="<auto>"
force=
forcedCliLocalPath="<none>"
architecture="<auto>"
dotNetInstallBranch="rel/1.0.0"

while [ $# -ne 0 ]
do
    name=$1
    case $name in
        -r|--repositoryRoot|-[Rr]epositoryRoot)
            shift
            repoRoot="$1"
            ;;
        -t|--toolsLocalPath|-[Tt]oolsLocalPath)
            shift
            toolsLocalPath="$1"
            ;;
        -c|--cliInstallPath|--cliLocalPath|-[Cc]liLocalPath)
            shift
            cliLocalPath="$1"
            ;;
        -u|--useLocalCli|-[Uu]seLocalCli)
            shift
            forcedCliLocalPath="$1"
            ;;
        -a|--architecture|-[Aa]rchitecture)
            shift
            architecture="$1"
            ;;
        --dotNetInstallBranch|-[Dd]ot[Nn]et[Ii]nstall[Bb]ranch)
            shift
            dotNetInstallBranch="$1"
            ;;
        --sharedFrameworkSymlinkPath|--symlink|-[Ss]haredFrameworkSymlinkPath)
            shift
            symlinkPath="$1"
            ;;
        --sharedFrameworkVersion|-[Ss]haredFrameworkVersion)
            sharedFxVersion="$1"
            ;;
        --force|-[Ff]orce)
            force=true
            ;;
        -v|--verbose|-[Vv]erbose)
            verbose=true
            ;;
        *)
            say_err "Unknown argument \`$name\`"
            exit 1
            ;;
    esac

    shift
done

if [ $toolsLocalPath = "<auto>" ]; then
    toolsLocalPath="$repoRoot/Tools"
fi

if [ $cliLocalPath = "<auto>" ]; then
    if [ $forcedCliLocalPath = "<none>" ]; then
        cliLocalPath="$toolsLocalPath/dotnetcli"
    else
        cliLocalPath=$forcedCliLocalPath
    fi
fi

if [ $symlinkPath = "<auto>" ]; then
    symlinkPath="$toolsLocalPath/dotnetcli/shared/Microsoft.NETCore.App/version"
fi

rootToolVersions="$repoRoot/.toolversions"
bootstrapComplete="$toolsLocalPath/bootstrap.complete"

# if the force switch is specified delete the semaphore file if it exists
if [[ $force && -f $bootstrapComplete ]]; then
    rm -f $bootstrapComplete
fi

# if the semaphore file exists and is identical to the specified version then exit
if [[ -f $bootstrapComplete && ! `cmp $bootstrapComplete $rootToolVersions` ]]; then
    say "$bootstrapComplete appears to show that bootstrapping is complete.  Use --force if you want to re-bootstrap."
    exit 0
fi

initCliScript="dotnet-install.sh"
dotnetInstallPath="$toolsLocalPath/$initCliScript"

# blow away the tools directory so we can start from a known state
if [ -d $toolsLocalPath ]; then
    # if the bootstrap.sh script was downloaded to the tools directory don't delete it
    find $toolsLocalPath -type f -not -name bootstrap.sh -exec rm -f {} \;
else
    mkdir $toolsLocalPath
fi

if [ $forcedCliLocalPath = "<none>" ]; then
    check_min_reqs

    # download CLI boot-strapper script
    download "https://raw.githubusercontent.com/dotnet/cli/$dotNetInstallBranch/scripts/obtain/dotnet-install.sh" "$dotnetInstallPath"
    chmod u+x "$dotnetInstallPath"

    # load the version of the CLI
    rootCliVersion="$repoRoot/.cliversion"
    dotNetCliVersion=`cat $rootCliVersion`

    if [ ! -e $cliLocalPath ]; then
        mkdir -p "$cliLocalPath"
    fi

    # now execute the script
    say_verbose "installing CLI: $dotnetInstallPath --version \"$dotNetCliVersion\" --install-dir $cliLocalPath --architecture \"$architecture\""
    $dotnetInstallPath --version "$dotNetCliVersion" --install-dir $cliLocalPath --architecture "$architecture"
    if [ $? != 0 ]; then
        say_err "The .NET CLI installation failed with exit code $?"
        exit $?
    fi
fi

runtimesPath="$cliLocalPath/shared/Microsoft.NETCore.App"
if [ $sharedFxVersion = "<auto>" ]; then
    # OSX doesn't support --version-sort, https://stackoverflow.com/questions/21394536/how-to-simulate-sort-v-on-mac-osx
    sharedFxVersion=`ls $runtimesPath | sed 's/^[0-9]\./0&/; s/\.\([0-9]\)$/.0\1/; s/\.\([0-9]\)\./.0\1./g; s/\.\([0-9]\)\./.0\1./g' | sort -r | sed 's/^0// ; s/\.0/./g' | head -n 1`
fi

# create a junction to the shared FX version directory. this is
# so we have a stable path to dotnet.exe regardless of version.
junctionTarget="$runtimesPath/$sharedFxVersion"
junctionParent="$(dirname "$symlinkPath")"

if [ ! -d $junctionParent ]; then
    mkdir -p $junctionParent
fi

if [ ! -e $symlinkPath ]; then
    ln -s $junctionTarget $symlinkPath
fi

# create a project.csproj for the packages to restore
projectCsproj="$toolsLocalPath/project.csproj"
pcContent="<Project Sdk=\"Microsoft.NET.Sdk\"> <PropertyGroup> <TargetFramework>netcoreapp1.0</TargetFramework> </PropertyGroup> <ItemGroup>"
while read v; do
    IFS='=' read -r -a line <<< "$v"
    pcContent="$pcContent <PackageReference Include=\"${line[0]}\" Version=\"${line[1]}\" />"
done <$rootToolVersions
pcContent="$pcContent </ItemGroup> </Project>"
echo $pcContent > $projectCsproj

# now restore the packages
buildToolsSource="${BUILDTOOLS_SOURCE:-https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json}"
nugetOrgSource="https://api.nuget.org/v3/index.json"

packagesPath="$repoRoot/packages"
dotNetExe="$cliLocalPath/dotnet"
restoreArgs="restore $projectCsproj --packages $packagesPath --source $buildToolsSource --source $nugetOrgSource"
say_verbose "Running $dotNetExe $restoreArgs"
$dotNetExe $restoreArgs
if [ $? != 0 ]; then
    say_err "project.csproj restore failed with exit code $?"
    exit $?
fi
# now stage the contents to tools directory and run any init scripts
while read v; do
    echo hi $v
    IFS='=' read -r -a line <<< "$v"
    # verify that the version we expect is what was restored
    pkgVerPath="$packagesPath/${line[0]}/${line[1]}"
    if [ ! -d $pkgVerPath ]; then
        say_err "Directory $pkgVerPath doesn't exist, ensure that the version restore matches the version specified."
        exit 1
    fi
    # at present we have the following conventions when staging package content:
    #   1.  if a package contains a "tools" directory then recursively copy its contents
    #       to a directory named the package ID that's under $ToolsLocalPath.
    #   2.  if a package contains a "libs" directory then recursively copy its contents
    #       under the $ToolsLocalPath directory.
    #   3.  if a package contains a file "lib\init-tools.cmd" execute it.
    if [ -d "$pkgVerPath/tools" ]; then
        destination="$toolsLocalPath/${line[0]}"
        mkdir -p $destination
        cp -r $pkgVerPath/tools/* $destination
    fi
    if [ -d "$pkgVerPath/lib" ]; then
        cp -r $pkgVerPath/lib/* $toolsLocalPath
    fi
    if [ -f "$pkgVerPath/lib/init-tools.sh" ]; then
        echo "$pkgVerPath/lib/init-tools.sh" "$repoRoot" "$dotNetExe" "$toolsLocalPath" > "init-${line[0]}.log"
        "$pkgVerPath/lib/init-tools.sh" "$repoRoot" "$dotNetExe" "$toolsLocalPath" > "init-${line[0]}.log"
        exitCode=$?
        if [ $exitCode != 0 ]; then
            echo ERROR: "$pkgVerPath/lib/init-tools.sh" "$repoRoot" "$dotNetExe" "$toolsLocalPath" returned $exitCode
        fi
    fi
done <$rootToolVersions

cp $rootToolVersions $bootstrapComplete

say "Bootstrap finished successfully."

