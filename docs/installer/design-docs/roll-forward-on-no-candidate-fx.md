# Roll Forward On No Candidate Fx

## Default Behavior

The desired framework version is defined through a configuration json file (appname.runtimeconfig.json). For productions, the default behavior is to pick the latest patch compatible available version.

	For instance:

	Desired version: 1.0.1
	Available versions: 1.0.0, 1.0.1, 1.0.2, 1.0.3, 1.1.0, 1.1.1, 2.0.1
	Chosen version: 1.0.3

	Desired version: 1.0.1
	Available versions: 1.0.0, 1.1.0, 2.0.0
	Chosen version: there is no compatible version available

It's possible to disable the patch roll forward through the "applyPatches" property in the configuration file. If it is  set to 'false' and the specified version is not found, then we fail.

	For instance:

	Patch roll forward: disabled
	Desired version: 1.0.1
	Available versions: 1.0.2, 1.0.3
	Chosen version: there is no compatible version available

It's also possible to specify the desired framework version through the command line argument '--fx-version'. In this case, only the specified version will be accepted, even if patch roll forward is enabled. The expected behavior would be the same in the example above.

## Roll Forward in Absence of Candidate Fx

"Roll Forward On No Candidate Fx" is disabled by default, but it can be enabled through environment variable, configuration file or command line argument.

If this feature is enabled and no compatible framework version is found, we'll search for the next lowest production available. After locating it, a patch roll forward will be applied if enabled.

	For instance:

	Patch roll forward: enabled
	Roll Forward On No Candidate Fx: enabled
	Desired Version: 1.0.0
	Available versions: 1.1.1, 1.1.3, 1.2.0
	Chosen version: 1.1.3

	Patch roll forward: disabled
	Roll Forward On No Candidate Fx: enabled
	Desired Version: 1.0.0
	Available versions: 1.1.1, 1.1.3, 1.2.0
	Chosen version: 1.1.1

It's important to notice that, even if "Roll Forward On No Candidate Fx" is enabled, only the specified framework version will be accepted if the '--fx-version' argument is used.

### Enabling feature

There are three ways to enable the feature:

	1. Command line argument ('--roll-forward-on-no-candidate-fx' argument).
	2. Runtime configuration file ('rollForwardOnNoCandidateFx' property).
	3. DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX environment variable.

It can be enabled or disabled by setting the specified values to '1' or '0', respectively.

The conflicts will be resolved by following the priority rank described above (from 1 to 3).

	For instance:

	'rollForwardOnNoCandidateFx' property is set to '1'
	DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX env var is set to '0'
	The feature is ENABLED.

	'--roll-forward-on-no-candidate-fx' argument is set to '0'
	'rollForwardOnNoCandidateFx' property is set to '1'
	DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX env var is set to '1'
	The feature is DISABLED.

### Multilevel SharedFx Lookup

Finally, it's important to notice that, even with the feature enabled, the Multilevel SharedFx Lookup behavior is the same: if we are not able to find any compatible version in a folder, we search in the next one.

	For instance:

	Roll Forward On No Candidate Fx: enabled
	Desired version: 1.0.1
	Available versions in the current working dir: 1.0.0
	Available versions in the user .NET location: 2.0.0
	Available versions in the exe dir: 1.0.1
	Chosen version: 2.0.0