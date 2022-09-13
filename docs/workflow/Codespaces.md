# Using Codespaces <!-- omit in toc -->

* [Create a Codespace](#create-a-codespace)
* [Updating dotnet/runtime's Codespaces Configuration](#updating-dotnetruntimes-codespaces-configuration)
* [Testing out your Changes](#testing-out-your-changes)

Codespaces allows you to develop in a Docker container running in the cloud. You can use an in-browser version of VS Code or the full VS Code application with the [GitHub Codespaces VS Code Extension](https://marketplace.visualstudio.com/items?itemName=GitHub.codespaces). This means you don't need to install any prerequisites on your current machine in order to develop in _dotnet/runtime_.

## Create a Codespace

The _dotnet/runtime_ repo runs a nightly GitHub Action to build the latest code in it. This allows you to immediately start developing and testing after creating a Codespace without having to build the whole repo. When the machine is created, it will have built the repo using the code as of 6 AM UTC of that morning.

1. From this [repository's root](https://github.com/dotnet/runtime), drop-down the _Code_ button and select the _Codespaces_ tab.

![New codespace button](https://docs.github.com/assets/images/help/codespaces/new-codespace-button.png)

2. Select the Machine type. For _dotnet/runtime_, it is recommended to select at least a _4-Core_ machine. You can also verify that a "Prebuild" is ready.

![Codespace machine size](codespace-machine-size.png)

_If these instructions are out of date, see <https://docs.github.com/codespaces/developing-in-codespaces/creating-a-codespace#creating-a-codespace> for instructions on how to create a new Codespace._

## Updating dotnet/runtime's Codespaces Configuration

The Codespaces configuration is spread across the following places:

1. The [.devcontainer](/.devcontainer) folder contains:
    * The `devcontainer.json` file, which configures the Codespace and mostly has the required VS Code settings.
    * The _Dockerfile_ used to create the image.
    * The _scripts_ folder, which contains any scripts that are executed during the creation of the Codespace. This has the build command that builds the entire repo for the Prebuilds.
2. The GitHub Action can be configured by following the instructions [here](https://docs.github.com/codespaces/prebuilding-your-codespaces/configuring-prebuilds).

To test out changes to the `.devcontainer` files, you can follow the process in the [Applying Changes to your Configuration](https://docs.github.com/codespaces/customizing-your-codespace/configuring-codespaces-for-your-project#applying-changes-to-your-configuration) docs. This allows you to rebuild the Codespace privately before creating a PR.

## Testing out your Changes

To test out your `.yml` changes, here is the process:

**NOTE**: Executing these steps will overwrite the current prebuilt container for the entire repo. Afterwards, anyone creating a new Codespace will get a prebuilt machine with your test changes until the Action in `main` is executed again.

1. Edit and commit the files to a branch.
2. Push that to a branch on _dotnet/runtime_. Be careful that you aren't pushing to `main` or some other important branch. Prefix your branch name with your GitHub account name, so others know it is a dev branch. ex. `username/FixCodespaces`.
3. In the _Actions_ tab at the top of _dotnet/runtime_:
    * Select "Create Codespaces Prebuild" action on the left.
    * On the right click "Run workflow" and pick your branch.
    * After it runs, try to create a Codespace.
