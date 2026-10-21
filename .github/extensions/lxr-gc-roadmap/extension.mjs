// Extension: lxr-gc-roadmap
// Tracks LXR GC implementation phases, correctness evidence, and benchmark trends.

import { createServer } from "node:http";
import { randomBytes } from "node:crypto";
import { createCanvas, joinSession } from "@github/copilot-sdk/extension";
import { renderHtml } from "./renderer.mjs";
import {
    addBenchmarkResult,
    getState,
    readRoadmap,
    updateStep,
} from "./store.mjs";

const servers = new Map();
const implementationRequests = new Set();

function sendJson(res, statusCode, value) {
    res.writeHead(statusCode, {
        "Content-Type": "application/json; charset=utf-8",
        "Cache-Control": "no-store",
    });
    res.end(JSON.stringify(value));
}

async function readJson(req) {
    const chunks = [];
    let size = 0;

    for await (const chunk of req) {
        size += chunk.length;
        if (size > 16 * 1024) {
            throw new Error("Request body exceeds 16 KiB.");
        }
        chunks.push(chunk);
    }

    return JSON.parse(Buffer.concat(chunks).toString("utf8"));
}

function validateApiRequest(req, token) {
    if (req.headers["x-lxr-roadmap-token"] !== token) {
        throw new Error("Invalid canvas API token.");
    }

    const origin = req.headers.origin;
    if (origin && origin !== `http://${req.headers.host}`) {
        throw new Error("Cross-origin canvas API requests are not allowed.");
    }
}

