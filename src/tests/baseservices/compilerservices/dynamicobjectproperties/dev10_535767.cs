// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Basic test for dependent handles.
//
// Note that though this test uses ConditionalWeakTable it is not a test for that class. This is a stress 
// test that utilizes ConditionalWeakTable features, which would be used heavily if Dynamic Language Runtime 
// catches on.
//
// Basic test overview:
//  * Allocate an array of objects (we call these Nodes) with finalizers.
//  * Create a set of dependent handles that reference these objects as primary and secondary members (this is
//    where ConditionalWeakTable comes in, adding a key/value pair to such a table creates a dependent handle
//    with the primary set to the key and the secondary set to the value).
//  * Null out selected objects from the array in various patterns. This removes the only normal strong root
//    for such objects (leaving only the dependent handles to provide additional roots).
//  * Perform a full GC and wait for it and finalization to complete. Each object which is collected will use
//    its finalizer to inform the test that it's been disposed of.
//  * Run our own reachability analysis (a simple mark array approach) to build a picture of which objects in
//    the array should have been collected or not.
//  * Validate that the actual set of live objects matches our computed live set exactly.
//
// Test variations include the number of objects allocated, the relationship between the primary and secondary
// in each handle we allocate and the pattern with which we null out object references in the array.
//
// Additionally this test stresses substantially more complex code paths in the GC if server mode is enabled.
// This can be achieved by setting the environment variable DOTNET_BuildFlavor=svr prior to executing the
// test executable.
//
// Note that we don't go to any lengths to ensure that dependent handle ownership is spread over multiple cpus
// on a server GC/MP test run. For large node counts (e.g. 100000) this happens naturally since initialization
// takes a while with multiple thread/CPU switches involved. We could be more explicit here (allocate handles
// using CPU affinitized threads) but if we do that we'd probably better look into different patterns of node
// ownership to avoid unintentionally restricting our test coverage.
//
// Another area into which we could look deeper is trying to force mark stack overflows in the GC (presumably
// by allocating complex object graphs with lots of interconnections, though I don't the specifics of the best
// way to force this). Causing mark stack overflows should open up a class of bug the old dependent handle
// implementation was subject to without requiring server GC mode or multiple CPUs.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

// How we assign nodes to dependent handles.
enum TableStyle
{
    Unconnected,    // The primary and secondary handles are assigned completely disjoint objects
    ForwardLinked,  // The primary of each handle is the secondary of the previous handle
    BackwardLinked, // The primary of each handle is the secondary of the next handle
    Random          // The primaries are each object in sequence, the secondaries are selected randomly from
                    // the same set
}

// How we choose object references in the array to null out (and thus potentially become collected).
enum CollectStyle
{
    None,           // Don't null out any (nothing should be collected)
    All,            // Null them all out (any remaining live objects should be collected)
    Alternate,      // Null out every second reference
    Random          // Null out each entry with a 50% probability
}

// We report errors by throwing an exception. Define our own Exception subclass so we can identify these
// errors unambiguously.
class TestException : Exception
{
    // We just supply a simple message string on error.
    public TestException(string message) : base(message)
    {
    }
}

