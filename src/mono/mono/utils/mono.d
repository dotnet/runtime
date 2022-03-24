/* 
 * mono.d: DTrace provider for Mono
 * 
 * Authors:
 *   Andreas Faerber <andreas.faerber@web.de>
 * 
 */

provider mono {
	/* Virtual Execution System (VES) */
	probe ves__init__begin ();
	probe ves__init__end ();

	/* Just-in-time compiler (JIT) */
	probe method__compile__begin (char* class_name, char* method_name, char* signature);
	probe method__compile__end (char* class_name, char* method_name, char* signature, int success);

	/* Garbage Collector (GC) */	
	probe gc__begin (int generation);
	probe gc__end (int generation);

	probe gc__requested (int generation, uintptr_t requested_size, int wait_to_finish);

	probe gc__concurrent__start__begin (int generation);
	probe gc__concurrent__update__finish__begin (int generation, long long num_major_objects_marked);

	probe gc__sweep__begin (int generation, int full_sweep);
	probe gc__sweep__end (int generation, int full_sweep);

	probe gc__world__stop__begin ();
	probe gc__world__stop__end ();
	probe gc__world__restart__begin (int generation);
	probe gc__world__restart__end (int generation);

	probe gc__nursery__tlab__alloc (uintptr_t addr, uintptr_t len);
	probe gc__nursery__obj__alloc (uintptr_t addr, uintptr_t size, char *ns_name, char *class_name);

	probe gc__major__obj__alloc__large (uintptr_t addr, uintptr_t size, char *ns_name, char *class_name);
	probe gc__major__obj__alloc__pinned (uintptr_t addr, uintptr_t size, char *ns_name, char *class_name);
	probe gc__major__obj__alloc__degraded (uintptr_t addr, uintptr_t size, char *ns_name, char *class_name);

	/* Can be nursery->nursery, nursery->major or major->major */
	probe gc__obj__moved (uintptr_t dest, uintptr_t src, int dest_gen, int src_gen, uintptr_t size, char *ns_name, char *class_name);

	probe gc__nursery__swept (uintptr_t addr, uintptr_t len);
	probe gc__major__swept (uintptr_t addr, uintptr_t len);

	probe gc__obj__pinned (uintptr_t addr, uintptr_t size, char *ns_name, char *class_name, int generation);

	probe gc__finalize__enqueue (uintptr_t addr, uintptr_t size, char *ns_name, char *class_name, int generation, int is_critical);
	probe gc__finalize__invoke (uintptr_t addr, uintptr_t size, char *ns_name, char *class_name);

	probe gc__weak__update (uintptr_t ref_addr, uintptr_t new_addr, uintptr_t size, char *ns_name, char *class_name, int track);

	probe gc__global__remset__add (uintptr_t ref_addr, uintptr_t obj_addr, uintptr_t size, char *ns_name, char *class_name);
	probe gc__obj__cemented (uintptr_t addr, uintptr_t size, char *ns_name, char *class_name);
};

#pragma D attributes Evolving/Evolving/Common provider mono provider
#pragma D attributes Private/Private/Unknown provider mono module
#pragma D attributes Private/Private/Unknown provider mono function
#pragma D attributes Evolving/Evolving/Common provider mono name
#pragma D attributes Evolving/Evolving/Common provider mono args
