---
applyTo: ".github/workflows/**,.github/aw/**,.github/agents/agentic-workflows.agent.md"
---

# Agentic Workflow Runtime Pins

These requirements apply when creating, editing, compiling, or upgrading gh-aw workflows.
Also follow the repository-specific requirements in
[the workflow agent](../agents/agentic-workflows.agent.md).

## Mandatory MCP Gateway Pin Review

**Every gh-aw upgrade MUST reassess the MCP gateway `container_pins` mapping in
`.github/workflows/aw.json` and any per-workflow overrides. An upgrade is incomplete until
the pins have been removed, updated, or explicitly justified against the new compiler's
default. Never blindly carry an old pin forward or remove it merely because gh-aw was
upgraded.**

The current temporary mapping redirects `ghcr.io/github/gh-aw-mcpg:v0.4.25`, the gh-aw
`v0.89.21` default, to digest-pinned `v0.4.30`.

**Current reviewed exception:** gh-aw `v0.89.21` does not apply repository container mappings
to `holistic-review`'s `CLI_PROXY_IMAGE`. That proxy retains the compiler default `v0.4.25`;
the main gateways use `v0.4.30`. Both exceed the reviewed minimum `v0.4.11`. Preserve strict
mode rather than adding an unsupported override. On every upgrade, explicitly reassess this
exception and remove it when the compiler supports the mapping for the proxy.

Before changing the compiler or its Actions version:

1. Verify the executable with `gh aw --version`; extension installation metadata alone is
   not proof of the compiler version.
2. Read `DefaultMCPGatewayVersion` in the upstream source at the exact target gh-aw release
   tag. Review the corresponding gateway releases and the reason for the existing override.
3. If the default is at least as new as the pinned target and satisfies its requirements,
   **remove the gateway mapping and any redundant per-workflow overrides**. Otherwise,
   update the mapping's source key to the new compiler's exact default image and retain or
   update its released target and verified digest after explicit review. A mapping with an
   obsolete source key is silently ineffective and MUST NOT be left behind. Never downgrade
   the gateway as a side effect of upgrading gh-aw.
4. Keep this document's current mapping and compiler-default statement accurate. Record
   the pin decision and resulting versions in the pull request description. If the decision
   cannot be verified, stop rather than publishing the upgrade.

New workflows must use the same reviewed gateway target while the mapping is required. Do
not introduce mixed gateway versions without an explicit, documented workflow-specific
requirement.

gh-aw `v0.89.21` rejects `sandbox.mcp.version` in strict mode and does not import it from
shared Markdown files. Do not disable strict mode merely to set a gateway version. Verify
which gateway and proxy consumers receive the repository mapping; never assume all do.

## Regeneration and Verification

- Never hand-edit generated `.lock.yml` files. Compile all workflows from the repository
  root with `gh aw compile --strict --schedule-seed dotnet/runtime --force-refresh-container-pins --validate`.
- Digest refresh does not select a newer gateway tag or update packages inside an image.
- Verify the generated compiler metadata, setup action, gateway image tags and digests, and
  any `CLI_PROXY_IMAGE` or `DIFC_PROXY_IMAGE` references. Checking only image pre-download is
  insufficient. Every runtime consumer must meet the reviewed minimum version, and any
  consumer that does not use the pinned target requires an explicitly reviewed, documented
  exception. Stop rather than silently carrying forward an unexpected version.
- Preserve workflow triggers, schedules, permissions, output limits, authentication, and
  repository-specific configuration unless an intentional migration requires a change.
- Commit source changes, regenerated lock files, and `.github/aw/actions-lock.json` together.
