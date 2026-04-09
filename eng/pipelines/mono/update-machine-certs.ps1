# This seems to update the machine cert store so that python can download the files as required by emscripten's install
# Based on info at https://pypi.org/project/certifi/
python -m pip install --upgrade pip
pip install  --upgrade certifi pip-system-certs

function UpdateSite {

    param (
        $WebsiteURL
    )

    Try {
        $Conn = New-Object System.Net.Sockets.TcpClient($WebsiteURL,443)

        Try {
            $Stream = New-Object System.Net.Security.SslStream($Conn.GetStream())
            $Stream.AuthenticateAsClient($WebsiteURL)

            $Cert = $Stream.Get_RemoteCertificate()

            $ValidTo = [datetime]::Parse($Cert.GetExpirationDatestring())

            Write-Host "`nConnection Successful" -ForegroundColor DarkGreen
            Write-Host "Website: $WebsiteURL"
        }
        Catch { Throw $_ }
        Finally { $Conn.close() }
        }
        Catch {
                Write-Host "`nError occurred connecting to $($WebsiteURL)" -ForegroundColor Yellow
                Write-Host "Website: $WebsiteURL"
                Write-Host "Status:" $_.exception.innerexception.message -ForegroundColor Yellow
                Write-Host ""
    }
}

UpdateSite("storage.googleapis.com")
UpdateSite("www.sqlite.org")
