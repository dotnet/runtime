"""Research-only gdb observer (P24): attach to a running slow-mode run and print the state of the concurrent trace.
Nothing is modified. usage: gdb -batch -x trace-state.py -p <pid>   (the frame must have the .dbg file next to the .so)
"""
import gdb

gdb.execute("set pagination off")
gdb.execute("set confirm off")


def v(expr):
    return gdb.parse_and_eval(expr)


def atom(expr):
    """Value of a std::atomic member."""
    return v(expr + "._M_i") if False else v(expr)


heap = "LxrGcHeap::s_heap"
eng = heap + "->m_engine"
out = {}
for name in ("m_trace_pending", "m_trace_phase", "m_hold_trace", "m_marking", "m_phase", "m_cycle_sweep"):
    try:
        out[name] = str(v(eng + "." + name))
    except gdb.error as error:
        out[name] = "err " + str(error)[:60]
print("ENGINE", out)
# Walk the work list.
kinds = {}
n = 0
first_unprocessed = []
try:
    w = v(eng + ".m_work._M_b._M_p")
except gdb.error:
    w = v(eng + ".m_work")
while int(w) != 0 and n < 3000000:
    n += 1
    kind = int(w["kind"])
    state = int(w["packet"]["m_state"]["_M_i"]) if False else -1
    try:
        processed = int(w["traceProcessed"]["_M_base"]["_M_i"])
        submitted = int(w["traceSubmitted"]["_M_base"]["_M_i"])
    except gdb.error:
        processed = submitted = -1
    key = (kind, processed, submitted)
    kinds[key] = kinds.get(key, 0) + 1
    if kind in (11, 12) and processed == 0 and len(first_unprocessed) < 5:
        first_unprocessed.append(str(w))
    w = w["next"]
print("WORK packets in list:", n)
for key, count in sorted(kinds.items()):
    print("  kind=%d traceProcessed=%d traceSubmitted=%d count=%d" % (key + (count,)))
print("sample unprocessed trace works:", first_unprocessed)
