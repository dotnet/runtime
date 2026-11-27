# Step 0: Prerequisites

Run before anything else — ensures tools and auth are available.

## 1. Python 3.8+ and dependencies

Python must be available on the system. Install the required package:

```bash
pip install requests
```

If `python` is not found, install it via `winget install -e --id Python.Python.3.12`
(Windows) or your system package manager (`apt install python3` on Linux).

## 2. Obtain `ADO_TOKEN`

The AzDO Test Results API requires a bearer token — even on dnceng-public,
`/_apis/test/runs` returns 203 (sign-in HTML page) without auth.

**Resolution order:**

```
if ADO_TOKEN env var is already set → done
else if `az` is installed:
    try: ADO_TOKEN=$(az account get-access-token --resource "499b84ac-1321-427f-aa17-267ca6975798" --query accessToken -o tsv)
    if that fails (not logged in) → ask user to run `az login`, then retry
else:
    install az cli (winget install -e --id Microsoft.AzureCLI on Windows,
                     curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash on Linux)
    then ask user to run `az login`
```

Set `ADO_TOKEN` as an environment variable for the session. All subsequent
steps (especially `extract_failed_tests.py`) read it automatically.
The token is valid ~60 minutes.

### CI environments

**In CI (AzDO pipeline):** Map `$(System.AccessToken)` → `ADO_TOKEN` env
var in the pipeline YAML — no `az` install or login needed.

### Other APIs

- **AzDO Builds API** — public, no auth required.
- **Helix API** — public, no auth required.
