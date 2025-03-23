[CmdletBinding()]
param(
    [string]
    [Parameter(Mandatory)]
    $thumbprintList
)

$thumbprints = $thumbprintList -split ','
$store = Get-Item -Path Cert:\CurrentUser\My
$store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
foreach ($thumbprint in $thumbprints)
{
    $cert = $store.Certificates.Find([System.Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint, $thumbprint, $false)
    if ($null -eq $cert)
    {
        Write-Host "Certificate with thumbprint '$thumbprint' not found in the user store."
    }
    $store.RemoveRange($cert)
    Write-Host "Removed certificate '$thumbprint'"
}
$store.Close()
Write-Host "Successfully removed diagnostic certificates"
