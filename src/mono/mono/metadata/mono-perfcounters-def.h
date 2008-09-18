/*
 * Define the system and runtime performance counters.
 * Each category is defined with the macro:
 * 	PERFCTR_CAT(catid, name, help, type, instances, first_counter_id)
 * and after that follows the counters inside the category, defined by the macro:
 * 	PERFCTR_COUNTER(counter_id, name, help, type)
 */
PERFCTR_CAT(CPU, "Processor", "", MultiInstance, CPU, CPU_USER_TIME)
PERFCTR_COUNTER(CPU_USER_TIME, "% User Time", "", Timer100Ns)
PERFCTR_COUNTER(CPU_PRIV_TIME, "% Privileged Time", "", Timer100Ns)
PERFCTR_COUNTER(CPU_INTR_TIME, "% Interrupt Time", "", Timer100Ns)
PERFCTR_COUNTER(CPU_DCP_TIME,  "% DCP Time", "", Timer100Ns)
PERFCTR_COUNTER(CPU_PROC_TIME, "% Processor Time", "", Timer100Ns)

PERFCTR_CAT(PROC, "Process", "", MultiInstance, Process, PROC_USER_TIME)
PERFCTR_COUNTER(PROC_USER_TIME, "% User Time", "", Timer100Ns)
PERFCTR_COUNTER(PROC_PRIV_TIME, "% Privileged Time", "", Timer100Ns)
PERFCTR_COUNTER(PROC_PROC_TIME, "% Processor Time", "", Timer100Ns)
PERFCTR_COUNTER(PROC_THREADS,   "Thread Count", "", NumberOfItems64)
PERFCTR_COUNTER(PROC_VBYTES,    "Virtual Bytes", "", NumberOfItems64)
PERFCTR_COUNTER(PROC_WSET,      "Working Set", "", NumberOfItems64)
PERFCTR_COUNTER(PROC_PBYTES,    "Private Bytes", "", NumberOfItems64)

/* sample runtime counter */
PERFCTR_CAT(MONO_MEM, "Mono Memory", "", SingleInstance, Mono, MEM_NUM_OBJECTS)
PERFCTR_COUNTER(MEM_NUM_OBJECTS, "Allocated Objects", "", NumberOfItems64)

PERFCTR_CAT(ASPNET, "ASP.NET", "", MultiInstance, Mono, ASPNET_REQ_Q)
PERFCTR_COUNTER(ASPNET_REQ_Q, "Requests Queued", "", NumberOfItems64)

PERFCTR_CAT(JIT, ".NET CLR JIT", "", MultiInstance, Mono, JIT_BYTES)
PERFCTR_COUNTER(JIT_BYTES, "# of IL Bytes JITted", "", NumberOfItems32)
PERFCTR_COUNTER(JIT_METHODS, "# of IL Methods JITted", "", NumberOfItems32)
PERFCTR_COUNTER(JIT_TIME, "% Time in JIT", "", Timer100Ns)
PERFCTR_COUNTER(JIT_BYTES_PSEC, "IL Bytes Jitted/Sec", "", RateOfCountsPerSecond32)
PERFCTR_COUNTER(JIT_FAILURES, "Standard Jit Failures", "", NumberOfItems32)

PERFCTR_CAT(EXC, ".NET CLR Exceptions", "", MultiInstance, Mono, EXC_THROWN)
PERFCTR_COUNTER(EXC_THROWN, "# of Exceps Thrown", "", NumberOfItems32)
PERFCTR_COUNTER(EXC_THROWN_PSEC, "# of Exceps Thrown/Sec", "", RateOfCountsPerSecond32)
PERFCTR_COUNTER(EXC_FILTERS_PSEC, "# of Filters/Sec", "", RateOfCountsPerSecond32)
PERFCTR_COUNTER(EXC_FINALLYS_PSEC, "# of Finallys/Sec", "", RateOfCountsPerSecond32)
PERFCTR_COUNTER(EXC_CATCH_DEPTH, "Throw to Catch Depth/Sec", "", NumberOfItems32)

