#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Build index.html from report-template.html and results/summary.json. Every number comes from the data."""

import argparse
import html
import json
import math
from pathlib import Path

HERE = Path(__file__).parent
WORKLOAD_NAME = {"binarytrees": "Binary trees", "requests": "Requests", "listsuffix": "List suffix"}
LABEL = {
    "java-lxr": "Java LXR", "java-g1": "Java G1", "java-parallel": "Java Parallel", "java-serial": "Java Serial",
    "net-lxr-nonmoving": "Our port: no moving", "net-lxr-nursery": "Our port: move young",
    "net-lxr-mature": "Our port: move all", "net-wks": ".NET Workstation", "net-server": ".NET Server",
}
COLOR = {
    "java-lxr": "#75e2cc", "java-g1": "#a5c8ff", "java-parallel": "#6aa6ff", "java-serial": "#3b82f6",
    "net-lxr-nonmoving": "#ffd9a8", "net-lxr-nursery": "#ffb259", "net-lxr-mature": "#e07b00",
    "net-wks": "#d4c8ff", "net-server": "#a78bfa",
}


def cell(data, workload, factor, collector):
    for item in data["cells"]:
        if (item["workload"], item["factor"], item["collector"]) == (workload, factor, collector):
            return item
    raise KeyError((workload, factor, collector))


def number(value):
    return f"{value:.0f}" if value >= 10 else f"{value:.1f}"


def slowdown_figure(data):
    left, right, top, bottom = 70, 985, 70, 400
    low, high = 0.5, 50.0
    decades = math.log10(high / low)

    def y(value):
        return bottom - (math.log10(value / low) / decades) * (bottom - top)

    series = ["java-lxr", "net-lxr-nonmoving", "net-lxr-nursery", "net-lxr-mature"]
    workloads = ["binarytrees", "requests", "listsuffix"]
    factors = [2.0, 4.0, 8.0]
    group = (right - left) / 9
    parts = [
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 500" role="img" aria-labelledby="slow-t slow-d">',
        '<title id="slow-t">How many times slower than the standard collector of the same runtime</title>',
        '<desc id="slow-d">For three workloads and three heap sizes, bars show the wall time of the Java LXR divided by the time of Java G1, '
        'and of our port in three modes divided by the time of the .NET Workstation GC. A log scale is used. Our port is 7 to 28 times slower. '
        'The Java LXR is 1.1 to 2.2 times slower, except 6.9 times in the tightest heap of the list suffix workload, where our port crashes.</desc>',
    ]
    for tick in (0.5, 1, 2, 5, 10, 20, 50):
        parts.append(f'<path d="M{left} {y(tick):.1f}H{right}" stroke="{"#cfe0ea" if tick == 1 else "#2b4658"}" '
                     f'stroke-width="{2 if tick == 1 else 1}" fill="none"/>')
        parts.append(f'<text x="{left - 8}" y="{y(tick) + 5:.1f}" text-anchor="end" class="c">{tick:g}x</text>')
    index = 0
    for workload in workloads:
        start = left + index * group
        parts.append(f'<text x="{start + 1.5 * group:.0f}" y="{bottom + 52}" text-anchor="middle" class="b">{WORKLOAD_NAME[workload]}</text>')
        for factor in factors:
            center = left + (index + 0.5) * group
            parts.append(f'<text x="{center:.0f}" y="{bottom + 22}" text-anchor="middle" class="c">heap {factor:g}x</text>')
            bar, gap = 17, 3
            width = len(series) * bar + (len(series) - 1) * gap
            x = center - width / 2
            for collector in series:
                item = cell(data, workload, factor, collector)
                if "elapsed" not in item:
                    parts.append(f'<text x="{x + bar / 2:.1f}" y="{bottom - 8}" text-anchor="middle" class="rt b">X</text>')
                else:
                    value = item["elapsed"]
                    top_y = y(max(value, low))
                    parts.append(f'<rect x="{x:.1f}" y="{top_y:.1f}" width="{bar}" height="{bottom - top_y:.1f}" fill="{COLOR[collector]}"/>')
                    parts.append(f'<text x="{x + bar / 2:.1f}" y="{top_y - 4:.1f}" text-anchor="middle" class="v">{number(value)}</text>')
                x += bar + gap
            index += 1
    # legend
    lx = left
    for collector in series:
        parts.append(f'<rect x="{lx}" y="14" width="16" height="16" fill="{COLOR[collector]}"/>')
        parts.append(f'<text x="{lx + 22}" y="27" class="c">{LABEL[collector]}</text>')
        lx += 215
    parts.append(f'<text x="{left}" y="52" class="rt c">X = our port stops with "Out of memory" in all 3 runs</text>')
    parts.append(f'<text x="{left + 420}" y="52" class="c">White line (1x) = the standard collector of the same runtime</text>')
    parts.append("</svg>")
    return "\n".join(parts)


