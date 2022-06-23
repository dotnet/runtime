# .NET Core host probing

The dotnet host uses probing when it searches for actual file on disk for a given asset. This happens for example when it tries to resolve assembly specified in `.deps.json` to a file on disk. The `.deps.json` specifies relative path of the asset in its library and also the relative path to the library (typically package name and version). Consider this small portion of `.deps.json` for example (only relevant parts, everything else omitted):
```json
{
  "targets": {
    ".NETCoreApp,Version=v2.1": {
      "Newtonsoft.Json/11.0.2": {
        "runtime": {
          "lib/netstandard2.0/Newtonsoft.Json.dll": {}
        }
      }
    }
  },
  "libraries": {
    "Newtonsoft.Json/11.0.2": {
      "type": "package",
      "serviceable": true,
      "path": "newtonsoft.json/11.0.2",
    }
  }
}
```

The library relative path in this case is `newtonsoft.json/11.0.2` and the asset relative path is `lib/netstandard2.0/Newtonsoft.Json.dll`. So the goal of the probing logic is to find the `Newtonsoft.Json.dll` file using the above relative paths.

## Probing
The probing itself is done by going over a list of probing paths, which are ordered according to their priority. For each path, the host will append the relative parts of the path as per above and see if the file actually exists on the disk.
If the file is found, the probing is done, and the full path just resolved is stored.
If the file is not found, the probing continues with the next path on the list.
If all paths are tried and the asset is still not found this is reported as an error (with the exception of app's `.deps.json` asset, in which case it's ignored).

## Probing paths
The list of probing paths ordered according to their priority. First path in the list below is tried first and so on.
* Servicing paths
  Servicing paths are only used for serviceable assets, that is the corresponding library record must specify `serviceable: true`.
  The base servicing path is
    * On Windows x64 `%ProgramFiles(x86)%\coreservicing`
    * On Windows x86 `%ProgramFiles%\coreservicing`
    * Otherwise (Linux/Mac) `$CORE_SERVICING`

  Given the base servicing path, the probing paths are
    * Servicing NI probe path `<servicing base>/|arch|` - this is used only for `runtime` assets
    * Servicing normal probe path `<servicing base>/pkgs` - this is used for all assets

* The application (or framework if we're resolving framework assets) directory
* Framework directories
  If the app (or framework) has dependencies on frameworks, these frameworks are used as probing paths.
  The order is from the higher level framework to lower level framework. The app is considered the highest level, it direct dependencies are next and so on.
  For assets from frameworks, only that framework and lower level frameworks are considered.
  Note: These directories come directly out of the framework resolution process. Special note on Windows where global locations are always considered even if the app is not executed via the shared `dotnet.exe`. More details can be found in [Shared FX Lookup](sharedfx-lookup.md).
* Shared store paths
  * `$DOTNET_SHARED_STORE/|arch|/|tfm|` - The environment variable `DOTNET_SHARED_STORE` can contain multiple paths, in which case each is appended with `|arch|/|tfm|` and used as a probing path.
  * If the app is executed through `dotnet.exe` then path relative to the directory with the `dotnet.exe` is used
    * `<dotnet.exe path>/store/|arch|/|tfm|`
  * On Windows, the global shared store is used
    * If running in WOW64 mode - `%ProgramFiles(x86)%\dotnet\store\|arch|\|tfm|`
    * Otherwise - `%ProgramFiles%\dotnet\store\|arch|\|tfm|`
* Additional probing paths
  In these paths the `|arch|/|tfm|` string can be used and will be replaced with the actual values before using the path.
  * `--additionalprobingpath` command line arguments
  * `additionalProbingPaths` specified in `.runtimeconfig.json` and `.runtimeconfig.dev.json` for the app and each framework (highest to lowest)


  Note about framework-dependent and self-contained apps. With regard to probing the main difference is that self-contained apps don't have any framework dependencies, so all assets (including assemblies which normally come from a framework) are probed for in the app's directory.
