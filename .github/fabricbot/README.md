# FabricBot scripts

Contains scripts used for generating FabricBot automation for the area pod issue/PR project boards. Scripts require nodejs to run:

```bash
$ node scripts/updateAreaPodConfigs.js
```

or if your system has `make`

```bash
$ make
```

Running the script will generate JSON configuration files under the `generated/` subfolder. The generated files are being tracked by git to simplify auditing changes of the generator script. When making changes to the generator script, please ensure that you have run the script and have committed the new generated files.

Please note that the generated files themselves have no impact on live FabricBot configuration. The changes need to be merged into the `.github/fabricbot.json` file at the root of the `runtime` and `dotnet-api-docs` repos. Merging the generated config into those files relies on manual editing to preserve other configuration blocks not affected by this script.
