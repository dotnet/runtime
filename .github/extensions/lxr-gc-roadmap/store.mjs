import { createHash, randomUUID } from "node:crypto";
import { mkdir, readFile, readdir, rename, rm, writeFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const extensionDirectory = dirname(fileURLToPath(import.meta.url));
const roadmapPath = join(extensionDirectory, "roadmap.md");
const benchmarkResultsDirectory = join(extensionDirectory, "benchmark-results");
const statuses = new Set(["planned", "in_progress", "done", "failed"]);
const currentSchemaVersion = 2;
const supportedSchemaVersions = new Set([1, 2]);
let mutationQueue = Promise.resolve();

function serializeMutation(operation) {
    const result = mutationQueue.then(operation, operation);
    mutationQueue = result.then(
        () => undefined,
        () => undefined,
    );
    return result;
}

function parseField(body, name) {
    const match = body.match(new RegExp(`^- \\*\\*${name}:\\*\\*[ \\t]*(.*)$`, "mi"));
    return match?.[1]?.trim() ?? "";
}

const KNOWN_STEP_FIELDS = new Map([
    ["status", "status"],
    ["summary", "summary"],
    ["correctness", "correctness"],
    ["benchmarks", "benchmarks"],
    ["runtime changes", "runtimeChanges"],
    ["evidence", "evidence"],
    ["dependencies", "dependencies"],
    ["references", "references"],
]);

/// Collects every `- **Name:** value` field in a step body. Fields the schema does not know are
/// returned in `extra` rather than discarded: a roadmap entry is agent-authored prose, and a
/// field that vanishes with no error reads exactly like a field nobody wrote.
function parseStepFields(stepBody) {
    const known = {};
    const extra = [];
    for (const match of stepBody.matchAll(/^- \*\*(.+?):\*\*[ \t]*(.*)$/gm)) {
        const label = match[1].trim();
        const value = match[2].trim();
        const target = KNOWN_STEP_FIELDS.get(label.toLowerCase());
        if (target) {
            known[target] = value;
        } else {
            extra.push({ label, value });
        }
    }

    return { known, extra };
}

function parseRoadmap(markdown) {
    const phases = [];
    const phasePattern = /^## (P\d+) — (.+)$/gm;
    const phaseMatches = [...markdown.matchAll(phasePattern)];

    for (let index = 0; index < phaseMatches.length; index++) {
        const match = phaseMatches[index];
        const start = match.index + match[0].length;
        const end = phaseMatches[index + 1]?.index ?? markdown.length;
        const body = markdown.slice(start, end);
        const firstStep = body.search(/^### /m);
        const phaseBody = firstStep >= 0 ? body.slice(0, firstStep) : body;
        const steps = [];
        const stepPattern = /^### (P\d+\.\d+) — (.+)$/gm;
        const stepMatches = [...body.matchAll(stepPattern)];

        for (let stepIndex = 0; stepIndex < stepMatches.length; stepIndex++) {
            const stepMatch = stepMatches[stepIndex];
            const stepStart = stepMatch.index + stepMatch[0].length;
            const stepEnd = stepMatches[stepIndex + 1]?.index ?? body.length;
            const stepBody = body.slice(stepStart, stepEnd);
            const { known, extra } = parseStepFields(stepBody);
            steps.push({
                id: stepMatch[1],
                title: stepMatch[2].trim(),
                status: known.status || "planned",
                summary: known.summary ?? "",
                correctness: known.correctness ?? "",
                benchmarks: known.benchmarks ?? "",
                runtimeChanges: known.runtimeChanges ?? "",
                evidence: known.evidence ?? "",
                dependencies: known.dependencies ?? "",
                references: known.references ?? "",
                extra,
            });
        }

        phases.push({
            id: match[1],
            title: match[2].trim(),
            status: parseField(phaseBody, "Status") || "planned",
            summary: parseField(phaseBody, "Summary"),
            steps,
        });
    }

    return phases;
}

async function writeAtomic(path, content) {
    const temporaryPath = `${path}.${process.pid}.${randomUUID()}.tmp`;
    try {
        await writeFile(temporaryPath, content, "utf8");
        await rename(temporaryPath, path);
    } finally {
        await rm(temporaryPath, { force: true });
    }
}

function requireText(value, name) {
    if (typeof value !== "string" || value.trim().length === 0) {
        throw new Error(`${name} must be a non-empty string.`);
    }
    return value.trim();
}

function optionalMetric(value, name) {
    if (value === undefined || value === null) {
        return null;
    }
    if (typeof value !== "number" || !Number.isFinite(value) || value < 0) {
        throw new Error(`${name} must be a finite, non-negative number.`);
    }
    return value;
}

export async function readRoadmap() {
    return readFile(roadmapPath, "utf8");
}

async function readBenchmarkDocuments() {
    await mkdir(benchmarkResultsDirectory, { recursive: true });
    const files = (await readdir(benchmarkResultsDirectory))
        .filter((file) => file.endsWith(".json"))
        .sort();
    return Promise.all(
        files.map(async (file) => {
            try {
                return {
                    file,
                    document: JSON.parse(await readFile(join(benchmarkResultsDirectory, file), "utf8")),
                };
            } catch (error) {
                return { file, document: null, problem: `is not valid JSON: ${error.message}` };
            }
        }),
    );
}

function collectCheckpoints(sources, problems) {
    const checkpoints = [];
    for (const { file, document, problem } of sources) {
        if (problem) {
            problems.push({ file, problem });
            continue;
        }
        if (!supportedSchemaVersions.has(document.schemaVersion)) {
            problems.push({
                file,
                problem: `declares schemaVersion ${JSON.stringify(document.schemaVersion)}; expected one of ${[...supportedSchemaVersions].join(", ")}`,
            });
            continue;
        }
        if (!Array.isArray(document.checkpoints)) {
            problems.push({ file, problem: "has no top-level `checkpoints` array" });
            continue;
        }
        for (const checkpoint of document.checkpoints) {
            const invalid = describeCheckpointProblem(checkpoint);
            if (invalid) {
                problems.push({ file, problem: invalid });
                continue;
            }
            checkpoints.push({ ...checkpoint, sourceFile: file });
        }
    }
    return checkpoints;
}

function describeCheckpointProblem(checkpoint) {
    if (!checkpoint || typeof checkpoint !== "object") {
        return "contains a checkpoint that is not an object";
    }
    if (typeof checkpoint.id !== "string" || !checkpoint.id) {
        return "contains a checkpoint with no `id`";
    }
    if (typeof checkpoint.date !== "string" || !/^\d{4}-\d{2}-\d{2}$/.test(checkpoint.date)) {
        return `checkpoint ${checkpoint.id} has no valid YYYY-MM-DD \`date\``;
    }
    if (!Array.isArray(checkpoint.results)) {
        return `checkpoint ${checkpoint.id} has no \`results\` array`;
    }
    return null;
}

async function readBenchmarks() {
    const sources = await readBenchmarkDocuments();
    const problems = [];
    const checkpoints = collectCheckpoints(sources, problems)
        .sort((left, right) => left.date.localeCompare(right.date) || left.id.localeCompare(right.id));

    return {
        schemaVersion: currentSchemaVersion,
        metrics: Object.assign({}, ...sources.map(({ document }) => document?.metrics ?? {})),
        checkpoints,
        problems,
        sourceFiles: sources.map(({ file }) => file),
    };
}

export async function getState() {
    const [markdown, benchmarks] = await Promise.all([
        readRoadmap(),
        readBenchmarks(),
    ]);
    const phases = parseRoadmap(markdown);
    const steps = phases.flatMap((phase) => phase.steps);

    return {
        phases,
        benchmarks,
        markdown,
        roadmapRevision: createHash("sha256").update(markdown).digest("hex"),
        progress: {
            total: steps.length,
            planned: steps.filter((step) => step.status === "planned").length,
            inProgress: steps.filter((step) => step.status === "in_progress").length,
            done: steps.filter((step) => step.status === "done").length,
            failed: steps.filter((step) => step.status === "failed").length,
        },
    };
}

export async function updateStep(input) {
    return serializeMutation(async () => {
        const id = requireText(input?.id, "id");
        let markdown = await readRoadmap();
        const escapedId = id.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
        const heading = new RegExp(`(^### ${escapedId} — .+$)([\\s\\S]*?)(?=^### |^## |$(?![\\s\\S]))`, "m");
        const match = markdown.match(heading);
        if (!match) {
            throw new Error(`Unknown roadmap step: ${id}`);
        }

        let section = match[0];
        const updates = {
            Status: input.status,
            Summary: input.summary,
            Correctness: input.correctness,
            Benchmarks: input.benchmarks,
            "Runtime changes": input.runtimeChanges,
        };

        if (input.status !== undefined && !statuses.has(input.status)) {
            throw new Error("status must be planned, in_progress, done, or failed.");
        }

        for (const [field, value] of Object.entries(updates)) {
            if (value === undefined) {
                continue;
            }
            const normalized = String(value).replace(/\r?\n/g, " ").trim();
            const fieldPattern = new RegExp(`^- \\*\\*${field}:\\*\\*.*$`, "mi");
            if (!fieldPattern.test(section)) {
                if (field === "Runtime changes") {
                    section = section.replace(
                        /^- \*\*Dependencies:\*\*/mi,
                        () => `- **Runtime changes:** ${normalized}\n- **Dependencies:**`,
                    );
                    continue;
                }
                throw new Error(`Roadmap step ${id} has no ${field} field.`);
            }
            section = section.replace(fieldPattern, () => `- **${field}:** ${normalized}`);
        }

        markdown = markdown.replace(match[0], () => section);
        await writeAtomic(roadmapPath, markdown);
        return getState();
    });
}

export async function addBenchmarkResult(input) {
    return serializeMutation(async () => {
        const checkpoint = requireText(input?.checkpoint, "checkpoint");
        const date = requireText(input?.date, "date");
        const stepId = requireText(input?.stepId, "stepId");
        const scenario = requireText(input?.scenario, "scenario");
        const collector = requireText(input?.collector, "collector");

        if (!["lxr", "workstation", "server"].includes(collector)) {
            throw new Error("collector must be lxr, workstation, or server.");
        }
        if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) {
            throw new Error("date must use YYYY-MM-DD format.");
        }
        const phases = parseRoadmap(await readRoadmap());
        if (!phases.some((phase) => phase.steps.some((step) => step.id === stepId))) {
            throw new Error(`Unknown roadmap step: ${stepId}`);
        }

        const sources = await readBenchmarkDocuments();
        const usable = sources.filter(({ document }) => Array.isArray(document?.checkpoints));
        const source = usable.find(({ document }) =>
            document.checkpoints.some((item) => item?.id === checkpoint),
        );
        const existing = source?.document.checkpoints.find((item) => item.id === checkpoint);
        if (existing && (existing.date !== date || existing.stepId !== stepId)) {
            throw new Error(
                `Checkpoint ${checkpoint} already belongs to ${existing.stepId} on ${existing.date}.`,
            );
        }
        const entry = existing ?? { id: checkpoint, date, stepId, notes: "", results: [] };

        const result = {
            scenario,
            collector,
            operationsPerSecond: optionalMetric(input.operationsPerSecond, "operationsPerSecond"),
            pauseAverageMs: optionalMetric(input.pauseAverageMs, "pauseAverageMs"),
            pauseP99Ms: optionalMetric(input.pauseP99Ms, "pauseP99Ms"),
            pauseMaxMs: optionalMetric(input.pauseMaxMs, "pauseMaxMs"),
            workingSetMb: optionalMetric(input.workingSetMb, "workingSetMb"),
            committedMb: optionalMetric(input.committedMb, "committedMb"),
            notes: typeof input.notes === "string" ? input.notes.trim() : "",
        };
        const resultIndex = entry.results.findIndex(
            (item) => item.scenario === scenario && item.collector === collector,
        );
        if (resultIndex >= 0) {
            entry.results[resultIndex] = result;
        } else {
            entry.results.push(result);
        }

        const safeCheckpoint = checkpoint.replace(/[^A-Za-z0-9._-]+/g, "-");
        const document = source?.document ?? {
            schemaVersion: currentSchemaVersion,
            checkpoints: [entry],
        };
        const destination = source?.file ?? `${safeCheckpoint}.json`;
        await writeAtomic(
            join(benchmarkResultsDirectory, destination),
            `${JSON.stringify(document, null, 4)}\n`,
        );
        return getState();
    });
}

export async function replaceRoadmap(markdown, expectedRevision) {
    return serializeMutation(async () => {
        if (typeof markdown !== "string" || !markdown.includes("<!-- lxr-gc-roadmap:v1 -->")) {
            throw new Error("Markdown must contain the lxr-gc-roadmap:v1 format marker.");
        }
        const currentMarkdown = await readRoadmap();
        const currentRevision = createHash("sha256").update(currentMarkdown).digest("hex");
        if (expectedRevision !== currentRevision) {
            throw new Error("The roadmap changed after the editor loaded. Refresh before saving.");
        }
        const phases = parseRoadmap(markdown);
        if (phases.length === 0 || phases.every((phase) => phase.steps.length === 0)) {
            throw new Error("Markdown must contain at least one phase and one step.");
        }
        const invalidStatus = phases
            .flatMap((phase) => [phase.status, ...phase.steps.map((step) => step.status)])
            .find((status) => !statuses.has(status));
        if (invalidStatus) {
            throw new Error(`Invalid roadmap status: ${invalidStatus}`);
        }
        await writeAtomic(roadmapPath, markdown.endsWith("\n") ? markdown : `${markdown}\n`);
        return getState();
    });
}
