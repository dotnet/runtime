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
    local hasMinimum=false
    if machine_has "curl"; then
        hasMinimum=true;
    fi
    if machine_has "wget"; then
        hasMinimum=true;
    fi
    if [ "$hasMinimum" = "false" ]; then
        say_err "curl (recommended) or wget are required to download dotnet. Install missing prereq to proceed."
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
    which curl > /dev/null 2> /dev/null
    if [ $? -ne 0 ]; then
        echo "using wget for download"
        if [ -z "$out_path" ]; then
            wget -q --tries 10 $remote_path || failed=true
        else
            wget -q --tries 10 -O $out_path $remote_path || failed=true
        fi
    else 
        echo "using curl for download"
        if [ -z "$out_path" ]; then
            curl --retry 10 -sSL --create-dirs $remote_path || failed=true
        else
            curl --retry 10 -sSL --create-dirs -o $out_path $remote_path || failed=true
        fi
    fi
    
    if [ "$failed" = true ]; then
        say_err "Download failed"
        return 1
    fi
}

verbose=false
repoRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
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
    mkdir -p $toolsLocalPath
fi

if [ $forcedCliLocalPath = "<none>" ]; then
    check_min_reqs

    # download CLI boot-strapper script
    # temporary hack for custom dotnet-install script which allows curl or wget use
    # download "https://raw.githubusercontent.com/dotnet/cli/$dotNetInstallBranch/scripts/obtain/dotnet-install.sh" "$dotnetInstallPath"
    if [ ! -d $toolsLocalPath ]; then
        mkdir -p $toolsLocalPath
    fi
    cp -fv $repoRoot/dotnet-install.sh $dotnetInstallPath
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
    if [ "$?" != "0" ]; then
        say_err "The .NET CLI installation failed with exit code $?"
        exit $?
    fi
fi

cp $rootToolVersions $bootstrapComplete

say "Bootstrap finished successfully."

