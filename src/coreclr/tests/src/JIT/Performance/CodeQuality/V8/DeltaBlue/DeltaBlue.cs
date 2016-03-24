// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*
  This is a Java implemention of the DeltaBlue algorithm described in:
    "The DeltaBlue Algorithm: An Incremental Constraint Hierarchy Solver"
    by Bjorn N. Freeman-Benson and John Maloney
    January 1990 Communications of the ACM,
    also available as University of Washington TR 89-08-06.

  This implementation by Mario Wolczko, Sun Microsystems, Sep 1996,
  based on the Smalltalk implementation by John Maloney.

*/

// The code has been adapted for use as a C# benchmark by Microsoft.

#define USE_STACK

using Microsoft.Xunit.Performance;
using System;
using System.Collections;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

/* 
Strengths are used to measure the relative importance of constraints.
New strengths may be inserted in the strength hierarchy without
disrupting current constraints.  Strengths cannot be created outside
this class, so pointer comparison can be used for value comparison.
*/

internal class Strength
{
    private int _strengthValue;
    private String _name;

    private Strength(int strengthValue, String name)
    {
        _strengthValue = strengthValue;
        _name = name;
    }

    public static Boolean stronger(Strength s1, Strength s2)
    {
        return s1._strengthValue < s2._strengthValue;
    }

    public static Boolean weaker(Strength s1, Strength s2)
    {
        return s1._strengthValue > s2._strengthValue;
    }

    public static Strength weakestOf(Strength s1, Strength s2)
    {
        return weaker(s1, s2) ? s1 : s2;
    }

    public static Strength strongest(Strength s1, Strength s2)
    {
        return stronger(s1, s2) ? s1 : s2;
    }

    // for iteration
    public Strength nextWeaker()
    {
        switch (_strengthValue)
        {
            case 0: return weakest;
            case 1: return weakDefault;
            case 2: return normal;
            case 3: return strongDefault;
            case 4: return preferred;
            case 5: return strongPreferred;

            case 6:
            default:
                Console.Error.WriteLine("Invalid call to nextStrength()!");
                //System.exit(1);
                return null;
        }
    }

    // Strength constants
    public static Strength required = new Strength(0, "required");
    public static Strength strongPreferred = new Strength(1, "strongPreferred");
    public static Strength preferred = new Strength(2, "preferred");
    public static Strength strongDefault = new Strength(3, "strongDefault");
    public static Strength normal = new Strength(4, "normal");
    public static Strength weakDefault = new Strength(5, "weakDefault");
    public static Strength weakest = new Strength(6, "weakest");

    public void print()
    {
        Console.Write("strength[" + _strengthValue + "]");
    }
}




//------------------------------ variables ------------------------------

// I represent a constrained variable. In addition to my value, I
// maintain the structure of the constraint graph, the current
// dataflow graph, and various parameters of interest to the DeltaBlue
// incremental constraint solver.

internal class Variable
{
    public int value;               // my value; changed by constraints
    public ArrayList constraints;   // normal constraints that reference me
    public Constraint determinedBy; // the constraint that currently determines
                                    // my value (or null if there isn't one)
    public int mark;                // used by the planner to mark constraints
    public Strength walkStrength;   // my walkabout strength
    public Boolean stay;            // true if I am a planning-time constant
    public String name;             // a symbolic name for reporting purposes


    private Variable(String name, int initialValue, Strength walkStrength,
             int nconstraints)
    {
        value = initialValue;
        constraints = new ArrayList(nconstraints);
        determinedBy = null;
        mark = 0;
        this.walkStrength = walkStrength;
        stay = true;
        this.name = name;
    }

    public Variable(String name, int value) : this(name, value, Strength.weakest, 2)
    {
    }

    public Variable(String name) : this(name, 0, Strength.weakest, 2)
    {
    }

    public void print()
    {
        Console.Write(name + "(");
        walkStrength.print();
        Console.Write("," + value + ")");
    }

    // Add the given constraint to the set of all constraints that refer to me.
    public void addConstraint(Constraint c)
    {
        constraints.Add(c);
    }

    // Remove all traces of c from this variable.
    public void removeConstraint(Constraint c)
    {
        constraints.Remove(c);
        if (determinedBy == c) determinedBy = null;
    }