// Class encapsulating test runs over a set of objects/handles allocated with the specified TableStyle.
class TestSet
{
    // Create a new test with the given table style and object count.
    public TestSet(TableStyle ts, int count)
    {
        // Use one random number generator for the life of the test. Could support explicit seeds for
        // reproducible tests here.
        m_rng = new Random();

        // Remember our parameters.
        m_count = count;
        m_style = ts;

        // Various arrays.
        m_nodes = new Node[count];      // The array of objects
        m_collected = new bool[count];  // Records whether each object has been collected (entries are set by
                                        // the finalizer on Node)
        m_marks = new bool[count];      // Array used during individual test runs to calculate whether each
                                        // object should still be alive (allocated once here to avoid
                                        // injecting further garbage collections at run time)

        // Allocate each object (Node). Each knows its own unique ID (the index into the node array) and has a
        // back pointer to this test object (so it can phone home to report its own collection at finalization
        // time).
        for (int i = 0; i < count; i++)
            m_nodes[i] = new Node(this, i);

        // Determine how many handles we need to allocate given the number of nodes. This varies based on the
        // table style.
        switch (ts)
        {
        case TableStyle.Unconnected:
            // Primaries and secondaries are completely different objects so we split our nodes in half and
            // allocate that many handles.
            m_handleCount = count / 2;
            break;

        case TableStyle.ForwardLinked:
            // Nodes are primaries in one handle and secondary in another except one that falls off the end.
            // So we have as many handles as nodes - 1.
            m_handleCount = count - 1;
            break;

        case TableStyle.BackwardLinked:
            // Nodes are primaries in one handle and secondary in another except one that falls off the end.
            // So we have as many handles as nodes - 1.
            m_handleCount = count - 1;
            break;

        case TableStyle.Random:
            // Each node is a primary in some handle (secondaries are selected from amongst all the same nodes
            // randomly). So we have as many nodes as handles.
            m_handleCount = count;
            break;
        }

        // Allocate an array of HandleSpecs. These aren't the real handles, just structures that allow us
        // remember what's in each handle (in terms of the node index number for the primary and secondary).
        // We need to track this information separately because we can't access the real handles directly
        // (ConditionalWeakTable hides them) and we need to recall exactly what the primary and secondary of
        // each handle is so we can compute our own notion of object liveness later.
        m_handles = new HandleSpec[m_handleCount];

        // Initialize the handle specs to assign objects to handles based on the table style.
        for (int i = 0; i < m_handleCount; i++)
        {
            int primary = -1, secondary = -1;

            switch (ts)
            {
            case TableStyle.Unconnected:
                // Assign adjacent nodes to the primary and secondary of each handle.
                primary = i * 2;
                secondary = (i * 2) + 1;
                break;

            case TableStyle.ForwardLinked:
                // Primary of each handle is the secondary of the last handle.
                primary = i;
                secondary = i + 1;
                break;

            case TableStyle.BackwardLinked:
                // Primary of each handle is the secondary of the next handle.
                primary = i + 1;
                secondary = i;
                break;

            case TableStyle.Random:
                // Primary is each node in sequence, secondary is any of the nodes randomly.
                primary = i;
                secondary = m_rng.Next(m_handleCount);
                break;
            }

            m_handles[i].Set(primary, secondary);
        }

        // Allocate a ConditionalWeakTable mapping Node keys to Node values.
        m_table = new ConditionalWeakTable<Node, Node>();

        // Using our handle specs computed above add each primary/secondary node pair to the
        // ConditionalWeakTable in turn. This causes the ConditionalWeakTable to allocate a dependent handle
        // for each entry with the primary and secondary objects we specified as keys and values (note that
        // this scheme prevents us from creating multiple handles with the same primary though if this is
        // desired we could achieve it by allocating multiple ConditionalWeakTables).
        for (int i = 0; i < m_handleCount; i++)
            m_table.Add(m_nodes[m_handles[i].m_primary], m_nodes[m_handles[i].m_secondary]);
    }

    // Call this method to indicate a test error with a given message. This will terminate the test
    // immediately.
    void Error(string message)
    {
        throw new TestException(message);
    }

