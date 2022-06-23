## Automation

### Fabric Bot

This repository uses Fabric Bot to automate issue and pull request management. All automation rules are defined in the [`.github/fabricbot.json`](../../.github/fabricbot.json) file.

#### Notifications

You are welcome to enable notifications for yourself for one or more areas. You will be tagged whenever there are new issues and PR's in the area. You do not need to have commit access for this. To add or remove notifications for yourself, please offer a PR that edits the "mentionees" value for that area. [Here is an example](https://github.com/dotnet/runtime/commit/c28b13f0cf4e2127a74285b65188413ca7e677d4).

#### Other changes

For any other changes, you will need access to the [`Fabric Bot portal`](https://portal.fabricbot.ms/bot/) which is only available to Microsoft employees at present. Ensure you are signed out from the portal, choose "Import Configuration" option and make changes using the editor. It's necessary to use the portal because there is at present no published JSON schema for the configuration format.
