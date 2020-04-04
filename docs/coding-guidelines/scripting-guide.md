Scripting Guide
===============

Generally, for script files (.sh, .ps1 & .cmd), our current best guidance is consistency. When editing files, keep new code and changes consistent with the style in the files. For new files, it should conform to the style for that component. If there is a completely new component, anything that is reasonably broadly accepted is fine.

PowerShell
-----------------

### -NoProfile
Unlike Unix or Linux based shells, when PowerShell is invoked by default it executes [profile scripts](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_profiles), even on Unix or Linux systems. This feature may cause side effects which may affect executing code. To guard against this, it is a requirement to always invoke PowerShell with the `-NoProfile` option. For example, the following PowerShell command in valid:

```cmd
powershell -NoProfile "Get-ChildItem -path %__TestBinDir% -Include '*.ni.*' -Recurse -Force | Remove-Item -force"
```