    // Run a single test pass on the node set. Null out node references according to the given CollectStyle,
    // run a garbage collection and then verify that each node is either live or dead as we predict. Take care
    // of the order in which test runs are made against a single TestSet: e.g. running a CollectStyle.All will
    // collect all nodes, rendering further runs relatively uninteresting.
    public void Run(CollectStyle cs)
    {
        Console.WriteLine("Running test TS:{0} CS:{1} {2} entries...",
                          Enum.GetName(typeof(TableStyle), m_style),
                          Enum.GetName(typeof(CollectStyle), cs),
                          m_count);

        // Iterate over the array of nodes deciding for each whether to sever the reference (null out the
        // entry).
        for (int i = 0; i < m_count; i++)
        {
            bool sever = false;
            switch (cs)
            {
            case CollectStyle.All:
                // Sever all references.
                sever = true;
                break;

            case CollectStyle.None:
                // Don't sever any references.
                break;

            case CollectStyle.Alternate:
                // Sever every second reference (starting with the first).
                if ((i % 2) == 0)
                    sever = true;
                break;

            case CollectStyle.Random:
                // Sever any reference with a 50% probability.
                if (m_rng.Next(100) > 50)
                    sever = true;
                break;
            }

            if (sever)
                m_nodes[i] = null;
        }

        // Initialize a full GC and wait for all finalizers to complete (so we get an accurate picture of
        // which nodes were collected).
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.WaitForPendingFinalizers(); // the above call may correspond to a GC prior to the Collect above, call it again

        // Calculate our own view of which nodes should be alive or dead. Use a simple mark array for this.
        // Once the algorithm is complete a true value at a given index in the array indicates a node that
        // should still be alive, otherwise the node should have been collected.

        // Initialize the mark array. Set true for nodes we still have a strong reference to from the array
        // (these should definitely not have been collected yet). Set false for the other nodes (we assume
        // they must have been collected until we prove otherwise).
        for (int i = 0; i < m_count; i++)
            m_marks[i] = m_nodes[i] != null;

        // Perform multiple passes over the handles we allocated (or our recorded version of the handles at
        // least). If we find a handle with a marked (live) primary where the secondary is not yet marked then
        // go ahead and mark that secondary (dependent handles are defined to do this: primaries act as if
        // they have a strong reference to the secondary up until the point they are collected). Repeat this
        // until we manage a scan over the entire table without marking any additional nodes as live. At this
        // point the marks array should reflect which objects are still live.
        while (true)
        {
            // Assume we're not going any further nodes to mark as live.
            bool marked = false;

            // Look at each handle in turn.
            for (int i = 0; i < m_handleCount; i++)

                if (m_marks[m_handles[i].m_primary])
                {
                    // Primary is live.
                    if (!m_marks[m_handles[i].m_secondary])
                    {
                        // Secondary wasn't marked as live yet. Do so and remember that we marked at least
                        // node as live this pass (so we need to loop again since this secondary could be the
                        // same as a primary earlier in the table).
                        m_marks[m_handles[i].m_secondary] = true;
                        marked = true;
                    }
                }

            // Terminate the loop if we scanned the entire table without marking any additional nodes as live
            // (since additional scans can't make any difference).
            if (!marked)
                break;
        }

        // Validate our view of node liveness (m_marks) correspond to reality (m_nodes and m_collected).
        for (int i = 0; i < m_count; i++)
        {
            // Catch nodes which still have strong references but have collected anyway. This is stricly a
            // subset of the next test but it would be a very interesting bug to call out.
            if (m_nodes[i] != null && m_collected[i])
                Error(String.Format("Node {0} was collected while it still had a strong root", i));

            // Catch nodes which we compute as alive but have been collected.
            if (m_marks[i] && m_collected[i])
                Error(String.Format("Node {0} was collected while it was still reachable", i));

            // Catch nodes which we compute as dead but haven't been collected.
            if (!m_marks[i] && !m_collected[i])
                Error(String.Format("Node {0} wasn't collected even though it was unreachable", i));
        }
    }

    // Method called by nodes when they're finalized (i.e. the node has been collected).
    public void Collected(int id)
    {
        // Catch nodes which are collected twice.
        if (m_collected[id])
            Error(String.Format("Node {0} collected twice", id));

        m_collected[id] = true;
    }