async function queueImplementation(stepId) {
    const state = await getState();
    const phase = state.phases.find((item) => item.steps.some((step) => step.id === stepId));
    const step = phase?.steps.find((item) => item.id === stepId);
    if (!phase || !step) {
        throw new Error(`Unknown roadmap step: ${stepId}`);
    }
    if (step.status === "done") {
        throw new Error(`Roadmap step ${stepId} is already done.`);
    }
    if (step.status === "in_progress") {
        throw new Error(`Roadmap step ${stepId} already has an implementation session in progress.`);
    }
    if (implementationRequests.has(stepId)) {
        throw new Error(`Implementation planning for ${stepId} is already queued.`);
    }

    const prompt = [
        `The user clicked Implement for LXR GC roadmap step ${step.id}.`,
        "Act as the coordinator now: create exactly one new App project session with kickoff.mode=\"plan\", coordinate_with_creator=true, and notify_on_idle=\"always\".",
        "The child must produce a detailed implementation plan first. The user will review that pending plan in the App and, when approving it, continue with Autopilot implementation.",
        "All work — runtime/EE/JIT contract changes and collector implementation alike — belongs in the kkokosa/runtime project (the runtime-fork project). Create the child session with base_branch=\"lxr\"; never branch an LXR step from the project default `main`. The abandoned C:\\github\\runtimelab prototype is not a source of code, status, or measurements; do not read from it or branch from it.",
        "Every LXR pull request targets the `lxr` branch in kkokosa/runtime. Fork `main` tracks `upstream/main` and must not receive LXR commits. If `lxr` is missing or does not contain the completed dependency steps, stop instead of falling back to `main`.",
        "Do not implement the roadmap step in this coordinator session.",
        "",
        `Phase: ${phase.id} — ${phase.title}`,
        `Step: ${step.id} — ${step.title}`,
        `Status: ${step.status}`,
        `Summary: ${step.summary}`,
        `Correctness criteria: ${step.correctness}`,
        `Benchmark criteria: ${step.benchmarks}`,
        `Runtime changes: ${step.runtimeChanges || "None currently identified."}`,
        `Dependencies: ${step.dependencies}`,
        `References: ${step.references}`,
        "",
        "The parity oracle is chosen per mechanism between two binding-pinned pairs of the reference implementation at C:\\github\\lxr-reference: pldi-2022 = binding abbdd1d (tag lxr-pldi-2022) resolving core df8d30a3, and lxr-head = binding 0682434 (branch lxr) resolving core 304ce69d. The basis is the pair the binding actually builds with no override, not the core tag 4d4e516c or core branch head 9625c174 — those were also built in P0.1 and are behaviorally indistinguishable, but they are labelled secondary evidence rather than the declared basis. The choice must be recorded with its reason; coupled mechanisms share one oracle. The lxr-x/simplified branch is excluded. Full parity with the reference is required: approximations and GC-side workarounds are not acceptable substitutes for a reference mechanism.",
        "P0.1 established facts that bind later steps, as corrected by P0.3: the PLDI oracle fails under sustained load, tripping its own barrier assertion `assert!(old.is_null() || rc::count(old) != 0, \"zero rc count\")` at src/plan/barriers.rs:315-319 with the condition at :316, in FieldLoggingBarrier::slow, with the cfg(any(sanity, debug_assertions)) gate explaining why release SIGSEGVs on the same corrupt state. Do not read HEAD's clean runs as evidence that the invariant holds there: at core 304ce69d no barrier-side check exists at all, and the only `zero rc count` assertion is in src/util/sanity/sanity_checker.rs:311-316 with the condition at :312, behind cfg(feature = \"sanity\"), which is not a default feature and was not enabled by the build recipe, so it was compiled out and the comparison was never run. Both spans were first recorded one line off, at :317 and :313, because grepping for the message literal lands on the format-string line rather than the condition — cite multi-line assertion macros as a span and name the condition line. RC and SATB mechanisms still follow specify-from-PLDI, validate-against-lxr-head, but validation requires re-instating the invariant at the validating revision first. RC width is a designed tunable (LOG_REF_COUNT_BITS selectable via lxr_rc_bits_{2,4,8}, default 2 bits with MAX_REF_COUNT == 3) and a port must parameterize rather than hardcode it. At the PLDI oracle there is no src/plan/lxr/ tree at all: LXR is a compile-time feature configuration of the Immix plan, so cross-revision comparison is mechanism-to-mechanism and never path-to-path.",
        "Cite reference locations at the declared oracle revision rather than from the checkout. C:\\github\\lxr-reference/mmtk-core is checked out at the named 9625c174, not at the declared oracle 304ce69d, and the trees differ in shape: 304ce69d has a flat 9-file src/plan/lxr/ including cm.rs, gc_work.rs and remset.rs, while 9625c174 has 13 files with a gc_work/ subdirectory plus block_allocation.rs. A path read from the working tree can name a file that does not exist at the oracle, and any src/plan/lxr/gc_work/... path in this brief is head-named rather than oracle-verified. All four revisions exist as objects in that clone, so read them with `git show <rev>:<path>` and cite in <rev>:<path> form. Never modify that checkout.",
        "P0.1 and P0.2 artifacts live in docs/design/lxr-port/ on the long-lived `lxr` integration branch, originally merged via kkokosa/runtime PRs #1 and #2 — read them from `lxr`, e.g. `git show lxr:docs/design/lxr-port/P0.1-mechanism-diff.md` and `git show lxr:docs/design/lxr-port/P0.2-paper-targets.md`. Their scripts record how the reference was built and reference the WSL build root /root/lxr and the reference checkout C:\\github\\lxr-reference, which are environment paths rather than repository paths.",
        "A citation is not evidence unless the cited code is compiled in the configuration being discussed. Locating text at a line proves only that the text is there. P0.3 cited plan/immix/global.rs:532-534 and plan/lxr/global.rs:377-379 as both oracles ignoring System.gc(); both sit under cfg(feature = \"nogc_no_zeroing\"), a feature in neither oracle's default set and never enabled by the build recipe, so neither line is compiled. The live paths are memory_manager.rs:686 to mmtk.rs:436 to util/heap/gc_trigger.rs:143 at HEAD, which sets the user-triggered flag and requests the collection, and at PLDI the trait default plan/global.rs:590-598, whose body is entirely commented out. Dead code that reads correctly is the sharpest form of this trap and a mechanical citation checker cannot catch it, because the text resolves. Establish that a cited line is reachable in the built configuration before treating it as behavior, and state that configuration alongside the citation.",
        "P0.2 established further binding facts. The local paper is the arXiv Extended Version dated 1 Nov 2022, not the PLDI proceedings paper, so cite it as arXiv:2210.17175 with its table and PDF page number and never assume proceedings numbering matches. The paper reports no p95 pause comparison against G1 at all: Table 7's p50/p95 are LXR-only, and in Table 1, the sole comparative pause table, LXR's GC pauses are longer than G1's at every percentile while its query latency is far better, under the caption \"Short GC pauses do not assure low latency\". Application-observed latency is therefore the acceptance signal and pause distribution is a characterization, not a win condition. Throughput versus G1 is heap-dependent at 0.97 at 1.3x, 0.96 at 2x and 1.01 at 6x, so LXR is slower at generous heaps. Barrier overhead of 1.6% is a geometric mean measured against a no-barrier full-heap Immix build rather than against G1, and sits inside a noise floor of roughly plus or minus three percent. The paper also disabled class unloading, compressed pointers and weak references in all four collectors, a configuration a shipping CoreCLR can never be in.",
        "Environment facts established by running the commands, not by inference. `pip install` works: a machine-wide C:\\ProgramData\\pip\\pip.ini points index-url at an internal proxy feed, so installs resolve through packagefeedproxy.microsoft.io even though the public CDN files.pythonhosted.org fails its TLS handshake — a raw-connectivity failure to that CDN does not mean pip is blocked. `git clone` works. PDF pages can be rasterized in-process with the pymupdf package (page.get_pixmap), so rendering is available even though pdftotext, mutool, pdftoppm, qpdf, Ghostscript, ImageMagick, node and java are all absent as standalone binaries; absence of a binary never implies absence of the capability. Never record a tool as unavailable without running it to completion and pasting the failure.",
        "Any verification script this step produces must audit the artifact it ships with, and must be run against committed content. P0.3's verify-ledger.sh defaulted its document root to an absolute path inside one worktree, so running the committed script from a clean `git archive` extract silently audited a different, mid-edit checkout — reporting a failure the committed tree did not have, and equally able to report a pass it had not earned. Derive default inputs from ${BASH_SOURCE[0]} so a script always checks the tree it ships in, let an explicit argument override, and derive expected counts from the document rather than repeating a literal at several assertion sites. Run the gate on extracted committed content rather than a working tree before treating its exit code as evidence, and note that an i/lf script checked out w/crlf cannot be executed by bash at all, so a pass obtained from a converted copy attests to the copy rather than to the artifact.",
        "If this step carries runtime changes, they must be designed as generic GC-contract capabilities for which LXR is the first consumer rather than the definition: generalize the mechanism not the client, achieve zero measured cost when unused by init-time selection rather than branching, stay additive and version-gated, remain independently defensible without LXR, and carry their own performance evidence.",
        "The child kickoff prompt must include all details above, require reading applicable repository instructions and the cited paper/reference implementation, preserve unrelated work, use Plan mode before edits, and update the coordinator with the final plan and implementation/verification outcome.",
    ].join("\n");

    implementationRequests.add(stepId);
    try {
        await session.send({ prompt });
    } catch (error) {
        implementationRequests.delete(stepId);
        throw error;
    }
    await updateStep({ id: stepId, status: "in_progress" });
    return {
        queued: true,
        stepId: step.id,
        message: "Implementation planning request queued in the coordinator session.",
    };
}

