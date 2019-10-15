# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

#Requires -RunAsAdministrator

# Certificate configuration

$script:testDataUri = "https://github.com/dotnet/corefx-testdata/archive/master.zip" 
$script:testData = "corefx-testdata"
$script:certificatePath = "$($script:testData)\corefx-testdata-master\System.Net.TestData"

$script:clientPrivateKeyPath = Join-Path $script:certificatePath "testclient1_at_contoso.com.pfx"
$script:clientPrivateKeyPassword = "testcertificate"

$script:serverPrivateKeyPath = Join-Path $script:certificatePath "contoso.com.pfx"
$script:serverPrivateKeyPassword = "testcertificate"

Function GetFullPath($relativePath)
{
    return (Get-Item $relativePath).FullName
}

Function DeleteTestData
{
    if (Test-Path $script:testData)
    {
        rmdir $script:testData -Recurse -Force 
    }
    
    del ($testData + ".zip") -ErrorAction SilentlyContinue
}

Function DownloadTestData
{
    DeleteTestData
    DownloadFile $script:testDataUri ($testData + ".zip")
    Expand-Archive ($testData + ".zip")
}

Function LoadCertificateAndRoot($fileName, $password)
{
    $privateCerts = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection
    $fullPath = GetFullPath $fileName
   
    $privateCerts.Import($fullPath, $password, ("MachineKeySet", "PersistKeySet", "Exportable"))

    $privateKeyCert = $null
    foreach ($cert in $privateCerts)
    {
        if ($privateKeyCert -eq $null -and $cert.HasPrivateKey)
        {
            $privateKeyCert = $cert
        }
    }

    $rootCACert = $privateCerts | where {$_.Subject -eq $privateKeyCert.Issuer}

    return ($privateKeyCert, $rootCACert)
}

Function AddCertificateToStore($certificate, $storeName, $storeLocation)
{
    $rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store($storeName, $storeLocation)
    $rootStore.Open("ReadWrite")
    $rootStore.Add($certificate)
    $rootStore.Close()
}

Function InstallCertificates($fileName, $password)
{
    Write-Host "Acquiring test data."
    DownloadTestData

    Write-Host "Adding certificates"
    ($private, $root) = LoadCertificateAndRoot $fileName $password

    Write-Host -ForegroundColor DarkGray "`tAdding root certificate: $($root.Subject)"
    AddCertificateToStore $root "Root" "LocalMachine"

    Write-Host -ForegroundColor DarkGray "`tAdding private key certificate: $($private.Subject)"
    AddCertificateToStore $private "My" "LocalMachine"

    Write-Host "Removing temporary files"
    DeleteTestData
}

Function InstallClientCertificates
{
    Write-Host -ForegroundColor Cyan "Installing Client Certificates"
    InstallCertificates $script:clientPrivateKeyPath $script:clientPrivateKeyPassword
}

Function InstallServerCertificates
{
    Write-Host -ForegroundColor Cyan "Installing Server Certificates"
    InstallCertificates $script:serverPrivateKeyPath $script:serverPrivateKeyPassword
}

Function GetServerCertificate
{
    return dir Cert:\LocalMachine\My | where { $_.DnsNameList | where{$_.Punycode -eq $script:iisServerFQDN} }
}

Function RemoveCertificates($filename, $password)
{
    Write-Host "Acquiring test data."
    DownloadTestData
    ($private, $root) = LoadCertificateAndRoot $fileName $password
    
    Write-Host -ForegroundColor DarkGray "`tRemoving root certificate: $($root.Subject)"
    dir Cert:\LocalMachine\Root | where {$_.Subject -eq $root.Subject} | foreach { rm (Join-Path Cert:\LocalMachine\Root $_.Thumbprint) }
    Write-Host -ForegroundColor DarkGray "`tRemoving private key certificate: $($private.Subject)"
    dir Cert:\LocalMachine\My | where {$_.Subject -eq $private.Subject} | foreach { rm (Join-Path Cert:\LocalMachine\My $_.Thumbprint) -DeleteKey }
    
    DeleteTestData
}

Function RemoveClientCertificates
{
    Write-Host -ForegroundColor Cyan "Removing Client Certificates"
    RemoveCertificates $script:clientPrivateKeyPath $script:clientPrivateKeyPassword
}

Function RemoveServerCertificates
{
    Write-Host -ForegroundColor Cyan "Removing Server Certificates"
    RemoveCertificates $script:serverPrivateKeyPath $script:serverPrivateKeyPassword
}