    // Structure used to record the primary and secondary nodes in every dependent handle we allocated. Nodes
    // are identified by ID (their index into the node array).
    struct HandleSpec
    {
        public int                      m_primary;
        public int                      m_secondary;

        public void Set(int primary, int secondary)
        {
            m_primary = primary;
            m_secondary = secondary;
        }
    }

    int                                 m_count;        // Count of nodes in array
    TableStyle                          m_style;        // Style of handle creation
    Node[]                              m_nodes;        // Array of nodes
    bool[]                              m_collected;    // Array indicating which nodes have been collected
    bool[]                              m_marks;        // Array indicating which nodes should be live 
    ConditionalWeakTable<Node, Node>    m_table;        // Table that creates and holds our dependent handles
    int                                 m_handleCount;  // Number of handles we create
    HandleSpec[]                        m_handles;      // Array of descriptions of each handle
    Random                              m_rng;          // Random number generator
}

// The type of object we reference from our dependent handles. Doesn't do much except report its own garbage
// collection to the owning TestSet.
class Node
{
    // Allocate a node and remember our owner (TestSet) and ID (index into node array).
    public Node(TestSet owner, int id)
    {
        m_owner = owner;
        m_id = id;
    }

    // On finalization report our collection to the owner TestSet.
    ~Node()
    {
        m_owner.Collected(m_id);
    }

    TestSet                             m_owner;        // TestSet which created us
    int                                 m_id;           // Our index into above TestSet's node array
}

// The test class itself.
public class DhTest1
{
    // Entry point.
    [Fact]
    public static int TestEntryPoint()
    {
        // The actual test runs are controlled from RunTest. True is returned if all succeeded, false
        // otherwise.
        if (new DhTest1().RunTest())
        {
            Console.WriteLine("Test PASS");
            return 100;
        }
        else
        {
            Console.WriteLine("Test FAIL");
            return 999;
        }
    }

    // Run a series of tests with different table and collection styles.
    bool RunTest()
    {
        // Number of nodes we'll allocate in each run (we could take this as an argument instead).
        int numNodes = 10000;

        // Run everything under an exception handler since test errors are reported as exceptions.
        try
        {
            // Run a pass with each table style. For each style run through the collection styles in the order
            // None, Alternate, Random and All. This sequence is carefully selected to remove progressively
            // more nodes from the array (since, within a given TestSet instance, once a node has actually
            // been collected it won't be resurrected for future runs).

            TestSet ts1 = new TestSet(TableStyle.Unconnected, numNodes);
            ts1.Run(CollectStyle.None);
            ts1.Run(CollectStyle.Alternate);
            ts1.Run(CollectStyle.Random);
            ts1.Run(CollectStyle.All);

            TestSet ts2 = new TestSet(TableStyle.ForwardLinked, numNodes);
            ts2.Run(CollectStyle.None);
            ts2.Run(CollectStyle.Alternate);
            ts2.Run(CollectStyle.Random);
            ts2.Run(CollectStyle.All);

            TestSet ts3 = new TestSet(TableStyle.BackwardLinked, numNodes);
            ts3.Run(CollectStyle.None);
            ts3.Run(CollectStyle.Alternate);
            ts3.Run(CollectStyle.Random);
            ts3.Run(CollectStyle.All);

            TestSet ts4 = new TestSet(TableStyle.Random, numNodes);
            ts4.Run(CollectStyle.None);
            ts4.Run(CollectStyle.Alternate);
            ts4.Run(CollectStyle.Random);
            ts4.Run(CollectStyle.All);
        }
        catch (TestException te)
        {
            // "Expected" errors.
            Console.WriteLine("TestError: {0}", te.Message);
            return false;
        }
        catch (Exception e)
        {
            // Totally unexpected errors (probably shouldn't see these unless there's a test bug).
            Console.WriteLine("Unexpected exception: {0}", e.GetType().Name);
            return false;
        }

        // If we get as far as here the test succeeded.
        return true;
    }
}

