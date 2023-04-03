// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Tests for moving exits out of loops and ensuring that doing so doesn't
// violate EH clause nesting rules.

namespace N
{
    public class C
    {
        // Simple search loop: should move the "return true" out of the loop
        static bool Simple(int[] values)
        {
            foreach (int value in values)
            {
                if (value == 5)
                {
                    return true;
                }
            }
            return false;
        }

        // Nested loop with return that exits both: should move exit out of both.
        static bool Nested(int[][] values)
        {
            foreach (int[] innerValues in values)
            {
                foreach(int value in innerValues)
                {
                    if (value == 5)
                    {
                        CallSomeMethod();
                        return true;
                    }
                }
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CallSomeMethod(int n = 0) { }

        static int SharedTop(int n, int a, int b, int x, int y)
        {
            int result = 0;

            do
            {
                do
                {
                    result += x * y + x * x + y * y;
                } while ((--n & 3) != 1);
                ++x;
                result += a * b + a * a + b * b;
            } while ((--n) != 0);

            return result;
        }

        class Node
        {
            public int value;
            public Node next;
        }

        // Entire loop is enclosed in a try block: should still be able to move
        // exit path out of loop.
        static bool InTry(Node node)
        {
            try
            {
                while (node != null)
                {
                    if (node.value == 5)
                    {
                        return (node.next.value == 7);
                    }
                    node = node.next;
                }
            }
            catch (NullReferenceException) { }
            return false;
        }

        // Nested loop again, make sure throws are recognized as exits -- throw should
        // be moved out of loop
        static bool NestedThrows(int[][] values)
        {
            try
            {
                foreach (int[] innerValues in values)
                {
                    foreach (int value in innerValues)
                    {
                        if (value == 5)
                        {
                            throw new System.Exception("foo");
                        }
                    }
                }
                return false;
            }
            catch
            {
                return true;
            }
        }

        // Can't move out of loop without crossing try region boundary; should leave in loop.
        // Loop should still be recognized, and invariant multiplication hoisted out of it.
        static bool CrossTry(Node node, int x, int y)
        {
            int r = 0;
            while (node != null)
            {
                r += x * y * x * y * x * y * x * y * x * y * x * y;
                try
                {
                    if (node.value == 5)
                    {
                        return (node.next.value == 7);
                    }
                }
                catch (NullReferenceException) { return true; }
                node = node.next;
            }
            return (r < 13);
        }

        // Example where we move a branch out to its target and so should make
        // it fall through.
        static bool MoveToTarget(int n, int j)
        {
            CallSomeMethod();

            do // Loop 1 starts here
            {
                if (n == 1)
                {
                    // This path exits Loop 1, so should get moved out.
                    CallSomeMethod(1);
                    goto One;
                }
                n = ((n & 1) == 0 ? n / 2 : 3 * n + 1);
            } while (n != 0);
            // Exit when n == 0 falls through here, so we'll move Call(1) farther down
            CallSomeMethod(2);
            return false;
            // No fallthrough here, so we'll move CallSomeMethod(1) here.
            // But that means the 'goto One' will become a goto-next that we should
            // clean up.
            One:
            // Making the block at 'One' a loop head prompts an assertion failure
            // analyzing that loop (wasn't expecting branch-to-next since it runs
            // after flow opts) if we don't clean up the branch-to-next.
            do
            {
                CallSomeMethod(3);
            } while (--j != 0);

            return true;
        }

        // Example where we move a run of blocks that a goto in the loop
        // branches around.
        static bool ContractGoto(int n, int j)
        {
            CallSomeMethod();

            do  // Loop 1
            {
                CallSomeMethod(1);
                goto StillInLoop;  // this goto will become goto-next when Call(2) gets moved

                EarlyReturn:   // this should get moved out-of-line
                CallSomeMethod(2);
                return false;

                StillInLoop:  // make this the top of a loop so assertion would fire if goto left
                do  // Loop 2
                {
                    n = ((n & 1) == 0 ? n / 2 : 3 * n + 1);
                } while (n == 0);

                if (j < 0)
                {
                    goto EarlyReturn;
                }

            } while (n != 1);

            return true;
        }

        // Example where we move a label to just after a goto targeting it and so should
        // changethat goto-next to fall through.
        static bool MoveAfterGoto(int n, int j)
        {
            CallSomeMethod();

            do // Loop 1 starts here
            {
                if (j > 0)
                {
                    // This path exits Loop 1, so should get moved out.
                    CallSomeMethod(1);
                    goto MovingLabel;
                }
                // This block stays in the loop.
                n = ((n & 1) == 0 ? n / 2 : 3 * n + 1);
                continue;
                MovingLabel: // This gets moved out, just after its goto, requring cleanup
                do  // Loop 2 (gets moved out of Loop 1)
                {
                    CallSomeMethod(3);
                } while (--j != 0);
                return true;
            } while (n != 1);
            // Exit when n == 0 falls through here, so we'll move Call(1) farther down
            CallSomeMethod(2);
            return false;
        }

        // Test to make sure we check for exits on fall-through edges.
        static bool FallThroughExit(int n, int j, int k, int l)
        {
            CallSomeMethod(1);

            do
            {
                CallSomeMethod(2);
                if (n == 1)  // There is an exit here but it's fall-through
                {
                    CallSomeMethod(3);
                    break;
                }
                // This is not an articulated block, but missing the first exit
                // would make us think it is and we might hoist this big expression.
                CallSomeMethod(k * j + k * k + j * j + k * l + j * l + l * l);
                n = ((n & 1) == 0 ? n / 2 : 3 * n + 1);
            } while (n > 1);  // This will be the only exit we see if we miss the other
            CallSomeMethod(4);
            return true;
        }

        // Repro case for a specific corner case in loop processing code
        // (chunk of blocks moved after entry of another loop leading to
        // a new block immediately following the entry, which subsequently
        // gets moved to a third loop where its PredecessorNum is evaluated).
        static int MoveAfterEntry(int n, int m, int x, int k)
        {
            int result = 0;

            try
            {

                do  // Loop 1
                {
                    CallSomeMethod(1);

                    if (n == 5)  // The body of this `if` will get moved out of Loop 1
                    {
                        MaybeThrow(k);
                        result += 6;
                        goto Finish;
                    }

                    result += 3;
                } while (--n > 0);

                CallSomeMethod(3);

                do // Loop 2
                {
                    CallSomeMethod(4);

                    if (m == 3)
                    {
                        // Code moved out of Loop 1 will end up here; all of the above
                        // blocks have fallthrough, and we don't want to move into the `try` region
                        // That means we'll need to insert a new block just before the moved code
                        // that jumps around it to enter the `try`.
                        // Subsequently we'll want to move the same chunk of code out of Loop 2;
                        // the new block should stay with CallSomeMethod(4), not move with the
                        // chunk pulled down from Loop 1, even if this is an exit path.

                        try
                        {
                            CallSomeMethod(5);
                            result += 8;
                        }
                        catch
                        {
                            // Getting an exception here means something failed
                            result -= 100;
                        }
                        goto Finish;
                    }
                    result += 9;
                } while (--m > 0);

                CallSomeMethod(6);

                do // Loop 3
                {
                    result += 13;
                    try { CallSomeMethod(); } catch { goto Finish; } // EH here to trigger right path processing Loop 3
                    if (k == 88)
                    {
                        CallSomeMethod(7);
                        goto Finish;
                        // Blocks moved out of Loop 2 will get moved here.  Processing of Loop 3
                        // will then trip over the new block if we moved it out of Loop 2, while
                        // checking to see if these can be considered part of Loop 3.
                    }

                    CallSomeMethod(8);
                } while (--x > 0);

                CallSomeMethod(9);
            }
            catch {
                // Getting an exception here is expected when k == 21
            }

            Finish:
            return result;
        }


        // Test to make sure we can safely handle single-exit loops when the
        // algorithm to compact loops decrements the exit count from two back
        // to one and leaves it there.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void MaybeThrow(int n)
        {
            if (n == 21)
            {
                throw new Exception("Twenty-One");
            }
        }
        static bool InnerInfiniteLoop(int n, int j, int k, int l)
        {
            CallSomeMethod();

            do
            {
                CallSomeMethod(1);

                if (n == 1)  // This is the first exit
                {
                    break;
                }

                // This is not an articulated block, but missing the first exit
                // would make us think it is and we might hoist this big expression.
                CallSomeMethod(k * j + k * k + j * j + k * l + j * l + l * l);

                try
                {
                    if (n == 23)  // This is the second exit, but the exit path
                    {             // cannot be moved out due to the try block.
                        CallSomeMethod(3);
                        do
                        {
                            MaybeThrow(--n);
                        } while (true);
                    }
                }
                catch
                {
                    return false;
                }
                n = ((n & 1) == 0 ? n / 2 : 3 * n + 1);
            } while (true);

            CallSomeMethod(4);

            if (j < 0)
            {
                return false;
            }
            return true;
        }

        class Box
        {
            public int Data;
        }

        // Test to make sure we don't record a loop and then
        // move some of its blocks out.
        static bool InOut(int n, Box box)
        {
            int value = box.Data;
            int target = 0;
            int result = 0;

            do  // Outer loop starts here
            {
                result += box.Data;  // Hoisting this is illegal due to the write in the inner loop
                goto enterInnerLoop; // branch around 'continueOuterLoop'

                continueOuterLoop:  // Target for lexically-backward exit from inner loop that
                ++result;           // is not an exit from the outer loop
                continue;

                enterInnerLoop:
                --result;
                do  // Inner loop starts here
                {
                    if (target == 0)  // Conditional exit from inner loop that we'll want to move out
                    {                 // but need to be careful not to move it out of the outer loop
                        box.Data = value + 1;  // ValueNumbering needs to see that this store is part of
                        target = value * n;    // the outer loop, else we'll think the load above is invariant.
                        goto continueOuterLoop;
                    }
                } while (box.Data < 0);
            } while (--n > 0);

            return (result == target);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int[] has5 = new int[] { 1, 2, 3, 4, 5 };
            int[] no5 = new int[] { 6, 7, 8, 9 };
            int[][] has5jagged = new int[][] { no5, has5 };
            int[][] no5jagged = new int[][] { no5, no5 };
            Node faultHead = new Node { value = 6, next = new Node { value = 13, next = new Node { value = 5, next = null } } };
            Node trueHead = new Node { value = 23, next = new Node { value = 5, next = new Node { value = 7, next = null } } };
            Node falseHead = new Node { value = 5, next = new Node { value = 8, next = null } };

            int result = 100; // 100 indicates success; increment for errors.

            if (!Simple(has5) || Simple(no5))
            {
                ++result;
            }
            if (!Nested(has5jagged) || Nested(no5jagged))
            {
                ++result;
            }
            if (SharedTop(17, 2, 3, 4, 5) != 1145)
            {
                ++result;
            }
            if (!InTry(trueHead) || InTry(falseHead) || InTry(faultHead))
            {
                ++result;
            }
            if (!NestedThrows(has5jagged) || NestedThrows(no5jagged))
            {
                ++result;
            }
            if (!CrossTry(trueHead, 8, 4) || CrossTry(falseHead, 8, 4) || !CrossTry(faultHead, 8, 4))
            {
                ++result;
            }

            if (!MoveToTarget(7, 2))
            {
                ++result;
            }
            if (!ContractGoto(23, 5) || ContractGoto(42, -5))
            {
                ++result;
            }
            if (!MoveAfterGoto(23, 5) || MoveAfterGoto(42, -5))
            {
                ++result;
            }

            if (!FallThroughExit(8, 5, 7, 22))
            {
                ++result;
            }
            if ((MoveAfterEntry(15, 16, 17, 18) != 36) || (MoveAfterEntry(8, 15, 20, 21) != 9))
            {
                ++result;
            }
            if (!InnerInfiniteLoop(8, 5, 3, 18) || InnerInfiniteLoop(16, -5, 2, 4) || InnerInfiniteLoop(23, 7, 6, 5) || !InnerInfiniteLoop(1, 0, 11, 22))
            {
                ++result;
            }

            if (!InOut(17, new Box() { Data = 5 }))
            {
                ++result;
            }

            return result;
        }
    }
}