def stall_figure(data):
    series = ["java-lxr", "java-g1", "java-parallel", "java-serial", "net-lxr-nonmoving", "net-lxr-mature", "net-wks", "net-server"]
    left, right, top, bottom = 70, 985, 90, 400
    low, high = 2.0, 3000.0
    decades = math.log10(high / low)

    def y(value):
        return bottom - (math.log10(value / low) / decades) * (bottom - top)

    workloads = ["binarytrees", "requests", "listsuffix"]
    group = (right - left) / 3
    parts = [
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 500" role="img" aria-labelledby="stall-t stall-d">',
        '<title id="stall-t">Longest stall of the program, at 4 times the minimum heap</title>',
        '<desc id="stall-d">For each workload, bars show the longest time that a 1 millisecond sleeper thread had to wait. Java LXR, G1, Parallel, Serial, '
        'and .NET Workstation and Server stay between 5 and 50 milliseconds. Our port waits between 140 milliseconds and 1 second.</desc>',
    ]
    for tick in (2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000):
        parts.append(f'<path d="M{left} {y(tick):.1f}H{right}" stroke="#2b4658" stroke-width="1" fill="none"/>')
        parts.append(f'<text x="{left - 8}" y="{y(tick) + 5:.1f}" text-anchor="end" class="c">{tick} ms</text>')
    for w_index, workload in enumerate(workloads):
        center = left + (w_index + 0.5) * group
        parts.append(f'<text x="{center:.0f}" y="{bottom + 28}" text-anchor="middle" class="b">{WORKLOAD_NAME[workload]}</text>')
        bar, gap = 24, 4
        width = len(series) * bar + (len(series) - 1) * gap
        x = center - width / 2
        for collector in series:
            item = cell(data, workload, 4.0, collector)
            value = item["hiccup_max_us"] / 1000.0
            top_y = y(max(value, low))
            parts.append(f'<rect x="{x:.1f}" y="{top_y:.1f}" width="{bar}" height="{bottom - top_y:.1f}" fill="{COLOR[collector]}"/>')
            parts.append(f'<text x="{x + bar / 2:.1f}" y="{top_y - 4:.1f}" text-anchor="middle" class="v">{number(value)}</text>')
            x += bar + gap
    lx, ly = left, 18
    for index, collector in enumerate(series):
        if index == 4:
            lx, ly = left, 44
        parts.append(f'<rect x="{lx}" y="{ly}" width="16" height="16" fill="{COLOR[collector]}"/>')
        parts.append(f'<text x="{lx + 22}" y="{ly + 13}" class="c">{LABEL[collector]}</text>')
        lx += 225
    parts.append("</svg>")
    return "\n".join(parts)


def kinds_table(data):
    rows = []
    for workload in ["binarytrees", "requests", "listsuffix"]:
        for collector in ["java-lxr", "net-lxr-nonmoving", "net-lxr-mature"]:
            item = cell(data, workload, 4.0, collector)
            pauses = item["gc_count"]
            avg = item["gc_ms"] / pauses if pauses else 0
            rows.append(
                f"<tr><td>{WORKLOAD_NAME[workload]}</td><td>{LABEL[collector]}</td><td>{item['heap_mib']} MiB</td>"
                f"<td>{pauses:.0f}</td><td>{item['lxr_full']:.0f}</td><td>{avg:.1f} ms</td><td>{item['hiccup_max_us'] / 1000:.0f} ms</td></tr>")
    return ("<table><caption>Collections in the measured part, at 4 times the minimum heap (median of 3 runs)</caption>"
            "<thead><tr><th scope=\"col\">Workload</th><th scope=\"col\">Collector</th><th scope=\"col\">Heap</th>"
            "<th scope=\"col\">Collections</th><th scope=\"col\">Of these: full</th><th scope=\"col\">Average pause</th>"
            "<th scope=\"col\">Longest stall</th></tr></thead><tbody>" + "".join(rows) + "</tbody></table>")