async function routeRequest(req, res, token) {
    const url = new URL(req.url ?? "/", "http://127.0.0.1");

    try {
        if (req.method === "GET" && url.pathname === "/") {
            res.writeHead(200, {
                "Content-Type": "text/html; charset=utf-8",
                "Cache-Control": "no-store",
                "Content-Security-Policy": "default-src 'self'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; img-src 'self' data:",
            });
            res.end(renderHtml());
            return;
        }

        if (url.pathname.startsWith("/api/")) {
            validateApiRequest(req, token);
        }

        if (req.method === "GET" && url.pathname === "/api/state") {
            sendJson(res, 200, await getState());
            return;
        }

        if (req.method === "POST" && url.pathname === "/api/implement") {
            if (!req.headers["content-type"]?.startsWith("application/json")) {
                throw new Error("Content-Type must be application/json.");
            }
            const input = await readJson(req);
            sendJson(res, 202, await queueImplementation(input.stepId));
            return;
        }

        sendJson(res, 404, { error: "Not found" });
    } catch (error) {
        sendJson(res, 400, { error: error instanceof Error ? error.message : String(error) });
    }
}

async function startServer(instanceId) {
    const token = randomBytes(24).toString("base64url");
    const server = createServer((req, res) => {
        void routeRequest(req, res, token);
    });
    await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
    const address = server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    return { server, url: `http://127.0.0.1:${port}/`, instanceId, token };
}

