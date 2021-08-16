// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

class SsaRenameState
{
    struct StackNode;

    class Stack
    {
        StackNode* m_top;

    public:
        Stack() : m_top(nullptr)
        {
        }

        StackNode* Top()
        {
            return m_top;
        }

        void Push(StackNode* node)
        {
            node->m_stackPrev = m_top;
            m_top             = node;
        }

        StackNode* Pop()
        {
            StackNode* top = m_top;
            m_top          = top->m_stackPrev;
            return top;
        }
    };

    struct StackNode
    {
        // Link to the previous stack top node
        StackNode* m_stackPrev;
        // Link to the previously pushed stack (used only when popping blocks)
        Stack* m_listPrev;
        // The basic block (used only when popping blocks)
        BasicBlock* m_block;
        // The actual information StackNode stores - the SSA number
        unsigned m_ssaNum;

        StackNode(Stack* listPrev, BasicBlock* block, unsigned ssaNum)
            : m_listPrev(listPrev), m_block(block), m_ssaNum(ssaNum)
        {
        }
    };

    // Memory allocator
    CompAllocator m_alloc;
    // Number of local variables to allocate stacks for
    unsigned m_lvaCount;
    // An array of stack objects, one for each local variable
    Stack* m_stacks;
    // The tail of the list of stacks that have been pushed to
    Stack* m_stackListTail;
    // Same state for the special implicit memory variables
    Stack m_memoryStack[MemoryKindCount];
    // A stack of free stack nodes
    Stack m_freeStack;

public:
    SsaRenameState(CompAllocator alloc, unsigned lvaCount);

    // Get the SSA number at the top of the stack for the specified variable.
    unsigned Top(unsigned lclNum);

    // Push a SSA number onto the stack for the specified variable.
    void Push(BasicBlock* block, unsigned lclNum, unsigned ssaNum);

    // Pop all stacks that have an entry for "block" on top.
    void PopBlockStacks(BasicBlock* block);

    // Similar functions for the special implicit memory variable.
    unsigned TopMemory(MemoryKind memoryKind)
    {
        return m_memoryStack[memoryKind].Top()->m_ssaNum;
    }

    void PushMemory(MemoryKind memoryKind, BasicBlock* block, unsigned ssaNum)
    {
        Push(&m_memoryStack[memoryKind], block, ssaNum);
    }

private:
    void EnsureStacks();

    // Allocate a new stack entry (possibly by popping it from the free stack)
    template <class... Args>
    StackNode* AllocStackNode(Args&&... args)
    {
        StackNode* stack = m_freeStack.Top();

        if (stack != nullptr)
        {
            m_freeStack.Pop();
        }
        else
        {
            stack = m_alloc.allocate<StackNode>(1);
        }

        return new (stack, jitstd::placement_t()) StackNode(std::forward<Args>(args)...);
    }

    // Push a SSA number onto a stack
    void Push(Stack* stack, BasicBlock* block, unsigned ssaNum);

    INDEBUG(void DumpStack(Stack* stack);)
};
