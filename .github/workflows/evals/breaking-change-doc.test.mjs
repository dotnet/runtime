import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { runInNewContext } from "node:vm";

const comment = { type: "add_comment", item_number: 123, body: "Sanitized comment" };
const noop = { type: "noop", message: "No documentation needed" };
const draft = "Complete raw comment\n";
const cases = [
    ...["report_missing_tool", "report_missing_data", "report_incomplete"].map((type) => ({
        name: `comment plus ${type}`,
        items: [comment, { type }],
        drafts: true,
        error: /Documentation could not be completed/,
    })),
    { name: "valid comment", items: [comment], drafts: true },
    { name: "noop without drafts", items: [noop] },
    { name: "dry-run drafts", items: [noop], drafts: true, dryRun: true },
    { name: "dry-run noop without drafts", items: [noop], dryRun: true },
    { name: "wrong processed PR", items: [{ ...comment, item_number: 124 }], drafts: true, error: /wrong PR/ },
    { name: "wrong raw PR", items: [comment], drafts: true, rawPr: 124, error: /raw comment intent/ },
    { name: "mismatched raw body", items: [comment], drafts: true, rawBody: "Different", error: /does not match/ },
    { name: "duplicate processed comments", items: [comment, comment], drafts: true, error: /comment or an explicit noop/ },
    { name: "duplicate raw comments", items: [comment], drafts: true, duplicateRaw: true, error: /raw comment intent/ },
    { name: "dry-run publication", items: [comment], drafts: true, dryRun: true, error: /must not request publication/ },
    { name: "drafts without intent", items: [], drafts: true, error: /drafts alone/ },
    { name: "non-dry-run noop with drafts", items: [noop], drafts: true, error: /require publication/ },
    { name: "missing drafts", items: [comment], error: /ENOENT/ },
    { name: "empty issue draft", items: [comment], drafts: true, emptyIssue: true, error: /draft is empty/ },
    { name: "incomplete output", items: [{ type: "report_incomplete" }], error: /could not be completed/ },
    { name: "agent errors", items: [noop], errors: ["Error"], error: /output is invalid/ },
    { name: "invalid PR number", items: [comment], drafts: true, pr: "123x", error: /Invalid PR number/ },
    { name: "empty comment", items: [{ ...comment, body: " " }], drafts: true, error: /comment is empty/ },
];

// Execute the actual inline scripts so source and regenerated lock changes cannot bypass the fixtures.
const scripts = await Promise.all(["md", "lock.yml"].map(async (extension) => {
    const workflow = await readFile(new URL(`../breaking-change-doc.${extension}`, import.meta.url), "utf8");
    const steps = [...workflow.matchAll(/^\s*- name: Validate documentation outcome\r?$/gm)];
    assert.equal(steps.length, 1, `expected one documentation validator in ${extension}`);
    const match = workflow.slice(steps[0].index).match(/^( *)script: \|\r?\n((?:\1  .*(?:\r?\n|$))+)/m);
    assert.ok(match, `documentation validator script was not found in ${extension}`);
    const script = match[2].split(/\r?\n/).map((line) => line.slice(match[1].length + 2)).join("\n");
    return { extension, script };
}));

test("source and compiled documentation validators match", () => {
    assert.equal(scripts[0].script, scripts[1].script);
});

for (const { extension, script } of scripts) {
    for (const fixture of cases) {
        test(`${extension}: ${fixture.name}`, () => {
            const rawComment = {
                ...comment,
                item_number: fixture.rawPr ?? 123,
                body: fixture.rawBody ?? draft,
            };
            const files = new Map([
                ["/tmp/gh-aw/agent_output.json", JSON.stringify({ items: fixture.items, errors: fixture.errors })],
                ["/tmp/gh-aw/safeoutputs.jsonl",
                    [rawComment, ...(fixture.duplicateRaw ? [rawComment] : [])].map(JSON.stringify).join("\n")],
            ]);
            if (fixture.drafts) {
                files.set("artifacts/docs/breakingChanges/issue-draft.md", fixture.emptyIssue ? " " : "Issue draft");
                files.set("artifacts/docs/breakingChanges/pr-comment.md", draft);
            }
            const execute = () => runInNewContext(script, {
                require(id) {
                    assert.equal(id, "node:fs");
                    return {
                        readFileSync(path, encoding) {
                            assert.equal(encoding, "utf8");
                            if (!files.has(path)) {
                                throw new Error(`ENOENT: ${path}`);
                            }
                            return files.get(path);
                        },
                        existsSync: (path) => files.has(path),
                    };
                },
                process: { env: { PR_NUMBER: fixture.pr ?? "123", SUPPRESS_OUTPUT: String(!!fixture.dryRun) } },
            }, { timeout: 1000 });
            if (fixture.error) {
                assert.throws(execute, fixture.error);
            } else {
                assert.doesNotThrow(execute);
            }
        });
    }
}