    // Attempt to assign the given value to me using the given strength.
    public void setValue(int value, Strength strength)
    {
        EditConstraint e = new EditConstraint(this, strength);
        if (e.isSatisfied())
        {
            this.value = value;
            deltablue.planner.propagateFrom(this);
        }
        e.destroyConstraint();
    }
}




//------------------------ constraints ------------------------------------

// I am an abstract class representing a system-maintainable
// relationship (or "constraint") between a set of variables. I supply
// a strength instance variable; concrete subclasses provide a means
// of storing the constrained variables and other information required
// to represent a constraint.

internal abstract class Constraint
{
    public Strength strength; // the strength of this constraint

    public Constraint() { } // this has to be here because of
                            // Java's constructor idiocy.

    public Constraint(Strength strength)
    {
        this.strength = strength;
    }

    // Answer true if this constraint is satisfied in the current solution.
    public abstract Boolean isSatisfied();

    // Record the fact that I am unsatisfied.
    public abstract void markUnsatisfied();

    // Normal constraints are not input constraints. An input constraint
    // is one that depends on external state, such as the mouse, the
    // keyboard, a clock, or some arbitrary piece of imperative code.
    public virtual Boolean isInput() { return false; }

    // Activate this constraint and attempt to satisfy it.
    public void addConstraint()
    {
        addToGraph();
        deltablue.planner.incrementalAdd(this);
    }

    // Deactivate this constraint, remove it from the constraint graph,
    // possibly causing other constraints to be satisfied, and destroy
    // it.
    public void destroyConstraint()
    {
        if (isSatisfied()) deltablue.planner.incrementalRemove(this);
        else
            removeFromGraph();
    }

    // Add myself to the constraint graph.
    public abstract void addToGraph();

    // Remove myself from the constraint graph.
    public abstract void removeFromGraph();

    // Decide if I can be satisfied and record that decision. The output
    // of the choosen method must not have the given mark and must have
    // a walkabout strength less than that of this constraint.
    public abstract void chooseMethod(int mark);

    // Set the mark of all input from the given mark.
    public abstract void markInputs(int mark);

    // Assume that I am satisfied. Answer true if all my current inputs
    // are known. A variable is known if either a) it is 'stay' (i.e. it
    // is a constant at plan execution time), b) it has the given mark
    // (indicating that it has been computed by a constraint appearing
    // earlier in the plan), or c) it is not determined by any
    // constraint.
    public abstract Boolean inputsKnown(int mark);

    // Answer my current output variable. Raise an error if I am not
    // currently satisfied.
    public abstract Variable output();

    // Attempt to find a way to enforce this constraint. If successful,
    // record the solution, perhaps modifying the current dataflow
    // graph. Answer the constraint that this constraint overrides, if
    // there is one, or nil, if there isn't.
    // Assume: I am not already satisfied.
    //
    public Constraint satisfy(int mark)
    {
        chooseMethod(mark);
        if (!isSatisfied())
        {
            if (strength == Strength.required)
            {
                deltablue.error("Could not satisfy a required constraint");
            }
            return null;
        }
        // constraint can be satisfied
        // mark inputs to allow cycle detection in addPropagate
        markInputs(mark);
        Variable outvar = output();
        Constraint overridden = outvar.determinedBy;
        if (overridden != null) overridden.markUnsatisfied();
        outvar.determinedBy = this;
        if (!deltablue.planner.addPropagate(this, mark))
        {
            Console.WriteLine("Cycle encountered");
            return null;
        }
        outvar.mark = mark;
        return overridden;
    }

    // Enforce this constraint. Assume that it is satisfied.
    public abstract void execute();

    // Calculate the walkabout strength, the stay flag, and, if it is
    // 'stay', the value for the current output of this
    // constraint. Assume this constraint is satisfied.
    public abstract void recalculate();

    public abstract void printInputs();

    public void printOutput() { output().print(); }

    public void print()
    {
        if (!isSatisfied())
        {
            Console.Write("Unsatisfied");
        }
        else
        {
            Console.Write("Satisfied(");
            printInputs();
            Console.Write(" -> ");
            printOutput();
            Console.Write(")");
        }
        Console.Write("\n");
    }
}



//-------------unary constraints-------------------------------------------

