"""Research-only gdb observer (P24): at every end of a lazy sweep (LxrGcHeap::FinishLazy), record what the policy sees and decides.
Nothing is modified. Environment: LXR_GDB_OUTPUT = output json file. Needs the .dbg file next to the .so.
Every value is read in its own try block: a function that the compiler inlined gives null, not a lost record.
"""
import gdb
import json
import os

records = []
STATE_NAMES = ["Unallocated", "Unmarked", "Marked", "Reusable"]
KINDS = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}


def call(expression):
    return gdb.parse_and_eval(expression)


def get(expression, convert=int):
    try:
        return convert(call(expression))
    except gdb.error as error:
        return None


def inspect():
    engine = int(gdb.parse_and_eval("$rdi"))
    heap = int(gdb.parse_and_eval("$rsi"))
    E = "((LxrReferenceCounting*)%d)" % engine
    H = "((LxrGcHeap*)%d)" % heap
    record = {"n": len(records), "_heap": heap}
    kind = get(H + "->m_current_pause_kind")
    record["pause_kind"] = KINDS.get(kind, kind)
    record["admitted_pages"] = (get(E + "->GetAdmittedBytes()") or 0) // 4096
    record["immix_capacity_blocks"] = (get(E + "->GetImmixCapacityBytes()") or 0) // 32768
    record["used_pages"] = get("(unsigned long)(" + E + "->GetUsedBytesEstimate()/4096)")
    record["mature_live_pages_before"] = get(H + "->m_policy.m_mature._M_i", float)
    buffer = int(call("(unsigned long)malloc(64)"))
    if get(E + "->GetMaturePages((unsigned long*)%d)" % buffer) is not None:
        record["mature_pages"] = get("*(unsigned long*)%d" % buffer)
    if len(records) < 12 or len(records) % 6 == 0:
        histogram = [0, 0, 0, 0]
        arena = get(E + "->GetReservationStart()")
        ok = arena is not None
        for block in range(record["immix_capacity_blocks"] if ok else 0):
            if get(E + "->m_blocks.GetState(%d, (ImmixBlockState*)%d, (unsigned char*)%d)"
                   % (arena + block * 32768, buffer, buffer + 8)) is None:
                ok = False
                break
            histogram[get("*(unsigned char*)%d" % buffer)] += 1
        if ok:
            record["block_states"] = dict(zip(STATE_NAMES, histogram))
            record["non_free_blocks"] = sum(histogram[1:])
    call("(void)free((void*)%d)" % buffer)
    return record


def after(record):
    heap = record.pop("_heap")
    H = "((LxrGcHeap*)%d)" % heap
    record["cycle_hint_after"] = get(H + "->m_policy.m_cycle._M_base._M_i")
    record["emergency_hint_after"] = get(H + "->m_policy.m_emergency._M_base._M_i")
    record["mature_live_pages_after"] = get(H + "->m_policy.m_mature._M_i", float)
    record["cause"] = get("(int)" + H + "->m_collection_cause._M_i")


gdb.execute("set pagination off")
gdb.execute("set confirm off")
gdb.execute("set breakpoint pending on")
for name in ("SIGUSR1", "SIGUSR2", "SIGPWR", "SIG34", "SIG35", "SIG36"):
    gdb.execute("handle %s nostop noprint pass" % name)
gdb.execute("break LxrGcHeap::FinishLazy")
gdb.execute("run")
while gdb.selected_inferior().pid != 0:
    try:
        record = inspect()
        records.append(record)
        gdb.execute("finish")
        after(record)
    except gdb.error as error:
        records.append({"n": len(records), "error": str(error)[:200]})
    with open(os.environ["LXR_GDB_OUTPUT"], "w") as stream:
        json.dump(records, stream, indent=1)
        stream.write("\n")
    if len(records) >= 80:
        break
    try:
        gdb.execute("continue")
    except gdb.error:
        break
gdb.execute("kill")
