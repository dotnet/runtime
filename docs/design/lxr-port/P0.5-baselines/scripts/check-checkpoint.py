#!/usr/bin/env python
"""Structural check of a P0.5 schema-v2 checkpoint file.

Checks the shape the canvas documents, and in particular the two failure modes the brief calls out
by name: a metric emitted as 0 where it means "not measured", and a record that claims a latency
percentile without saying how latency was measured.

Exits 0 with a one-line summary, or 1 with the reason.
"""
import json
import sys

V2_ONLY = (
    "latencyP50Ms", "latencyP99Ms", "latencyP999Ms", "latencyP9999Ms", "latencyMaxMs",
    "arrivalRatePerSecond", "achievedRatePerSecond", "heapFactor", "heapLimitMb",
)
PAUSE_FIELDS = ("pauseAverageMs", "pauseP99Ms", "pauseMaxMs")


def fail(message):
    print(message)
    sys.exit(1)


def main():
    path = sys.argv[1]
    try:
        with open(path, "r", encoding="utf-8") as handle:
            document = json.load(handle)
    except (OSError, ValueError) as error:
        fail("not valid JSON: %s" % error)

    if not isinstance(document, dict):
        fail("top level is %s, not an object" % type(document).__name__)

    version = document.get("schemaVersion")
    if version not in (1, 2):
        fail("schemaVersion is %r; the canvas ignores anything but 1 or 2" % (version,))

    checkpoints = document.get("checkpoints")
    if not isinstance(checkpoints, list):
        fail("checkpoints is %r, not a top-level array" % type(checkpoints).__name__)
    if not checkpoints:
        fail("checkpoints is empty")

    total = 0
    charted = 0
    excluded = 0
    for index, checkpoint in enumerate(checkpoints):
        for field in ("id", "date", "results"):
            if field not in checkpoint:
                fail("checkpoint %d has no %r" % (index, field))
        date = checkpoint["date"]
        if len(date) != 10 or date[4] != "-" or date[7] != "-":
            fail("checkpoint %r date %r is not YYYY-MM-DD" % (checkpoint["id"], date))
        if not isinstance(checkpoint["results"], list):
            fail("checkpoint %r results is not an array" % checkpoint["id"])

        for result in checkpoint["results"]:
            total += 1
            if not result.get("scenario") or not result.get("collector"):
                fail("a record in %r has no scenario or collector" % checkpoint["id"])

            status = result.get("status", "ok")
            valid = result.get("valid", True)
            if status != "ok" or not valid:
                excluded += 1
                # A failed or invalid record must say why, or it is just a hole.
                if not result.get("invalidReason") and not result.get("skipReason"):
                    fail("%s/%s is status=%s valid=%s but gives no reason"
                         % (result["scenario"], result["collector"], status, valid))
                continue
            charted += 1

            # The P0.4 emitter bug: `?? 0` published a zero for a metric never measured, which reads
            # as a collector with perfect zero pauses. A real zero pause time does not occur in a
            # scenario that ran for seconds, so a literal 0 here is treated as the bug it was.
            for field in PAUSE_FIELDS:
                if field in result and result[field] == 0:
                    fail("%s/%s has %s == 0; null means not measured and must not be emitted as zero"
                         % (result["scenario"], result["collector"], field))

            # An open-loop latency number without its method is not coordinated-omission-free, and
            # without its offered load it is not interpretable at all.
            has_latency = any(
                isinstance(result.get(field), (int, float)) for field in
                ("latencyP50Ms", "latencyP99Ms", "latencyP999Ms", "latencyP9999Ms", "latencyMaxMs")
            )
            if has_latency:
                if not result.get("latencyMethod"):
                    fail("%s/%s publishes latency percentiles with no latencyMethod"
                         % (result["scenario"], result["collector"]))
                if not isinstance(result.get("arrivalRatePerSecond"), (int, float)):
                    fail("%s/%s publishes latency with no arrivalRatePerSecond"
                         % (result["scenario"], result["collector"]))

            # A throughput number without its heap factor is unusable: the paper's results invert
            # with heap generosity.
            if isinstance(result.get("operationsPerSecond"), (int, float)):
                if not isinstance(result.get("heapFactor"), (int, float)):
                    fail("%s/%s publishes throughput with no heapFactor"
                         % (result["scenario"], result["collector"]))

            if not result.get("coreclrSha256"):
                fail("%s/%s has no coreclrSha256; it is not tied to a build"
                     % (result["scenario"], result["collector"]))

    if version == 2:
        seen = set()
        for checkpoint in checkpoints:
            for result in checkpoint["results"]:
                seen.update(field for field in V2_ONLY if field in result)
        if not seen:
            fail("schemaVersion 2 but no v2 field appears anywhere; the version claim is unearned")

    print("%d record(s), %d chartable, %d excluded, schemaVersion %d" % (total, charted, excluded, version))
    return 0


if __name__ == "__main__":
    sys.exit(main())
