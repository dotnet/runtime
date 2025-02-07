[CmdletBinding()]
param(
    [string]
    [Parameter(Mandatory)]
    $certList
)
# Required for the pipeline logging functions
$ci = $true
. $PSScriptRoot/../common/pipeline-logging-functions.ps1

$certs = $certList -split ','
$thumbprints = @()
$certCollection = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection
foreach ($cert in $certs)
{
    $certBytes = [System.Convert]::FromBase64String($(Get-Item "Env:$cert").Value)
    $certCollection.Import($certBytes,$null, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet)
}

foreach ($cert in $certCollection)
{
    Write-Host "Installed certificate '$($cert.Thumbprint)' with subject: '$($cert.Subject)'"
    $thumbprints += $cert.Thumbprint
}

$store = Get-Item -Path Cert:\CurrentUser\My
$store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
$store.AddRange($certCollection)
$store.Close()

Write-PipelineSetVariable -name "DacCertificateThumbprints" -Value "$($thumbprints -join ',')" -IsMultiJobVariable $false
Write-Host "Successfully installed diagnostic certificates"