// I am an abstract superclass for constraints having a single
// possible output variable.
//
internal abstract class UnaryConstraint : Constraint
{
    public Variable myOutput; // possible output variable
    public Boolean satisfied; // true if I am currently satisfied

    public UnaryConstraint(Variable v, Strength strength) : base(strength)

    {
        myOutput = v;
        satisfied = false;
        addConstraint();
    }

    // Answer true if this constraint is satisfied in the current solution.
    public override Boolean isSatisfied() { return satisfied; }

    // Record the fact that I am unsatisfied.
    public override void markUnsatisfied() { satisfied = false; }

    // Answer my current output variable.
    public override Variable output() { return myOutput; }

    // Add myself to the constraint graph.
    public override void addToGraph()
    {
        myOutput.addConstraint(this);
        satisfied = false;
    }

    // Remove myself from the constraint graph.
    public override void removeFromGraph()
    {
        if (myOutput != null) myOutput.removeConstraint(this);
        satisfied = false;
    }

    // Decide if I can be satisfied and record that decision.
    public override void chooseMethod(int mark)
    {
        satisfied = myOutput.mark != mark
                   && Strength.stronger(strength, myOutput.walkStrength);
    }

    public override void markInputs(int mark) { }   // I have no inputs

    public override Boolean inputsKnown(int mark) { return true; }

    // Calculate the walkabout strength, the stay flag, and, if it is
    // 'stay', the value for the current output of this
    // constraint. Assume this constraint is satisfied."
    public override void recalculate()
    {
        myOutput.walkStrength = strength;
        myOutput.stay = !isInput();
        if (myOutput.stay) execute(); // stay optimization
    }

    public override void printInputs() { } // I have no inputs
}


// I am a unary input constraint used to mark a variable that the
// client wishes to change.
//
internal class EditConstraint : UnaryConstraint
{
    public EditConstraint(Variable v, Strength str) : base(v, str) { }

    // I indicate that a variable is to be changed by imperative code.
    public override Boolean isInput() { return true; }

    public override void execute() { } // Edit constraints do nothing.
}

// I mark variables that should, with some level of preference, stay
// the same. I have one method with zero inputs and one output, which
// does nothing. Planners may exploit the fact that, if I am
// satisfied, my output will not change during plan execution. This is
// called "stay optimization".
//
internal class StayConstraint : UnaryConstraint
{
    // Install a stay constraint with the given strength on the given variable.
    public StayConstraint(Variable v, Strength str) : base(v, str) { }

    public override void execute() { } // Stay constraints do nothing.
}




//-------------binary constraints-------------------------------------------


// I am an abstract superclass for constraints having two possible
// output variables.
//
internal abstract class BinaryConstraint : Constraint
{
    public Variable v1, v2; // possible output variables
    public sbyte direction; // one of the following...
    public static sbyte backward = -1;    // v1 is output
    public static sbyte nodirection = 0;  // not satisfied
    public static sbyte forward = 1;      // v2 is output

    public BinaryConstraint() { } // this has to be here because of
                                  // Java's constructor idiocy.

    public BinaryConstraint(Variable var1, Variable var2, Strength strength)
      : base(strength)
    {
        v1 = var1;
        v2 = var2;
        direction = nodirection;
        addConstraint();
    }

    // Answer true if this constraint is satisfied in the current solution.
    public override Boolean isSatisfied() { return direction != nodirection; }

    // Add myself to the constraint graph.
    public override void addToGraph()
    {
        v1.addConstraint(this);
        v2.addConstraint(this);
        direction = nodirection;
    }

    // Remove myself from the constraint graph.
    public override void removeFromGraph()
    {
        if (v1 != null) v1.removeConstraint(this);
        if (v2 != null) v2.removeConstraint(this);
        direction = nodirection;
    }

    // Decide if I can be satisfied and which way I should flow based on
    // the relative strength of the variables I relate, and record that
    // decision.
    //
    public override void chooseMethod(int mark)
    {
        if (v1.mark == mark)
            direction =
          v2.mark != mark && Strength.stronger(strength, v2.walkStrength)
            ? forward : nodirection;

        if (v2.mark == mark)
            direction =
          v1.mark != mark && Strength.stronger(strength, v1.walkStrength)
            ? backward : nodirection;

        // If we get here, neither variable is marked, so we have a choice.
        if (Strength.weaker(v1.walkStrength, v2.walkStrength))
            direction =
          Strength.stronger(strength, v1.walkStrength) ? backward : nodirection;
        else
            direction =
          Strength.stronger(strength, v2.walkStrength) ? forward : nodirection;
    }

