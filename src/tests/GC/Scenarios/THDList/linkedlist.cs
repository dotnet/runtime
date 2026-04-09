// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ThdList {
    using System.Threading;
    using System;
    using System.IO;

    public class Node
    {
        internal int m_data;
        internal Node m_pNext;
    }


    public class LinkedList
    {

        internal Node m_pHead;
        internal Random m_Random;

        public LinkedList(int ThreadId)
        {
            m_pHead = null;
            m_Random = new Random();
            // console synchronization Console.SetOut(TextWriter.Synchronized(Console.Out));
        }

        public void Empty (int ThreadId)
        {
            Console.WriteLine("Thread {0}: List Empty", ThreadId);
            m_pHead = null;
        }

        public void AddNodes (int howMany, int ThreadId)
        {
            //Adds howMany nodes to the linked list
            for (int i = 0; i < howMany; i++)
            {
                m_pHead = Insert(m_pHead, m_Random.Next(10));
            }
            Console.WriteLine("Thread {0} Added {1} Nodes", ThreadId, howMany);
        }

        public void DeleteNodes (int howMany, int ThreadId)
        {
            //Deletes howMany nodes from the linked list
            for (int i = 0; i < howMany; i++)
            {
                m_pHead = Delete(m_pHead, m_Random.Next(10));
            }
            Console.WriteLine("Thread {0} Deleted {1} Nodes", ThreadId, howMany);
        }

        private Node Insert(Node head, int element)
        {

            if(head == null)                                            //if is NULL make a new node
            {                                                           //and copy number to the new node
                head=new Node();                                        //make new node
                head.m_data = element;                                  //copy number
                head.m_pNext=null ;                                     //set the next to NULL
            }
            else
            {
                Node temp;
                temp = new Node();                                      //Add the new node as the head
                temp.m_data = element;
                temp.m_pNext = head;
                head = temp;
            }
            return head;
        }


        private Node Delete(Node head, int element)
        {
            if(head == null)
            {
                return head;                                                //Node not found
            }
            if (element == head.m_data)                                 //if it was the first data (node)
            {
                return head.m_pNext;
            }
            head.m_pNext = Delete(head.m_pNext, element);               //Recurse to the next element
            return head;                                                // in the list
        }
    }
}
