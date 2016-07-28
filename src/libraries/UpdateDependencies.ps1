#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# This script updates all the project.json files with the latest build version,
# and then creates a Pull Request for the change.

param(
    [Parameter(Mandatory=$true)][string]$GitHubUser,
    [Parameter(Mandatory=$true)][string]$GitHubEmail,
    [Parameter(Mandatory=$true)][string]$GitHubPassword,
    [Parameter(Mandatory=$true)][string]$VersionFileUrl,
    [string[]]$DirPropsVersionElements = 'CoreFxExpectedPrerelease',
    [string]$GitHubUpstreamOwner='dotnet', 
    [string]$GitHubOriginOwner=$GitHubUser,
    [string]$GitHubProject='corefx',
    [string]$GitHubUpstreamBranch='master',
    # a semi-colon delimited list of GitHub users to notify on the PR
    [string]$GitHubPullRequestNotifications='')

$LatestVersion = Invoke-WebRequest $VersionFileUrl -UseBasicParsing
$LatestVersion = $LatestVersion.ToString().Trim()

# Make a nicely formatted string of the dir props version elements. Short names, joined by commas.
$DirPropsVersionNames = ($DirPropsVersionElements | %{ $_ -replace 'ExpectedPrerelease', '' }) -join ', '

# Updates the dir.props file with the latest build number
function UpdateValidDependencyVersionsFile
{
    if (!$LatestVersion)
    {
        Write-Error "Unable to find latest dependency version at $VersionFileUrl ($DirPropsVersionNames)"
        return $false
    }

    $DirPropsPath = "$PSScriptRoot\dir.props"
    
    $DirPropsContent = Get-Content $DirPropsPath | % {
        $line = $_
        $DirPropsVersionElements | % {
            $line = $line -replace `
                "<$_>.*</$_>", `
                "<$_>$LatestVersion</$_>"
        }
        $line
    }
    Set-Content $DirPropsPath $DirPropsContent

    return $true
}

# Updates all the project.json files with out of date version numbers
function RunUpdatePackageDependencyVersions
{
    cmd /c $PSScriptRoot\build.cmd managed /t:UpdateInvalidPackageVersions | Out-Host

    return $LASTEXITCODE -eq 0
}

# Creates a Pull Request for the updated version numbers
function CreatePullRequest
{
    $GitStatus = git status --porcelain
    if ([string]::IsNullOrWhiteSpace($GitStatus))
    {
        Write-Warning "Dependencies are currently up to date"
        return $true
    }

    $CommitMessage = "Updating $DirPropsVersionNames dependencies to $LatestVersion"

    $env:GIT_COMMITTER_NAME = $GitHubUser
    $env:GIT_COMMITTER_EMAIL = $GitHubEmail
    git commit -a -m "$CommitMessage" --author "$GitHubUser <$GitHubEmail>" | Out-Host

    $RemoteUrl = "github.com/$GitHubOriginOwner/$GitHubProject.git"
    $RemoteBranchName = "UpdateDependencies$([DateTime]::UtcNow.ToString('yyyyMMddhhmmss'))"
    $RefSpec = "HEAD:refs/heads/$RemoteBranchName"

    Write-Host "git push https://$RemoteUrl $RefSpec"
    # pipe this to null so the password secret isn't in the logs
    git push "https://$($GitHubUser):$GitHubPassword@$RemoteUrl" $RefSpec 2>&1 | Out-Null

    if ($GitHubPullRequestNotifications)
    {
        $PRNotifications = $GitHubPullRequestNotifications.Split(';', [StringSplitOptions]::RemoveEmptyEntries) -join ' @'
        $PRBody = "/cc @$PRNotifications"
    }
    else
    {
        $PRBody = ''
    }

    $CreatePRBody = @"
    {
        "title": "$CommitMessage",
        "body": "$PRBody",
        "head": "$($GitHubOriginOwner):$RemoteBranchName",
        "base": "$GitHubUpstreamBranch"
    }
"@

    $CreatePRHeaders = @{'Accept'='application/vnd.github.v3+json'; 'Authorization'="token $GitHubPassword"}

    try
    {
        Invoke-WebRequest https://api.github.com/repos/$GitHubUpstreamOwner/$GitHubProject/pulls -UseBasicParsing -Method Post -Body $CreatePRBody -Headers $CreatePRHeaders
    }
    catch
    {
        Write-Error $_.ToString()
        return $false
    }

    return $true
}

if (!(UpdateValidDependencyVersionsFile))
{
    Exit -1
}

if (!(RunUpdatePackageDependencyVersions))
{
    Exit -1
}

if (!(CreatePullRequest))
{
    Exit -1
}

Write-Host -ForegroundColor Green "Successfully updated dependencies from the latest build numbers"

exit $LastExitCode
