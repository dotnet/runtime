# GitHub team management handoff

Use this reference when an area-owner maintenance request updates GitHub area
team membership or creates or deletes a GitHub area team. The skill must not
call mutating GitHub APIs directly. Generate a Bash script and a PowerShell
script in a unique directory below the session's local `files/` artifact
directory, then report both paths.

## Safety and permissions

The generated scripts must:

- default repository association operations to `dotnet/runtime` in the
  `dotnet` organization;
- default to dry-run and require an explicit `--apply`/`-Apply` confirmation
  before mutating operations;
- use the authenticated `gh` CLI and never accept or print a token;
- use `set -euo pipefail` in Bash and `$ErrorActionPreference = 'Stop'` in
  PowerShell;
- check every API response and stop on an unexpected status;
- use the `gh` CLI for team creation, team updates, membership updates,
  repository association, and team deletion only when the requested change
  explicitly requires them;
- print the planned operation and target before each mutation.

Team creation, renaming, deletion, and membership changes require an
authenticated GitHub identity with the corresponding organization permissions.
Team creation is configurable: organization members can create teams when the
organization allows member-created teams; otherwise an organization owner is
required. Organization owners can delete teams, while team maintainers can
manage membership, team details, and repository access for teams they maintain.
The identity must also be an organization member where GitHub requires it.

## Team configuration

Team names use lower-kebab-case. Area teams are children of `dotnet/npt`, which
is itself a child of `dotnet/microsoft`.

When creating a new area team, declare and apply this configuration:

```text
team slug:             area-system-example
display name:          area-system-example
description:           Area owners for area-System.Example
visibility:            closed
repository permission: pull
notifications:         notifications_enabled
parent team:           dotnet/npt
```

The `area-System.Example` label preserves its dot and casing, while the team
slug is lower-kebab-case (`area-system-example`).

## Script contents

Create a unique directory, for example:

```text
<session-files>/update-system-example/
  update-system-example.sh
  update-system-example.ps1
```

The two scripts must contain the same operations and target values. Include:

- a header with the organization, repository, affected areas, required
  permissions, and a warning that the script changes live GitHub state;
- the proposed team slug, display name, description, visibility, notification
  setting, repository permission, and parent team (`dotnet/npt`);
- create/rename/delete team operations where requested;
- membership additions and removals, sorted and deduplicated;
- repository association updates when the team is intended to represent
  `dotnet/runtime`;
- dry-run as the default and an explicit confirmation switch for writes;
- a final read-only verification of team membership and team/repository
  association where the operation permits it.

## Bash sample

```bash
#!/usr/bin/env bash
set -euo pipefail

ORG="dotnet"
TEAM_SLUG="area-system-example"
ADD_MEMBERS=("new-owner" "another-owner")
# Includes members who used to be owners and other members to remove.
REMOVE_MEMBERS=("former-member" "former-owner")
FORMER_OWNERS=("former-owner")
NEW_MAINTAINERS=("new-lead")
APPLY=false
[[ "${1:-}" == "--apply" ]] && APPLY=true
CURRENT_USER="$(gh api user --jq .login)"
CURRENT_ROLE="$(gh api "/orgs/${ORG}/teams/${TEAM_SLUG}/memberships/${CURRENT_USER}" --jq .role)"

run() {
  echo "+ gh $*"
  if [[ "$APPLY" == true ]]; then
    gh "$@"
  fi
}

for username in "${ADD_MEMBERS[@]}"; do
  run api --method PUT \
    "/orgs/${ORG}/teams/${TEAM_SLUG}/memberships/${username}" \
    -f role=member
done

for username in "${NEW_MAINTAINERS[@]}"; do
  run api --method PUT \
    "/orgs/${ORG}/teams/${TEAM_SLUG}/memberships/${username}" \
    -f role=maintainer
done

for username in "${REMOVE_MEMBERS[@]}"; do
  if [[ "$username" == "$CURRENT_USER" ]]; then
    echo "Cannot remove the current user. Have a new maintainer remove ${username}."
    continue
  fi
  run api --method DELETE \
    "/orgs/${ORG}/teams/${TEAM_SLUG}/memberships/${username}"
done

for username in "${FORMER_OWNERS[@]}"; do
  echo "Former owner scheduled for removal: ${username}"
done

if [[ "$APPLY" != true ]]; then
  echo "Dry run only. Re-run with --apply to execute additions and removals."
elif [[ "$CURRENT_ROLE" == "maintainer" && ! " ${NEW_MAINTAINERS[*]} " =~ " ${CURRENT_USER} " ]]; then
  echo "The current user remains a maintainer. Have a new maintainer perform any requested downgrade."
fi
gh api "/orgs/${ORG}/teams/${TEAM_SLUG}/members" >/dev/null
```

## PowerShell sample

```powershell
param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$Org = 'dotnet'
$TeamSlug = 'area-system-example'
$AddMembers = @('new-owner', 'another-owner')
# Includes members who used to be owners and other members to remove.
$RemoveMembers = @('former-member', 'former-owner')
$FormerOwners = @('former-owner')
$NewMaintainers = @('new-lead')
$CurrentUser = & gh api user --jq .login
if ($LASTEXITCODE -ne 0) {
    throw "gh api failed to identify the current user"
}
$CurrentRole = & gh api "/orgs/$Org/teams/$TeamSlug/memberships/$CurrentUser" --jq .role
if ($LASTEXITCODE -ne 0) {
    throw "gh api failed to identify the current user's team role"
}

function Invoke-Gh {
    param([string[]]$Arguments)
    Write-Output "+ gh $($Arguments -join ' ')"
    if ($Apply) {
        & gh @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "gh failed with exit code $LASTEXITCODE"
        }
    }
}

foreach ($Username in $AddMembers) {
    Invoke-Gh @('api', '--method', 'PUT',
        "/orgs/$Org/teams/$TeamSlug/memberships/$Username",
        '-f', 'role=member')
}

foreach ($Username in $NewMaintainers) {
    Invoke-Gh @('api', '--method', 'PUT',
        "/orgs/$Org/teams/$TeamSlug/memberships/$Username",
        '-f', 'role=maintainer')
}

foreach ($Username in $RemoveMembers) {
    if ($Username -eq $CurrentUser) {
        Write-Warning "Cannot remove the current user. Have a new maintainer remove $Username."
        continue
    }
    Invoke-Gh @('api', '--method', 'DELETE',
        "/orgs/$Org/teams/$TeamSlug/memberships/$Username")
}

foreach ($Username in $FormerOwners) {
    Write-Output "Former owner scheduled for removal: $Username"
}

if (-not $Apply) {
    Write-Output 'Dry run only. Re-run with -Apply to execute additions and removals.'
}

if ($Apply) {
    if ($CurrentRole -eq 'maintainer' -and $NewMaintainers -notcontains $CurrentUser) {
        Write-Warning 'The current user remains a maintainer. Have a new maintainer perform any requested downgrade.'
    }
}
& gh api "/orgs/$Org/teams/$TeamSlug/members" | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "gh api verification failed with exit code $LASTEXITCODE"
}
```

These samples use `gh api` as the CLI interface to GitHub. Generated scripts
must check each command's exit status and response, never use `--silent` in a
way that hides an error, and must not delete an old team automatically during a
rename or collapse. Add members and new maintainer(s) before processing
removals. Never downgrade the current user from maintainer or remove the
current user. If either is needed, stop that operation and instruct the user
to have a new maintainer perform the downgrade or removal.
