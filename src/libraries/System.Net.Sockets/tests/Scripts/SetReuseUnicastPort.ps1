# The values of '50000' and '50' used in this script are examples. They should be changed based on the machine's
# actual requirements.
Set-nettcpsetting –settingname internetcustom –autoreuseportrangestartport 50000 –autoreuseportrangenumberofports 50
Get-nettcpsetting –settingname internetcustom