    // Record the fact that I am unsatisfied.
    public override void markUnsatisfied() { direction = nodirection; }

    // Mark the input variable with the given mark.
    public override void markInputs(int mark)
    {
        input().mark = mark;
    }

    public override Boolean inputsKnown(int mark)
    {
        Variable i = input();
        return i.mark == mark || i.stay || i.determinedBy == null;
    }

    // Answer my current output variable.
    public override Variable output() { return direction == forward ? v2 : v1; }

    // Answer my current input variable
    public Variable input() { return direction == forward ? v1 : v2; }

    // Calculate the walkabout strength, the stay flag, and, if it is
    // 'stay', the value for the current output of this
    // constraint. Assume this constraint is satisfied.
    //
    public override void recalculate()
    {
        Variable invar = input(), outvar = output();
        outvar.walkStrength = Strength.weakestOf(strength, invar.walkStrength);
        outvar.stay = invar.stay;
        if (outvar.stay) execute();
    }

    public override void printInputs()
    {
        input().print();
    }
}


// I constrain two variables to have the same value: "v1 = v2".
//
internal class EqualityConstraint : BinaryConstraint
{
    // Install a constraint with the given strength equating the given variables.
    public EqualityConstraint(Variable var1, Variable var2, Strength strength)
        : base(var1, var2, strength)

    {
    }

    // Enforce this constraint. Assume that it is satisfied.
    public override void execute()
    {
        output().value = input().value;
    }
}


// I relate two variables by the linear scaling relationship: "v2 =
// (v1 * scale) + offset". Either v1 or v2 may be changed to maintain
// this relationship but the scale factor and offset are considered
// read-only.
//
internal class ScaleConstraint : BinaryConstraint
{
    public Variable scale; // scale factor input variable
    public Variable offset; // offset input variable

    // Install a scale constraint with the given strength on the given variables.
    public ScaleConstraint(Variable src, Variable scale, Variable offset,
                   Variable dest, Strength strength)
    {
        // Curse this wretched language for insisting that constructor invocation
        // must be the first thing in a method...
        // ..because of that, we must copy the code from the inherited
        // constructors.
        this.strength = strength;
        v1 = src;
        v2 = dest;
        direction = nodirection;
        this.scale = scale;
        this.offset = offset;
        addConstraint();
    }

    // Add myself to the constraint graph.
    public override void addToGraph()
    {
        base.addToGraph();
        scale.addConstraint(this);
        offset.addConstraint(this);
    }

    // Remove myself from the constraint graph.
    public override void removeFromGraph()
    {
        base.removeFromGraph();
        if (scale != null) scale.removeConstraint(this);
        if (offset != null) offset.removeConstraint(this);
    }

    // Mark the inputs from the given mark.
    public override void markInputs(int mark)
    {
        base.markInputs(mark);
        scale.mark = offset.mark = mark;
    }

    // Enforce this constraint. Assume that it is satisfied.
    public override void execute()
    {
        if (direction == forward)
            v2.value = v1.value * scale.value + offset.value;
        else
            v1.value = (v2.value - offset.value) / scale.value;
    }

    // Calculate the walkabout strength, the stay flag, and, if it is
    // 'stay', the value for the current output of this
    // constraint. Assume this constraint is satisfied.
    public override void recalculate()
    {
        Variable invar = input(), outvar = output();
        outvar.walkStrength = Strength.weakestOf(strength, invar.walkStrength);
        outvar.stay = invar.stay && scale.stay && offset.stay;
        if (outvar.stay) execute(); // stay optimization
    }
}


// ------------------------------------------------------------


// A Plan is an ordered list of constraints to be executed in sequence
// to resatisfy all currently satisfiable constraints in the face of
// one or more changing inputs.

internal class Plan
{
    private ArrayList _v;

    public Plan() { _v = new ArrayList(); }

    public void addConstraint(Constraint c) { _v.Add(c); }

    public int size() { return _v.Count; }

    public Constraint constraintAt(int index)
    {
        return (Constraint)_v[index];
    }

