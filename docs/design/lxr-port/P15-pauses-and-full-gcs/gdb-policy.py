"""Research-only gdb observer: at every policy update, ask the engine what the policy sees. Nothing is modified."""

import gdb
import json
import os

records = []
STATE_NAMES = ["Unallocated", "Unmarked", "Marked", "Reusable"]


def call(expression):
    return gdb.parse_and_eval(expression)


def inspect():
    engine = int(gdb.parse_and_eval("$rdi"))
    buffer = int(call("(unsigned long)malloc(64)"))
    record = {"n": len(records)}
    call("((LxrReferenceCounting*)%d)->GetMaturePages((unsigned long*)%d)" % (engine, buffer))
    record["mature_pages"] = int(call("*(unsigned long*)%d" % buffer))
    record["admitted_pages"] = int(call("((LxrReferenceCounting*)%d)->GetAdmittedBytes()" % engine)) // 4096
    call("((LxrReferenceCounting*)%d)->GetUsedPages((unsigned long*)%d)" % (engine, buffer))
    record["used_pages"] = int(call("*(unsigned long*)%d" % buffer))
    record["immix_capacity_blocks"] = int(call("((LxrReferenceCounting*)%d)->GetImmixCapacityBytes()" % engine)) // 32768
    record["live_bytes"] = int(call("((LxrReferenceCounting*)%d)->GetLiveBytes()" % engine))
    if len(records) < 14:
        histogram = [0, 0, 0, 0]
        arena = int(call("((LxrReferenceCounting*)%d)->GetReservationStart()" % engine))
        for block in range(record["immix_capacity_blocks"]):
            call("((LxrReferenceCounting*)%d)->m_blocks.GetState(%d, (ImmixBlockState*)%d, (unsigned char*)%d)"
                 % (engine, arena + block * 32768, buffer, buffer + 8))
            histogram[int(call("*(unsigned char*)%d" % buffer))] += 1
        record["block_states"] = dict(zip(STATE_NAMES, histogram))
    call("(void)free((void*)%d)" % buffer)
    return record


gdb.execute("set pagination off")
gdb.execute("set confirm off")
gdb.execute("set breakpoint pending on")
for name in ("SIGUSR1", "SIGUSR2", "SIGPWR", "SIG34", "SIG35", "SIG36"):
    gdb.execute("handle %s nostop noprint pass" % name)
gdb.execute("break LxrGcHeap::FinishLazy")
gdb.execute("run")
while gdb.selected_inferior().pid != 0:
    try:
        records.append(inspect())
    except gdb.error as error:  # diagnostic script: report, never hide
        records.append({"n": len(records), "error": str(error)})
    with open(os.environ["LXR_GDB_OUTPUT"], "w") as stream:
        json.dump(records, stream, indent=1)
        stream.write("\n")
    if len(records) >= 40:
        break
    try:
        gdb.execute("continue")
    except gdb.error:
        break
gdb.execute("kill")
