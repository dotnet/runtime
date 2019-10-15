# Roll Forward On No Candidate Fx

## Default Behavior

The desired framework version is defined through a configuration json file (appname.runtimeconfig.json).

If the version specified is a _production_ version, the default behavior is:
1) Pick the nearest _production_ version based on [minor].[patch]
2) If none available, pick the nearest _preview_ version based on [minor].[patch]
3) Once the nearest compatible version is found, roll-forward to the latest version based on [patch]-[name]-[build]

```
	For instance:
	
	Desired version: 1.0.1
	Available versions: 1.0.0, 1.0.1, 1.0.2, 1.0.3, 1.1.0, 1.1.1, 2.0.1
	Chosen version: 1.0.3
	
	Desired version: 1.0.1
	Available versions: 1.0.0, 1.1.0-preview1-x, 1.1.0-preview2-x, 1.2.0-preview1-x
	Chosen version: 1.1.0-preview2-x
	
        Desired version: 1.0.1
	Available versions: 1.0.0, 1.1.0-preview1-x, 1.2.0, 1.2.1-preview1-x
	Chosen version: 1.2.0
	
	Desired version: 1.0.1
	Available versions: 1.0.0, 2.0.0
	Chosen version: there is no compatible version available
```

If the version specified is a _preview_ version, the default behavior is:
1) Pick the exact _preview_ version based on [major].[minor].[patch]-[name]-[build]
2) If none available, pick the nearest _preview_ version based on [name]-[build]

This means _preview_ is never rolled forward to _production_.

	For instance:

	Desired version: 1.0.1-preview2-x
	Available versions: 1.0.1-preview2-x, 1.0.1-preview3-x
	Chosen version: 1.0.1-preview2-x
	
	Desired version: 1.0.1-preview2-x
	Available versions: 1.0.1-preview3-x
	Chosen version: 1.0.1-preview3-x
	
	Desired version: 1.0.1-preview2-x
	Available versions: 1.0.1, 1.0.2-preview3-x
	Chosen version: there is no compatible version available	

## Settings to control behavior
### applyPatches
Once a compatible framework version is chosen as explained above, the latest patch is selected. To disable this, specify the "applyPatches" property in the configuration file.

	For instance:

	Patch roll forward: enabled
	Desired version: 1.0.1
	Available versions: 1.0.1, 1.0.2
	Chosen version: 1.0.2
	
	Patch roll forward: disabled
	Desired version: 1.0.1
	Available versions: 1.0.1, 1.0.2
	Chosen version: 1.0.1

	Patch roll forward: disabled
	Desired version: 1.0.1
	Available versions: 1.0.2, 1.0.3
	Chosen version: 1.0.2

### --fx-version
To specify the exact desired framework version, use the command line argument '--fx-version'. In this case, only the specified version will be accepted, even if patch roll forward is enabled. The expected behavior would be the same in the example above.

### Roll Forward in Absence of Candidate Fx

"Roll Forward On No Candidate Fx" only applies to _production_ versions and is enabled by default for [patch] and [minor], and be changed through:
- Command line argument ('--roll-forward-on-no-candidate-fx' argument)
- Runtime configuration file ('rollForwardOnNoCandidateFx' property)
- DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX environment variable
	
The valid values:

0) Off  (_do not roll forward_)
1) Roll forward on [minor] and [patch]  (_this is the default value_)
2) Roll forward on [major], [minor] and [patch]

If this feature is enabled and no compatible framework version is found, we'll search for the nearest production version available. After locating it, a patch roll forward will be applied if enabled.
```
	For instance:

	Patch roll forward: enabled
	Roll Forward On No Candidate Fx: 1 (minor)
	Desired Version: 1.0.0
	Available versions: 1.1.1, 1.1.3, 1.2.0, 2.0.0
	Chosen version: 1.1.3

	Patch roll forward: disabled
	Roll Forward On No Candidate Fx: 1 (minor)
	Desired Version: 1.0.0
	Available versions: 1.1.1, 1.1.3, 1.2.0
	Chosen version: 1.1.1
	
	Patch roll forward: enabled
	Roll Forward On No Candidate Fx: 0 (disabled)
	Desired Version: 1.0.0
	Available versions: 1.1.1
	Chosen version: there is no compatible version available
```

It's important to notice that, even if "Roll Forward On No Candidate Fx" is enabled, only the specified framework version will be accepted if the '--fx-version' argument is used.

Since there are three ways to specify the values, conflicts will be resolved by the order listed above (command line has priority over config, which has priority over the environment variable). 
```
	For instance:

	'rollForwardOnNoCandidateFx' property is set to '1'
	DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX env var is set to '0'
	The feature is ENABLED.

	'--roll-forward-on-no-candidate-fx' argument is set to '0'
	'rollForwardOnNoCandidateFx' property is set to '1'
	DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX env var is set to '1'
	The feature is DISABLED.
```	
	
There is no inheritance when there are chained framework references. If the app references FX1, and FX1 references FX2, then the resolution of FX2 only takes into account settings from `.runtimeconfig.json` in FX1, CLI and env. variable. The settings in the app's `.runtimeconfig.json` have no effect on resolution of FX2.

## Multilevel SharedFx Lookup

Finally, it's important to notice that, even with the feature enabled, the Multilevel SharedFx Lookup behavior is the same: if we are not able to find any compatible version in a folder, we search in the next one.
```
	For instance:

	Roll Forward On No Candidate Fx: 1 (minor)
	Desired version: 1.0.1
	Available versions in the current working dir: 1.1.0,
	Available versions in the shared location dir: 1.0.0
	Chosen version: 1.1.0
```