    public void execute()
    {
        for (int i = 0; i < size(); ++i)
        {
            Constraint c = (Constraint)constraintAt(i);
            c.execute();
        }
    }
}


// ------------------------------------------------------------

// The deltablue planner

internal class Planner
{
    private int _currentMark = 0;

    // Select a previously unused mark value.
    private int newMark() { return ++_currentMark; }

    public Planner()
    {
        _currentMark = 0;
    }

    // Attempt to satisfy the given constraint and, if successful,
    // incrementally update the dataflow graph.  Details: If satifying
    // the constraint is successful, it may override a weaker constraint
    // on its output. The algorithm attempts to resatisfy that
    // constraint using some other method. This process is repeated
    // until either a) it reaches a variable that was not previously
    // determined by any constraint or b) it reaches a constraint that
    // is too weak to be satisfied using any of its methods. The
    // variables of constraints that have been processed are marked with
    // a unique mark value so that we know where we've been. This allows
    // the algorithm to avoid getting into an infinite loop even if the
    // constraint graph has an inadvertent cycle.
    //
    public void incrementalAdd(Constraint c)
    {
        int mark = newMark();
        Constraint overridden = c.satisfy(mark);
        while (overridden != null)
        {
            overridden = overridden.satisfy(mark);
        }
    }


    // Entry point for retracting a constraint. Remove the given
    // constraint and incrementally update the dataflow graph.
    // Details: Retracting the given constraint may allow some currently
    // unsatisfiable downstream constraint to be satisfied. We therefore collect
    // a list of unsatisfied downstream constraints and attempt to
    // satisfy each one in turn. This list is traversed by constraint
    // strength, strongest first, as a heuristic for avoiding
    // unnecessarily adding and then overriding weak constraints.
    // Assume: c is satisfied.
    //
    public void incrementalRemove(Constraint c)
    {
        Variable outvar = c.output();
        c.markUnsatisfied();
        c.removeFromGraph();
        ArrayList unsatisfied = removePropagateFrom(outvar);
        Strength strength = Strength.required;
        do
        {
            for (int i = 0; i < unsatisfied.Count; ++i)
            {
                Constraint u = (Constraint)unsatisfied[i];
                if (u.strength == strength)
                    incrementalAdd(u);
            }
            strength = strength.nextWeaker();
        } while (strength != Strength.weakest);
    }

    // Recompute the walkabout strengths and stay flags of all variables
    // downstream of the given constraint and recompute the actual
    // values of all variables whose stay flag is true. If a cycle is
    // detected, remove the given constraint and answer
    // false. Otherwise, answer true.
    // Details: Cycles are detected when a marked variable is
    // encountered downstream of the given constraint. The sender is
    // assumed to have marked the inputs of the given constraint with
    // the given mark. Thus, encountering a marked node downstream of
    // the output constraint means that there is a path from the
    // constraint's output to one of its inputs.
    //
    public Boolean addPropagate(Constraint c, int mark)
    {
        ArrayList todo = new ArrayList();
        todo.Add(c);
        while (!(todo.Count == 0))
        {
#if USE_STACK
            Constraint d = (Constraint)todo[todo.Count - 1];
            todo.RemoveAt(todo.Count - 1);
#else
            Constraint d= (Constraint)todo[0];
            todo.RemoveAt(0);
#endif
            if (d.output().mark == mark)
            {
                incrementalRemove(c);
                return false;
            }
            d.recalculate();
            addConstraintsConsumingTo(d.output(), todo);
        }
        return true;
    }


    // The given variable has changed. Propagate new values downstream.
    public void propagateFrom(Variable v)
    {
        ArrayList todo = new ArrayList();
        addConstraintsConsumingTo(v, todo);
        while (!(todo.Count == 0))
        {
#if USE_STACK
            Constraint c = (Constraint)todo[todo.Count - 1];
            todo.RemoveAt(0);
#else
            Constraint c= (Constraint)todo[todo.Count-1];
            todo.RemoveAt(0);
#endif
            c.execute();
            addConstraintsConsumingTo(c.output(), todo);
        }
    }

