[CmdletBinding()]
param(
    [string]
    [Parameter(Mandatory)]
    $esrpClient,
    [Parameter(ValueFromRemainingArguments=$true)][string[]]$filesToSign
)

[array]$signEntries = $filesToSign | ForEach-Object {
    @{
        SourceLocation = $_
    }
}

$inputFile = Get-Content -Raw $PSScriptRoot/signing/input.template.json | ConvertFrom-Json
$inputFile.SignBatches[0].SignRequestFiles = $signEntries

$inputJson = [System.IO.Path]::GetTempFileName()
# Our JSON goes up to 6 levels deep, so we need to set the depth to 6
# to successfully round-trip our JSON through ConvertTo-Json
$inputFile | ConvertTo-Json -Depth 6 | Out-File -FilePath $inputJson -Encoding utf8

$outputJson = "$PSScriptRoot/../../artifacts/log/Release/signing-$(New-Guid).json.log"

New-Item $outputJson -ItemType File

$outputJson = Resolve-Path $outputJson

Write-Host "Signing files with DAC certificate"
Write-Host "Logging output to $outputJson"

& $esrpClient sign -a $PSScriptRoot/signing/auth.json -c $PSScriptRoot/signing/config.json -i $inputJson -o $outputJson -p $PSScriptRoot/signing/policy.json

# Validate that the files are signed correctly
foreach ($file in $filesToSign) {
    $signingCert = $(Get-AuthenticodeSignature $file).SignerCertificate
    if ($null -eq $signingCert)
    {
      throw "File $file does not contain a signature."
    }

    if ($signingCert.Subject -ne "CN=.NET DAC, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" `
        -or $signingCert.Issuer -ne "CN=Microsoft Code Signing PCA 2010, O=Microsoft Corporation, L=Redmond, S=Washington, C=US")
    {
      throw "File $file not in expected trust chain."
    }

    $certEKU = $signingCert.Extensions.Where({ $_.Oid.FriendlyName -eq "Enhanced Key Usage" }) | Select-Object -First 1

    if ($certEKU.EnhancedKeyUsages.Where({ $_.Value -eq "1.3.6.1.4.1.311.84.4.1" }).Count -ne 1)
    {
      throw "Signature for $file does not contain expected EKU."
    }

    Write-Host "$file is correctly signed."
}
