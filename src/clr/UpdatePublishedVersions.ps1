#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# This script updates the dotnet/versions repository based on a set of packages. It directly
# commits the changes using GitHub APIs.

param(
    [Parameter(Mandatory=$true)][string]$gitHubUser,
    [Parameter(Mandatory=$true)][string]$gitHubEmail,
    [Parameter(Mandatory=$true)][string]$gitHubAuthToken,
    [Parameter(Mandatory=$true)][string]$versionsRepoOwner,
    [Parameter(Mandatory=$true)][string]$versionsRepo,
    [Parameter(Mandatory=$true)][string]$versionsRepoPath,
    # A pattern matching all packages in the set that the versions repository should be set to.
    [Parameter(Mandatory=$true)][string]$nupkgPath,
    # Print out the new file contents, but don't change the versions repository.
    [switch]$dryRun)

function ConvertPathTo-Package([string]$path)
{
    # Find the package ID and version using a regex. This matches the semantic version
    # and assumes everything to the left is the id or a path to the package directory.
    $matched = $path -match '^(.*\\)?(.*?)\.(([0-9]+\.)?[0-9]+\.[0-9]+(-([A-z0-9-]+))?)\.(symbols\.)?nupkg$'
    if ($matched)
    {
        $packageInfo = @{
            Path = $path
            Name = $matches[2]
            Version = $matches[3]
            Prerelease = $matches[6]
        }
        $packageInfo.NameVersion = "$($packageInfo.Name) $($packageInfo.Version)"
        return $packageInfo
    }
    else
    {
        throw "Couldn't find name and version from path $path."
    }
}

# Updates a GitHub file with the specified file contents
function Update-GitHub-File(
    [string]$user = $gitHubUser,
    [string]$email = $gitHubEmail,
    [string]$authToken = $gitHubAuthToken,
    [string]$owner = $versionsRepoOwner,
    [string]$repo = $versionsRepo,
    [string]$path,
    [string]$newFileContent,
    [string]$commitMessage)
{
    function message([string]$message)
    {
        Write-Host -ForegroundColor Green "*** $message ***"
    }

    $headers = @{
        'Accept' = 'application/vnd.github.v3+json'
        'Authorization' = "token $authToken"
    }

    $fileUrl = "https://api.github.com/repos/$owner/$repo/contents/$path"

    message "Getting the `"sha`" of the current contents of file '$owner/$repo/$path'"

    $currentFile = Invoke-WebRequest $fileUrl -UseBasicParsing -Headers $headers
    $currentSha = (ConvertFrom-Json $currentFile.Content).sha

    message "Got `"sha`" value of '$currentSha'"

    message "Request to update file '$owner/$repo/$path' contents to:"
    Write-Host $newFileContent

    if ($dryRun)
    {
        message 'Not sending request: dry run.'
        return
    }

    # Base64 encode the string
    function ToBase64([string]$value)
    {
       return [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($value))
    }

    $updateFileBody = 
@"
{
  "message": "$commitMessage",
  "committer": {
    "name": "$user",
    "email": "$email"
  },
  "content": "$(ToBase64 $newFileContent)",
  "sha": "$currentSha"
}
"@

    message 'Sending request...'
    $putResponse = Invoke-WebRequest $fileUrl -UseBasicParsing -Method PUT -Body $updateFileBody -Headers $headers

    if ($putResponse.StatusCode -ge 200 -and $putResponse.StatusCode -lt 300)
    {
        message 'Successfully updated the file'
    }
}

# Store result of Get-ChildItem before piping to ConvertPathTo-Package. When directly piping, exceptions are ignored.
$packagePaths = Get-ChildItem $nupkgPath
$packages = $packagePaths | %{ ConvertPathTo-Package $_ }

$prereleaseVersion = ''
foreach ($package in $packages)
{
    if ($package.Prerelease)
    {
        $prereleaseVersion = $package.Prerelease
        break
    }
}

if (!$prereleaseVersion)
{
    throw "Could not find a Prerelease version in '$newPackages'"
}

$versionFilePath = "$versionsRepoPath/Latest.txt"
$versionFileContent = "$prereleaseVersion`n"

Update-GitHub-File `
    -path $versionFilePath `
    -newFileContent $versionFileContent `
    -commitMessage "Update '$versionFilePath' with $prereleaseVersion" 

$packageInfoFilePath = "$versionsRepoPath/Latest_Packages.txt"
$packageInfoFileContent = ($packages | %{ $_.NameVersion } | Sort-Object) -join "`r`n"

Update-GitHub-File `
    -path $packageInfoFilePath `
    -newFileContent $packageInfoFileContent `
    -commitMessage "Adding package info to '$packageInfoFilePath' for $prereleaseVersion"
    