    // Update the walkabout strengths and stay flags of all variables
    // downstream of the given constraint. Answer a collection of
    // unsatisfied constraints sorted in order of decreasing strength.
    //
    public ArrayList removePropagateFrom(Variable outvar)
    {
        outvar.determinedBy = null;
        outvar.walkStrength = Strength.weakest;
        outvar.stay = true;
        ArrayList unsatisfied = new ArrayList();
        ArrayList todo = new ArrayList();
        todo.Add(outvar);
        while (!(todo.Count == 0))
        {
#if USE_STACK
            Variable v = (Variable)todo[todo.Count - 1];
            todo.RemoveAt(todo.Count - 1);
#else
            Variable v= (Variable)todo[0];
            todo.RemoveAt(0);
#endif
            for (int i = 0; i < v.constraints.Count; ++i)
            {
                Constraint c = (Constraint)v.constraints[i];
                if (!c.isSatisfied())
                    unsatisfied.Add(c);
            }
            Constraint determiningC = v.determinedBy;
            for (int i = 0; i < v.constraints.Count; ++i)
            {
                Constraint nextC = (Constraint)v.constraints[i];
                if (nextC != determiningC && nextC.isSatisfied())
                {
                    nextC.recalculate();
                    todo.Add(nextC.output());
                }
            }
        }
        return unsatisfied;
    }

    // Extract a plan for resatisfaction starting from the outputs of
    // the given constraints, usually a set of input constraints.
    //
    public Plan extractPlanFromConstraints(ArrayList constraints)
    {
        ArrayList sources = new ArrayList();
        for (int i = 0; i < constraints.Count; ++i)
        {
            Constraint c = (Constraint)constraints[i];
            if (c.isInput() && c.isSatisfied())
                sources.Add(c);
        }
        return makePlan(sources);
    }

    // Extract a plan for resatisfaction starting from the given source
    // constraints, usually a set of input constraints. This method
    // assumes that stay optimization is desired; the plan will contain
    // only constraints whose output variables are not stay. Constraints
    // that do no computation, such as stay and edit constraints, are
    // not included in the plan.
    // Details: The outputs of a constraint are marked when it is added
    // to the plan under construction. A constraint may be appended to
    // the plan when all its input variables are known. A variable is
    // known if either a) the variable is marked (indicating that has
    // been computed by a constraint appearing earlier in the plan), b)
    // the variable is 'stay' (i.e. it is a constant at plan execution
    // time), or c) the variable is not determined by any
    // constraint. The last provision is for past states of history
    // variables, which are not stay but which are also not computed by
    // any constraint.
    // Assume: sources are all satisfied.
    //
    public Plan makePlan(ArrayList sources)
    {
        int mark = newMark();
        Plan plan = new Plan();
        ArrayList todo = sources;
        while (!(todo.Count == 0))
        {
#if USE_STACK
            Constraint c = (Constraint)todo[todo.Count - 1];
            todo.RemoveAt(todo.Count - 1);
#else
            Constraint c= (Constraint)todo[todo.Count-1];
            todo.RemoveAt(0);
#endif
            if (c.output().mark != mark && c.inputsKnown(mark))
            {
                // not in plan already and eligible for inclusion
                plan.addConstraint(c);
                c.output().mark = mark;
                addConstraintsConsumingTo(c.output(), todo);
            }
        }
        return plan;
    }

    public void addConstraintsConsumingTo(Variable v, ArrayList coll)
    {
        Constraint determiningC = v.determinedBy;
        ArrayList cc = v.constraints;
        for (int i = 0; i < cc.Count; ++i)
        {
            Constraint c = (Constraint)cc[i];
            if (c != determiningC && c.isSatisfied())
                coll.Add(c);
        }
    }
}

//------------------------------------------------------------

public class deltablue
{
    internal static Planner planner;
    internal static int chains, projections;

    public static int Main(String[] args)
    {
        deltablue d = new deltablue();
        bool result = d.inst_main(args);
        return (result ? 100 : -1);
    }

