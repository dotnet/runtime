$artifacts = 'artifacts'
$7z_zip = Join-Path -Path $artifacts -ChildPath '7za-win-x64.zip'
$7z_folder =  Join-Path -Path $artifacts -ChildPath '7za-win-x64'
$client = new-object System.Net.WebClient
$client.DownloadFile("https://public-stevedore.unity3d.com/r/public/7za-win-x64/38c5b39be2e8_a333cfccb708c88459b3812eb2597ca486ec9b416172543ca3ef8e5cd5f80984.zip",$7z_zip)
Expand-Archive -Path $7z_zip -DestinationPath $7z_folder