---
name: update-area-owners
description: >
  Update dotnet/runtime area ownership documentation and issue/PR routing. Use
  for adding, changing, or removing area leads, owners, consultants, mentionees,
  scoping/notes, or GitHub area team membership.
---

## Inputs

Accept a request describing one or more of:

- a new area label and its display row;
- a lead change;
- direct owner changes;
- GitHub team membership changes;
- consultant changes;
- non-team mentionees;
- removal of an area and the area into which it is being collapsed.

For each affected area, resolve and show:

```text
label:       the exact existing or proposed label, preserving casing
display row: affected area data mapped from the headers in docs/area-owners.md
team:        zero or more dotnet/<team-slug> teams, or no team
mentionees:  direct GitHub usernames and dotnet/<team-slug> entries in policy YAML
target:      destination area for a collapse, when applicable
```

If the request does not identify the destination for a removal/collapse, stop
and ask for it. Do not infer a destination.

# Update area owners

Keep the runtime area-owner sources synchronized. This skill is for repository
changes. When a request includes GitHub area-team membership or team
creation/deletion, read
[references/github-team-management.md](references/github-team-management.md).
It produces reviewable Bash and PowerShell handoff scripts for an authorized
maintainer; those scripts do not make live changes unless the maintainer
explicitly runs them.

## Source files and related artifacts

Read all of these before editing:

1. `docs/area-owners.md`
2. `.github/policies/resourceManagement.yml`
3. `.github/CODEOWNERS`
4. `docs/infra/automation.md`
5. `.github/labeler-readme.md` and any labeler workflow/configuration found by
   searching for the affected label

Search the repository for every exact affected label, team slug, old owner, and
new owner. Include references in workflow files, issue-management documentation,
and other peer policy files in the impact report. Do not change unrelated
references merely because they contain a similar string.

Do not create, rename, or delete actual GitHub labels. This skill updates area
ownership documentation and issue/PR routing only.

## Current source formats

### `docs/area-owners.md`

Read the file's area-table header row before interpreting or editing any area.
Use those headers to map each area's cell values. Preserve the table's existing
alignment, exact label casing, mentions, and explanatory notes. The area table
is followed by separate operating-system, architecture, and community-triager
sections. Change those sections only when the request explicitly targets one of
them, not when an area has a similar name.

### `.github/policies/resourceManagement.yml`

Area routing is the `eventResponderTasks` entry whose description is
`Area-owners`. It has two coupled parts:

1. An outer `or` list of `labelAdded` predicates under an open issue/PR
   condition.
2. A `then` list containing one `if` block per routed area. Each block has a
   `hasLabel` predicate and a `mentionUsers` action:

```yaml
      - if:
        - hasLabel:
            label: area-System.Example
        then:
        - mentionUsers:
            mentionees:
            - dotnet/area-system-example
            - consultant-username
            replyTemplate: >-
              Tagging subscribers to this area: ${mentionees}

              See info in [area-owners.md](https://github.com/dotnet/runtime/blob/main/docs/area-owners.md) if you want to be subscribed.
            assignMentionees: False
```

For a routed area, always update both the outer `labelAdded` predicate and the
inner `hasLabel`/`mentionUsers` block. Keep the standard reply template and
`assignMentionees: False`. Team values in this file are `dotnet/<team-slug>`;
direct mentionees are GitHub usernames without `@`.

The policy mentionee list is deliberately not a mechanical copy of the area's
owners field. Existing blocks can include a lead, omit an owner, include
consultants, or combine direct users with a team. Preserve those differences
unless the request specifically changes notifications. An area row may also
exist without a policy route; report that state instead of adding a route
without instruction.

`docs/infra/automation.md` confirms that this policy YAML controls area
subscriptions. The Markdown table alone is not sufficient.

## Workflow

### 1. Inventory and classify the request

Read the affected row and policy blocks, then search for all related references.
Classify each requested operation as one of:

- add;
- update lead;
- update direct owners;
- update team membership;
- update consultants;
- update non-team mentionees;
- collapse/remove.

For an existing team, inspect its current slug and membership with read-only
GitHub queries when available. If the team is absent, distinguish "create a
new team" from "use direct mentionees" and record that decision.

### 2. Plan the synchronized repository edit

Produce a compact before/after table for each area. Include:

- exact Markdown row changes;
- outer policy `labelAdded` changes;
- inner policy route changes;
- team operation(s), if any;
- references that are intentionally left unchanged and why.

For a new routed area, add the label to the outer list and add an inner route.
For a label rename performed externally, change both repository policy
occurrences and the Markdown row in one change after the GitHub label has been
renamed. This skill must not rename the GitHub label itself. For a lead-only
change, do not add the lead to policy mentionees unless the request also
changes notifications.

For owner changes, distinguish:

- direct owners represented in the mapped area data;
- members of a GitHub team represented by `@dotnet/<team-slug>`;
- policy mentionees who are neither direct owners nor team members.

For consultant or non-team mentionee changes, update the mapped notes field
and/or the policy list independently as requested. Never remove a team member
merely because a consultant was removed from the notes.

For a collapse/removal:

1. require and validate the destination area;
2. decide whether old issues/PRs retain the old label or need a label migration;
3. remove the source row;
4. remove the source `labelAdded` predicate and inner policy route;
5. update the destination row, policy mentionees, and notes only as needed;
6. emit an explicit team disposition: rename into the destination team, merge
   membership, archive/leave the old team, or delete it;
7. search again for stale source-label and source-team references.

### 3. Apply repository edits

Edit only the relevant sections of `docs/area-owners.md` and
`.github/policies/resourceManagement.yml`. Preserve surrounding ordering,
whitespace, and policy formatting. Update a peer file only when the inventory
shows a concrete reference that must change, such as a path-specific
`.github/CODEOWNERS` entry.

Do not make changes to actual GitHub labels or teams directly. The repository PR
should contain the ownership and routing changes; any generated team-management
scripts are session-local and should not be added to the PR.

### 4. Generate the session-local handoff

When the inventory identifies GitHub team membership or team creation/deletion
work, follow [the GitHub team-management handoff](references/github-team-management.md).
The generated scripts are session-local and must not be added to the repository
change.

### 5. Validate and report

Run the smallest available static checks:

- parse the edited YAML with an available YAML parser, if present;
- verify the Markdown table remains structurally valid;
- verify every routed area has exactly one outer trigger and one inner route;
- verify no requested old label/team reference remains except in documented
  migration notes;
- syntax-check both generated scripts without executing mutations;
- verify the Bash and PowerShell operation inventories are identical.

The final report must include:

1. repository files changed;
2. areas and scenarios handled;
3. generated script paths, when applicable;
4. exact permissions required for the listed team operations;
5. validation results and any unresolved external state.

## Output discipline

Do not claim that a GitHub team changed. The skill changes repository ownership
and routing files and, when needed, generates team-management scripts only.
State that team operations remain pending until an authorized user reviews and
runs the generated scripts.