    [Benchmark]
    public static void Bench()
    {
        deltablue d = new deltablue();
        int iterations = 200;
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                d.inst_inner(iterations, false);
            }
        }
    }

    public bool inst_main(String[] args)
    {
        int iterations = 200; // read iterations from arguments, walter 7/97
        if (args.Length > 0)
        {
            bool parsed = Int32.TryParse(args[0], out iterations);
            if (!parsed)
            {
                Console.WriteLine("Error: expected iteration count, got '{0}'", args[0]);
                return false;
            }
        }

        inst_inner(iterations, true);

        return true;
    }

    public void inst_inner(int iterations, bool verbose)
    {
        chains = 0;         // NS 11/11
        projections = 0;    // NS 11/11
        if (verbose)
        {
            Console.WriteLine("deltablue parameters: " + iterations + " iterations");
        }

        DateTime start = DateTime.Now;
        for (int i = 0; i < iterations; i++)
        {
            chainTest(1000);
            projectionTest(1000);
        }
        DateTime end = DateTime.Now;
        TimeSpan dur = end - start;
        if (verbose)
        {
            Console.WriteLine("chains : " + chains); //NS
            Console.WriteLine("projections : " + projections); //NS
            Console.WriteLine("Doing {0} iters of Deltablue takes {1} ms; {2} us/iter.",
                              iterations, dur.TotalMilliseconds, (1000.0 * dur.TotalMilliseconds) / iterations);
        }
    }

    //  This is the standard DeltaBlue benchmark. A long chain of
    //  equality constraints is constructed with a stay constraint on
    //  one end. An edit constraint is then added to the opposite end
    //  and the time is measured for adding and removing this
    //  constraint, and extracting and executing a constraint
    //  satisfaction plan. There are two cases. In case 1, the added
    //  constraint is stronger than the stay constraint and values must
    //  propagate down the entire length of the chain. In case 2, the
    //  added constraint is weaker than the stay constraint so it cannot
    //  be accomodated. The cost in this case is, of course, very
    //  low. Typical situations lie somewhere between these two
    //  extremes.
    //
    private void chainTest(int n)
    {
        planner = new Planner();

        Variable prev = null, first = null, last = null;

        // Build chain of n equality constraints
        for (int i = 0; i <= n; i++)
        {
            String name = "v" + i;
            Variable v = new Variable(name);
            if (prev != null)
                new EqualityConstraint(prev, v, Strength.required);
            if (i == 0) first = v;
            if (i == n) last = v;
            prev = v;
        }

        new StayConstraint(last, Strength.strongDefault);
        Constraint editC = new EditConstraint(first, Strength.preferred);
        ArrayList editV = new ArrayList();
        editV.Add(editC);
        Plan plan = planner.extractPlanFromConstraints(editV);
        for (int i = 0; i < 100; i++)
        {
            first.value = i;
            plan.execute();
            if (last.value != i)
                error("Chain test failed!");
        }
        editC.destroyConstraint();
        deltablue.chains++;
    }


    // This test constructs a two sets of variables related to each
    // other by a simple linear transformation (scale and offset). The
    // time is measured to change a variable on either side of the
    // mapping and to change the scale and offset factors.
    //
    private void projectionTest(int n)
    {
        planner = new Planner();

        Variable scale = new Variable("scale", 10);
        Variable offset = new Variable("offset", 1000);
        Variable src = null, dst = null;

        ArrayList dests = new ArrayList();

        for (int i = 0; i < n; ++i)
        {
            src = new Variable("src" + i, i);
            dst = new Variable("dst" + i, i);
            dests.Add(dst);
            new StayConstraint(src, Strength.normal);
            new ScaleConstraint(src, scale, offset, dst, Strength.required);
        }

        change(src, 17);
        if (dst.value != 1170) error("Projection test 1 failed!");

        change(dst, 1050);
        if (src.value != 5) error("Projection test 2 failed!");

        change(scale, 5);
        for (int i = 0; i < n - 1; ++i)
        {
            if (((Variable)dests[i]).value != i * 5 + 1000)
                error("Projection test 3 failed!");
        }

        change(offset, 2000);
        for (int i = 0; i < n - 1; ++i)
        {
            if (((Variable)dests[i]).value != i * 5 + 2000)
                error("Projection test 4 failed!");
        }
        deltablue.projections++;
    }

    private void change(Variable var, int newValue)
    {
        EditConstraint editC = new EditConstraint(var, Strength.preferred);
        ArrayList editV = new ArrayList();
        editV.Add(editC);
        Plan plan = planner.extractPlanFromConstraints(editV);
        for (int i = 0; i < 10; i++)
        {
            var.value = newValue;
            plan.execute();
        }
        editC.destroyConstraint();
    }

    public static void error(String s)
    {
        throw new Exception(s);
    }
}