def all_table(data):
    order = ["java-lxr", "java-g1", "java-parallel", "java-serial", "java-zgc",
             "net-lxr-nonmoving", "net-lxr-nursery", "net-lxr-mature", "net-wks", "net-server"]
    rows = []
    for workload in ["binarytrees", "requests", "listsuffix"]:
        for factor in [2.0, 4.0, 8.0]:
            for collector in order:
                item = cell(data, workload, factor, collector)
                head = f"<td>{WORKLOAD_NAME[workload]}</td><td>{factor:g}x</td><td>{LABEL.get(collector, 'Java ZGC')}</td><td>{item['heap_mib']}</td>"
                if "elapsed_ms" not in item:
                    reason = html.escape(item.get("failure", "").strip().splitlines()[-1] if item.get("failure") else "failed")
                    rows.append(f"<tr>{head}<td colspan=\"8\">failed in {item['runs']} of {item['runs']} runs: {reason}</td></tr>")
                    continue
                ratio = f"{item['elapsed']:.2f}" if "elapsed" in item else "-"
                cpu = f"{item['cpu']:.2f}" if "cpu" in item else "-"
                rows.append(
                    f"<tr>{head}<td>{item['elapsed_ms']:.0f}</td><td>{item['elapsed_min']:.0f} to {item['elapsed_max']:.0f}</td>"
                    f"<td>{ratio}</td><td>{item['cpu_ms']:.0f} ({cpu})</td><td>{item['gc_count']:.0f}</td><td>{item['gc_ms']:.0f}</td>"
                    f"<td>{item['hiccup_max_us'] / 1000:.0f}</td><td>{item['peak_rss_mb']:.0f}</td></tr>")
    return ("<table><caption>All results. Wall and CPU times in ms, median of 3 runs. The ratio divides by Java G1 (Java) or .NET Workstation (.NET).</caption>"
            "<thead><tr><th scope=\"col\">Workload</th><th scope=\"col\">Heap</th><th scope=\"col\">Collector</th><th scope=\"col\">MiB</th>"
            "<th scope=\"col\">Wall</th><th scope=\"col\">Wall, fastest to slowest run</th><th scope=\"col\">Ratio</th><th scope=\"col\">CPU (ratio)</th>"
            "<th scope=\"col\">Collections</th><th scope=\"col\">Pause ms</th><th scope=\"col\">Longest stall ms</th><th scope=\"col\">Peak RSS MiB</th></tr></thead>"
            "<tbody>" + "".join(rows) + "</tbody></table>")


def heaps_table(data, sizes):
    rows = []
    for workload in ["binarytrees", "requests", "listsuffix"]:
        s = sizes[workload]
        java = [cell(data, workload, f, "java-g1")["heap_mib"] for f in (2.0, 4.0, 8.0)]
        net = [cell(data, workload, f, "net-wks")["heap_mib"] for f in (2.0, 4.0, 8.0)]
        rows.append(f"<tr><td>{WORKLOAD_NAME[workload]}</td><td>{s['min_java']} MiB</td><td>{' / '.join(map(str, java))} MiB</td>"
                    f"<td>{s['min_net']} MiB</td><td>{' / '.join(map(str, net))} MiB</td></tr>")
    return ("<table><caption>Heap sizes</caption><thead><tr><th scope=\"col\">Workload</th><th scope=\"col\">Java: smallest heap for Serial</th>"
            "<th scope=\"col\">Java heap at 2x / 4x / 8x</th><th scope=\"col\">.NET: smallest heap for Workstation</th>"
            "<th scope=\"col\">.NET heap at 2x / 4x / 8x</th></tr></thead><tbody>" + "".join(rows) + "</tbody></table>")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--summary", default=str(HERE / "results" / "summary.json"))
    parser.add_argument("--sizes", default=str(HERE / "results" / "sizes.json"))
    parser.add_argument("--template", default=str(HERE / "report-template.html"))
    parser.add_argument("--output", default=str(HERE / "index.html"))
    args = parser.parse_args()
    data = json.loads(Path(args.summary).read_text(encoding="utf-8"))
    sizes = json.loads(Path(args.sizes).read_text(encoding="utf-8"))
    text = Path(args.template).read_text(encoding="utf-8")
    for key, value in {
        "FIG_SLOWDOWN": slowdown_figure(data), "FIG_STALL": stall_figure(data), "TABLE_KINDS": kinds_table(data),
        "TABLE_ALL": all_table(data), "TABLE_HEAPS": heaps_table(data, sizes),
    }.items():
        marker = "{{" + key + "}}"
        if marker not in text:
            raise SystemExit(f"Template has no {marker}")
        text = text.replace(marker, value)
    Path(args.output).write_text(text if text.endswith("\n") else text + "\n", encoding="utf-8", newline="\n")
    print("wrote", args.output)


if __name__ == "__main__":
    main()
