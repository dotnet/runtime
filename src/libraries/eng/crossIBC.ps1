#This script is for applying IBC data to Linux binaries on Windows. This is because currently IBCMerge does not run on Linux.
#It assumes that the binary output of a Linux build will live in a directory called Linux in the root of the repo, and it leaves
#the optimized binaries in a directory called Optimized.
param(
    [Parameter(Mandatory=$true)][string]$ToolSource,
    [Parameter(Mandatory=$true)][string]$ToolUser,
    [Parameter(Mandatory=$true)][string]$ToolPAT,
    [Parameter(Mandatory=$true)][string]$RepoRoot,
    [bool]$UsePartialNgen
)

#There seems to be a bug in the optional tool restore step that causes those packages to be downloaded to the root of the drive.
#That is why we have to look in both the root and the packages directory for IBCMerge.
function Find-IBCMerge {
    $IBCMergePath = gci -recurse .\.packages | where {$_.Name.Contains("ibcmerge.exe")}
    if(!$IBCMergePath)
    {
        Write-Error -Category ResourceUnavailable "Could not find IBCMerge.exe"
        exit
    }
    $IBCMergePath.FullName
}

#Both the Preprocess-IBCMerge and Apply-IBCData are taken directly from the steps done in codeOptimization.targets
function Preprocess-IBCMerge
{
    foreach($file in gci -recurse .packages\optimization.linux-x64.ibc.corefx\*.dll)
    {
        & $IBCMerge -q -f -delete -mo $file.FullName $file.FullName.Replace(".dll", ".ibc")
        & $IBCMerge -mi $file.FullName
    }
}

function Apply-IBCData
{
    $pwd = (Get-Item -Path ".\").FullName
    foreach($file in gci -recurse .packages\optimization.linux-x64.ibc.corefx\*.dll)
    {
        #Here we look through all of the Linux binaries for the implementation dll that matches in name to the IBC data
        $fileToOptimize = gci -recurse Linux\*Linux*\*.dll | where {$_.Name.Contains($file.Name)} | where {$_.FullName.Contains("netcoreapp")}
        if(!$fileToOptimize)
        {
            $fileToOptimize = gci -recurse Linux\*Unix*\*.dll | where {$_.Name.Contains($file.Name)} | where {$_.FullName.Contains("netcoreapp")}
        }
        if(!$fileToOptimize)
        {
            $fileToOptimize = gci -recurse Linux\*AnyOS*\*.dll | where {$_.Name.Contains($file.Name)} | where {$_.FullName.Contains("netcoreapp")} | where {!$_.FullName.Contains("netfx")}
        }
        if($fileToOptimize)
        {
            if(!$UsePartialNgen)
            {
                Write-Host "$IBCMerge -q -f -mo $fileToOptimize -incremental $file"
                & $IBCMerge -q -f -mo $fileToOptimize -incremental $file
            }
            else
            {
                & $IBCMerge -q -f -mo $fileToOptimize -incremental $file -partialNGEN -minify
            }
            $copyLocation = Join-Path -Path $pwd -ChildPath "Optimized"
            Write-Host "Copy-Item $fileToOptimize -Destination $copyLocation"
            Copy-Item $fileToOptimize -Destination $copyLocation
        }
    }
}

pushd $RepoRoot
.\build.cmd -restore /p:OptionalToolSource=$ToolSource /p:OptionalToolSourceUser=$ToolUser /p:OptionalToolSourcePassword=$ToolPAT /p:EnableProfileGuidedOptimization=true /p:IBCTarget=Linux -release -ci
mkdir Optimized
$IBCMerge = Find-IBCMerge
Preprocess-IBCMerge
Apply-IBCData
popd