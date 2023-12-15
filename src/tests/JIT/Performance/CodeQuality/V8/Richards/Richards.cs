// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This is a C# implementation of the Richards benchmark from:
//
//    http://www.cl.cam.ac.uk/~mr10/Bench.html
//
// The benchmark was originally implemented in BCPL by Martin Richards.

#define INTF_FOR_TASK

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace V8.Richards
{
    /// <summary>
    /// Support is used for a place to generate any 'miscellaneous' methods generated as part
    /// of code generation, (which do not have user-visible names)
    /// </summary>
    public class Support
    {
        public static bool runRichards()
        {
            Scheduler scheduler = new Scheduler();
            scheduler.addIdleTask(ID_IDLE, 0, null, COUNT);
            Packet queue = new Packet(null, ID_WORKER, KIND_WORK);
            queue = new Packet(queue, ID_WORKER, KIND_WORK);
            scheduler.addWorkerTask(ID_WORKER, 1000, queue);

            queue = new Packet(null, ID_DEVICE_A, KIND_DEVICE);
            queue = new Packet(queue, ID_DEVICE_A, KIND_DEVICE);
            queue = new Packet(queue, ID_DEVICE_A, KIND_DEVICE);
            scheduler.addHandlerTask(ID_HANDLER_A, 2000, queue);

            queue = new Packet(null, ID_DEVICE_B, KIND_DEVICE);
            queue = new Packet(queue, ID_DEVICE_B, KIND_DEVICE);
            queue = new Packet(queue, ID_DEVICE_B, KIND_DEVICE);
            scheduler.addHandlerTask(ID_HANDLER_B, 3000, queue);

            scheduler.addDeviceTask(ID_DEVICE_A, 4000, null);
            scheduler.addDeviceTask(ID_DEVICE_B, 5000, null);
            scheduler.schedule();

            return ((scheduler.queueCount == EXPECTED_QUEUE_COUNT)
                && (scheduler.holdCount == EXPECTED_HOLD_COUNT));
        }

        public const int COUNT = 1000;

        /**
         * These two constants specify how many times a packet is queued and
         * how many times a task is put on hold in a correct run of richards.
         * They don't have any meaning a such but are characteristic of a
         * correct run so if the actual queue or hold count is different from
         * the expected there must be a bug in the implementation.
         **/
        public const int EXPECTED_QUEUE_COUNT = 2322;
        public const int EXPECTED_HOLD_COUNT = 928;

        public const int ID_IDLE = 0;
        public const int ID_WORKER = 1;
        public const int ID_HANDLER_A = 2;
        public const int ID_HANDLER_B = 3;
        public const int ID_DEVICE_A = 4;
        public const int ID_DEVICE_B = 5;
        public const int NUMBER_OF_IDS = 6;

        public const int KIND_DEVICE = 0;
        public const int KIND_WORK = 1;

        /**
         * The task is running and is currently scheduled.
         */
        public const int STATE_RUNNING = 0;

        /**
         * The task has packets left to process.
         */
        public const int STATE_RUNNABLE = 1;

        /**
         * The task is not currently running.  The task is not blocked as such and may
         * be started by the scheduler.
         */
        public const int STATE_SUSPENDED = 2;

        /**
         * The task is blocked and cannot be run until it is explicitly released.
         */
        public const int STATE_HELD = 4;

        public const int STATE_SUSPENDED_RUNNABLE = STATE_SUSPENDED | STATE_RUNNABLE;
        public const int STATE_NOT_HELD = ~STATE_HELD;

        /* --- *
         * P a c k e t
         * --- */

        public const int DATA_SIZE = 4;

        [Fact]
        public static int TestEntryPoint()
        {
            return Test(null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Test(int? arg)
        {
            int n = arg ?? 1;
            bool result = Measure(n);
            return (result ? 100 : -1);
        }

        public static bool Measure(int n)
        {
            DateTime start = DateTime.Now;
            bool result = true;
            for (int i = 0; i < n; i++)
            {
                result &= runRichards();
            }
            DateTime end = DateTime.Now;
            TimeSpan dur = end - start;
            Console.WriteLine("Doing {0} iters of Richards takes {1} ms; {2} us/iter.",
                              n, dur.TotalMilliseconds, (1000.0 * dur.TotalMilliseconds) / n);
            return result;
        }
    }

    internal class Scheduler
    {
        public int queueCount;
        public int holdCount;
        public TaskControlBlock[] blocks;
        public TaskControlBlock list;
        public TaskControlBlock currentTcb;
        public int currentId;

        public Scheduler()
        {
            this.queueCount = 0;
            this.holdCount = 0;
            this.blocks = new TaskControlBlock[Support.NUMBER_OF_IDS];
            this.list = null;
            this.currentTcb = null;
            this.currentId = 0;
        }

        /**
         * Add an idle task to this scheduler.
         * @param {int} id the identity of the task
         * @param {int} priority the task's priority
         * @param {Packet} queue the queue of work to be processed by the task
         * @param {int} count the number of times to schedule the task
         */
        public void addIdleTask(int id, int priority, Packet queue, int count)
        {
            this.addRunningTask(id, priority, queue,
                                new IdleTask(this, 1, count));
        }

        /**
         * Add a work task to this scheduler.
         * @param {int} id the identity of the task
         * @param {int} priority the task's priority
         * @param {Packet} queue the queue of work to be processed by the task
         */
        public void addWorkerTask(int id, int priority, Packet queue)
        {
            this.addTask(id, priority, queue,
                         new WorkerTask(this, Support.ID_HANDLER_A, 0));
        }

        /**
         * Add a handler task to this scheduler.
         * @param {int} id the identity of the task
         * @param {int} priority the task's priority
         * @param {Packet} queue the queue of work to be processed by the task
         */
        public void addHandlerTask(int id, int priority, Packet queue)
        {
            this.addTask(id, priority, queue, new HandlerTask(this));
        }

        /**
         * Add a handler task to this scheduler.
         * @param {int} id the identity of the task
         * @param {int} priority the task's priority
         * @param {Packet} queue the queue of work to be processed by the task
         */
        public void addDeviceTask(int id, int priority, Packet queue)
        {
            this.addTask(id, priority, queue, new DeviceTask(this));
        }

        /**
         * Add the specified task and mark it as running.
         * @param {int} id the identity of the task
         * @param {int} priority the task's priority
         * @param {Packet} queue the queue of work to be processed by the task
         * @param {Task} task the task to add
         */
        public void addRunningTask(int id, int priority, Packet queue, Task task)
        {
            this.addTask(id, priority, queue, task);
            this.currentTcb.setRunning();
        }

        /**
         * Add the specified task to this scheduler.
         * @param {int} id the identity of the task
         * @param {int} priority the task's priority
         * @param {Packet} queue the queue of work to be processed by the task
         * @param {Task} task the task to add
         */
        public void addTask(int id, int priority, Packet queue, Task task)
        {
            this.currentTcb = new TaskControlBlock(this.list, id, priority, queue, task);
            this.list = this.currentTcb;
            this.blocks[id] = this.currentTcb;
        }

        /**
         * Execute the tasks managed by this scheduler.
         */
        public void schedule()
        {
            this.currentTcb = this.list;
#if TRACEIT
            int kkk = 0;
#endif
            while (this.currentTcb != null)
            {
#if TRACEIT
                Console.WriteLine("kkk = {0}", kkk); kkk++;
#endif
                if (this.currentTcb.isHeldOrSuspended())
                {
#if TRACEIT
                    Console.WriteLine("held");
#endif
                    this.currentTcb = this.currentTcb.link;
                }
                else
                {
                    this.currentId = this.currentTcb.id;
#if TRACEIT
                    Console.WriteLine("currentId is now...{0}", this.currentId.ToString());
#endif
                    this.currentTcb = this.currentTcb.run();
                }
            }
        }

        /**
         * Release a task that is currently blocked and return the next block to run.
         * @param {int} id the id of the task to suspend
         */
        public TaskControlBlock release(int id)
        {
            TaskControlBlock tcb = this.blocks[id];
            if (tcb == null) return tcb;
            tcb.markAsNotHeld();
            if (tcb.priority >= this.currentTcb.priority)
            {
                return tcb;
            }
            else
            {
                return this.currentTcb;
            }
        }

        /**
         * Block the currently executing task and return the next task control block
         * to run.  The blocked task will not be made runnable until it is explicitly
         * released, even if new work is added to it.
         */
        public TaskControlBlock holdCurrent()
        {
            this.holdCount++;
            this.currentTcb.markAsHeld();
            return this.currentTcb.link;
        }

        /**
         * Suspend the currently executing task and return the next task control block
         * to run.  If new work is added to the suspended task it will be made runnable.
         */
        public TaskControlBlock suspendCurrent()
        {
            this.currentTcb.markAsSuspended();
            return this.currentTcb;
        }

        /**
         * Add the specified packet to the end of the worklist used by the task
         * associated with the packet and make the task runnable if it is currently
         * suspended.
         * @param {Packet} packet the packet to add
         */
        public TaskControlBlock queue(Packet packet)
        {
            TaskControlBlock t = this.blocks[packet.id];
            if (t == null) return t;
            this.queueCount++;
            packet.link = null;
            packet.id = this.currentId;
            return t.checkPriorityAdd(this.currentTcb, packet);
        }
    }

    /**
     * A task control block manages a task and the queue of work packages associated
     * with it.
     * @param {TaskControlBlock} link the preceding block in the linked block list
     * @param {int} id the id of this block
     * @param {int} priority the priority of this block
     * @param {Packet} queue the queue of packages to be processed by the task
     * @param {Task} task the task
     * @constructor
     */
    public class TaskControlBlock
    {
        public TaskControlBlock link;
        public int id;
        public int priority;
        public Packet queue;
        public Task task;
        public int state;

        public TaskControlBlock(TaskControlBlock link, int id, int priority,
                                Packet queue, Task task)
        {
            this.link = link;
            this.id = id;
            this.priority = priority;
            this.queue = queue;
            this.task = task;
            if (queue == null)
            {
                this.state = Support.STATE_SUSPENDED;
            }
            else
            {
                this.state = Support.STATE_SUSPENDED_RUNNABLE;
            }
        }

        public void setRunning()
        {
            this.state = Support.STATE_RUNNING;
        }

        public void markAsNotHeld()
        {
            this.state = this.state & Support.STATE_NOT_HELD;
        }

        public void markAsHeld()
        {
            this.state = this.state | Support.STATE_HELD;
        }

        public bool isHeldOrSuspended()
        {
            return ((this.state & Support.STATE_HELD) != 0) || (this.state == Support.STATE_SUSPENDED);
        }

        public void markAsSuspended()
        {
            this.state = this.state | Support.STATE_SUSPENDED;
        }

        public void markAsRunnable()
        {
            this.state = this.state | Support.STATE_RUNNABLE;
        }

        /**
         * Runs this task, if it is ready to be run, and returns the next task to run.
         */
        public TaskControlBlock run()
        {
            Packet packet;
#if TRACEIT
             Console.WriteLine("  TCB::run, state = {0}", this.state);
#endif
            if (this.state == Support.STATE_SUSPENDED_RUNNABLE)
            {
                packet = this.queue;
                this.queue = packet.link;
                if (this.queue == null)
                {
                    this.state = Support.STATE_RUNNING;
                }
                else
                {
                    this.state = Support.STATE_RUNNABLE;
                }
#if TRACEIT
                 Console.WriteLine("   State is now {0}", this.state);
#endif
            }
            else
            {
#if TRACEIT
                 Console.WriteLine("  TCB::run, setting packet = Null.");
#endif
                packet = null;
            }
            return this.task.run(packet);
        }

        /**
         * Adds a packet to the worklist of this block's task, marks this as runnable if
         * necessary, and returns the next runnable object to run (the one
         * with the highest priority).
         */
        public TaskControlBlock checkPriorityAdd(TaskControlBlock task, Packet packet)
        {
            if (this.queue == null)
            {
                this.queue = packet;
                this.markAsRunnable();
                if (this.priority >= task.priority) return this;
            }
            else
            {
                this.queue = packet.addTo(this.queue);
            }
            return task;
        }

        public String toString()
        {
            return "tcb { " + this.task.toString() + "@" + this.state.ToString() + " }";
        }
    }

#if INTF_FOR_TASK
    // I deliberately ignore the "I" prefix convention here so that we can use Task as a type in both
    // cases...
    public interface Task
    {
        TaskControlBlock run(Packet packet);
        String toString();
    }
#else
     public abstract class Task
     {
         public abstract TaskControlBlock run(Packet packet);
         public abstract String toString();
     }
#endif

    /**
     * An idle task doesn't do any work itself but cycles control between the two
     * device tasks.
     * @param {Scheduler} scheduler the scheduler that manages this task
     * @param {int} v1 a seed value that controls how the device tasks are scheduled
     * @param {int} count the number of times this task should be scheduled
     * @constructor
     */
    internal class IdleTask : Task
    {
        public Scheduler scheduler;
        public int _v1;
        public int _count;

        public IdleTask(Scheduler scheduler, int v1, int count)
        {
            this.scheduler = scheduler;
            this._v1 = v1;
            this._count = count;
        }

        public
#if !INTF_FOR_TASK
            override
#endif
            TaskControlBlock run(Packet packet)
        {
            this._count--;
            if (this._count == 0) return this.scheduler.holdCurrent();
            if ((this._v1 & 1) == 0)
            {
                this._v1 = this._v1 >> 1;
                return this.scheduler.release(Support.ID_DEVICE_A);
            }
            else
            {
                this._v1 = (this._v1 >> 1) ^ 0xD008;
                return this.scheduler.release(Support.ID_DEVICE_B);
            }
        }

        public
#if !INTF_FOR_TASK
            override
#endif
            String toString()
        {
            return "IdleTask";
        }
    }

    /**
     * A task that suspends itself after each time it has been run to simulate
     * waiting for data from an external device.
     * @param {Scheduler} scheduler the scheduler that manages this task
     * @constructor
     */
    internal class DeviceTask : Task
    {
        public Scheduler scheduler;
        private Packet _v1;

        public DeviceTask(Scheduler scheduler)
        {
            this.scheduler = scheduler;
            _v1 = null;
        }

        public
#if !INTF_FOR_TASK
             override
#endif
             TaskControlBlock run(Packet packet)
        {
            if (packet == null)
            {
                if (_v1 == null) return this.scheduler.suspendCurrent();
                Packet v = _v1;
                _v1 = null;
                return this.scheduler.queue(v);
            }
            else
            {
                _v1 = packet;
                return this.scheduler.holdCurrent();
            }
        }

        public
#if !INTF_FOR_TASK
             override
#endif
             String toString()
        {
            return "DeviceTask";
        }
    }

    /**
     * A task that manipulates work packets.
     * @param {Scheduler} scheduler the scheduler that manages this task
     * @param {int} v1 a seed used to specify how work packets are manipulated
     * @param {int} v2 another seed used to specify how work packets are manipulated
     * @constructor
     */
    internal class WorkerTask : Task
    {
        public Scheduler scheduler;
        public int v1;
        public int _v2;

        public WorkerTask(Scheduler scheduler, int v1, int v2)
        {
            this.scheduler = scheduler;
            this.v1 = v1;
            this._v2 = v2;
        }

        public
#if !INTF_FOR_TASK
             override
#endif
             TaskControlBlock run(Packet packet)
        {
            if (packet == null)
            {
                return this.scheduler.suspendCurrent();
            }
            else
            {
                if (this.v1 == Support.ID_HANDLER_A)
                {
                    this.v1 = Support.ID_HANDLER_B;
                }
                else
                {
                    this.v1 = Support.ID_HANDLER_A;
                }
                packet.id = this.v1;
                packet.a1 = 0;
                for (int i = 0; i < Support.DATA_SIZE; i++)
                {
                    this._v2++;
                    if (this._v2 > 26) this._v2 = 1;
                    packet.a2[i] = this._v2;
                }
                return this.scheduler.queue(packet);
            }
        }

        public
#if !INTF_FOR_TASK
             override
#endif
             String toString()
        {
            return "WorkerTask";
        }
    }

    /**
     * A task that manipulates work packets and then suspends itself.
     * @param {Scheduler} scheduler the scheduler that manages this task
     * @constructor
     */
    internal class HandlerTask : Task
    {
        public Scheduler scheduler;
        public Packet v1;
        public Packet v2;

        public HandlerTask(Scheduler scheduler)
        {
            this.scheduler = scheduler;
            this.v1 = null;
            this.v2 = null;
        }

        public
#if !INTF_FOR_TASK
             override
#endif
             TaskControlBlock run(Packet packet)
        {
            if (packet != null)
            {
                if (packet.kind == Support.KIND_WORK)
                {
                    this.v1 = packet.addTo(this.v1);
                }
                else
                {
                    this.v2 = packet.addTo(this.v2);
                }
            }
            if (this.v1 != null)
            {
                int count = this.v1.a1;
                Packet v;
                if (count < Support.DATA_SIZE)
                {
                    if (this.v2 != null)
                    {
                        v = this.v2;
                        this.v2 = this.v2.link;
                        v.a1 = this.v1.a2[count];
                        this.v1.a1 = count + 1;
                        return this.scheduler.queue(v);
                    }
                }
                else
                {
                    v = this.v1;
                    this.v1 = this.v1.link;
                    return this.scheduler.queue(v);
                }
            }
            return this.scheduler.suspendCurrent();
        }

        public
#if !INTF_FOR_TASK
             override
#endif
             String toString()
        {
            return "HandlerTask";
        }
    }

    /**
     * A simple package of data that is manipulated by the tasks.  The exact layout
     * of the payload data carried by a packet is not importaint, and neither is the
     * nature of the work performed on packets by the tasks.
     *
     * Besides carrying data, packets form linked lists and are hence used both as
     * data and worklists.
     * @param {Packet} link the tail of the linked list of packets
     * @param {int} id an ID for this packet
     * @param {int} kind the type of this packet
     * @constructor
     */
    public class Packet
    {
        public Packet link;
        public int id;
        public int kind;
        public int a1;
        public int[] a2;

        public Packet(Packet link, int id, int kind)
        {
            this.link = link;
            this.id = id;
            this.kind = kind;
            this.a1 = 0;
            this.a2 = new int[Support.DATA_SIZE];
        }

        public Packet addTo(Packet queue)
        {
            this.link = null;
            if (queue == null) return this;
            Packet peek;
            Packet next = queue;
            while ((peek = next.link) != null)
                next = peek;
            next.link = this;
            return queue;
        }

        public String toString()
        {
            return "Packet";
        }
    }
}