const session = await joinSession({
    canvases: [
        createCanvas({
            id: "lxr-gc-roadmap",
            displayName: "LXR GC Roadmap",
            description: "Tracks LXR GC phases, correctness evidence, and benchmark trends against Workstation and Server GC.",
            inputSchema: {
                type: "object",
                additionalProperties: false,
                properties: {
                    focusStep: {
                        type: "string",
                        description: "Optional roadmap step ID to focus after opening.",
                    },
                },
            },
            actions: [
                {
                    name: "get_roadmap",
                    description: "Read all roadmap phases, progress, and benchmark checkpoints.",
                    handler: async () => getState(),
                },
                {
                    name: "update_step",
                    description: "Update a roadmap step status and its summary, correctness, and benchmark evidence.",
                    inputSchema: {
                        type: "object",
                        additionalProperties: false,
                        properties: {
                            id: { type: "string" },
                            status: { type: "string", enum: ["planned", "in_progress", "done", "failed"] },
                            summary: { type: "string" },
                            correctness: { type: "string" },
                            benchmarks: { type: "string" },
                            runtimeChanges: { type: "string" },
                        },
                        required: ["id"],
                    },
                    handler: async (ctx) => updateStep(ctx.input),
                },
                {
                    name: "add_benchmark_result",
                    description: "Add or replace one collector result in a benchmark checkpoint.",
                    inputSchema: {
                        type: "object",
                        additionalProperties: false,
                        properties: {
                            checkpoint: { type: "string" },
                            date: { type: "string" },
                            stepId: { type: "string" },
                            scenario: { type: "string" },
                            collector: { type: "string", enum: ["lxr", "workstation", "server"] },
                            operationsPerSecond: { type: "number", minimum: 0 },
                            pauseAverageMs: { type: "number", minimum: 0 },
                            pauseP99Ms: { type: "number", minimum: 0 },
                            pauseMaxMs: { type: "number", minimum: 0 },
                            workingSetMb: { type: "number", minimum: 0 },
                            committedMb: { type: "number", minimum: 0 },
                            notes: { type: "string" },
                        },
                        required: ["checkpoint", "date", "stepId", "scenario", "collector"],
                    },
                    handler: async (ctx) => addBenchmarkResult(ctx.input),
                },
                {
                    name: "get_markdown",
                    description: "Read the authoritative Markdown roadmap.",
                    handler: async () => ({ markdown: await readRoadmap() }),
                },
                {
                    name: "start_implementation",
                    description: "Queue creation of a child App session in Plan mode for a roadmap step.",
                    inputSchema: {
                        type: "object",
                        additionalProperties: false,
                        properties: {
                            stepId: { type: "string" },
                        },
                        required: ["stepId"],
                    },
                    handler: async (ctx) => queueImplementation(ctx.input.stepId),
                },
            ],
            open: async (ctx) => {
                let entry = servers.get(ctx.instanceId);
                if (!entry) {
                    entry = await startServer(ctx.instanceId);
                    servers.set(ctx.instanceId, entry);
                }

                const query = new URLSearchParams({ token: entry.token });
                if (typeof ctx.input?.focusStep === "string") {
                    query.set("focus", ctx.input.focusStep);
                }
                return {
                    title: "LXR GC Roadmap",
                    status: "Paper fidelity and performance tracker",
                    url: `${entry.url}?${query}`,
                };
            },
            onClose: async (ctx) => {
                const entry = servers.get(ctx.instanceId);
                if (entry) {
                    servers.delete(ctx.instanceId);
                    await new Promise((resolve) => entry.server.close(resolve));
                }
            },
        }),
    ],
});

process.on("uncaughtException", (error) => {
    void session.log(`LXR GC roadmap extension failed: ${error.message}`, { level: "error" })
        .finally(() => process.exit(1));
});

process.on("unhandledRejection", (reason) => {
    void session.log(`LXR GC roadmap extension rejected: ${String(reason)}`, { level: "error" })
        .finally(() => process.exit(1));
});
