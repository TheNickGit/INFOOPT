using System;
using System.Collections.Generic;


namespace Infoopt {


    // REPRESENTS A GENERIC DOUBLY LINKED-LIST
    // PRIMARILY HANDLES THE REFERENCES TO HEAD/TAIL OF SEQUENCE OF DOUBLY-NODES
    // NOTE: HEAD/TAIL SYNCHRONIZATION OF DOUBLY-LIST CAN GET FUCKT WHEN DOUBLY-NODES THEMSELVES ARE MODIFIED DIRECTLY
    class DoublyList<T> {

        
        public DoublyNode<T> head, tail;
        public int Length = 0;


        // CHECKS
        public bool isEmpty { get { return Object.ReferenceEquals(this.head, null); } }
        public bool isHead(DoublyNode<T> node) => node == this.head;
        public bool isTail(DoublyNode<T> node) => node == this.tail;


        // CONSTRUCTOR METHODS
        public static DoublyList<T> fromArray(T[] values)
        {
            DoublyNode<T> head = null, tail = null;
            foreach (T value in values)
            {
                if (Object.ReferenceEquals(head, null))
                    head = tail = new DoublyNode<T>(value);
                else
                    tail = tail.extendNext(value);
            }
            // TODO: Voeg de lengte van values[] toe aan de Length variabel
            return new DoublyList<T>(head, tail);
        }

        public DoublyList(DoublyNode<T> head = null, DoublyNode<T> tail = null)
        {
            if (!Object.ReferenceEquals(head, null) && !Object.ReferenceEquals(tail, null))
            {
                Program.Assert(head.prev == null && tail.next == null); // REMOVE AFTER DEBUGGING
                this.head = head;
                this.tail = this.head.extendNext(tail.value);
                this.Length = 2;
            }
        }


        // EXTEND AT HEAD/TAIL
        public DoublyNode<T> extendAtHead(T value)
        {
            if (this.isEmpty)
                this.head = this.tail = new DoublyNode<T>(value);
            else
                this.head = this.head.extendPrev(value);
            Length++;
            return this.head;
        }

        public DoublyNode<T> extendAtTail(T value)
        {
            if (this.isEmpty)
                this.head = this.tail = new DoublyNode<T>(value);
            else
                this.tail = this.tail.extendNext(value);
            Length++;
            return this.tail;
        }


        // INSERT BEFORE OR AFTER A SPECIFIC NODE
        public void insertBeforeNode(T value, DoublyNode<T> node)
        {
            DoublyNode<T> prev = node.insertPrev(value);
            if (this.isHead(node))
            {
                this.head = prev;
                Program.Assert(this.head.prev == null); // REMOVE AFTER DEBUGGING
            }
            Length++;
        }

        public void insertAfterNode(T value, DoublyNode<T> node)
        {
            DoublyNode<T> next = node.insertNext(value);
            if (this.isTail(node))
            {
                this.tail = next;
                Program.Assert(this.tail.next == null); // REMOVE AFTER DEBUGGING
            }
            Length++;
        }


        // EJECT (POP) NODE BEFORE OR AFTER A SPECIFIC NODE
        public DoublyNode<T> ejectBeforeNode(DoublyNode<T> node)
        {
            if (this.isHead(node))
            {
                this.head = node;
            }
            Length--;
            return node.ejectPrev();
        }

        public DoublyNode<T> ejectAfterNode(DoublyNode<T> node)
        {
            if (this.isTail(node))
            {
                this.tail = node;
            }
            Length--;
            return node.ejectNext();
        }


        // SPLIT DOUBLY-LIST IN TWO PIECES BEFORE OR AFTER A SPECIFIC NODE
        // AND RETURNS THE DOUBLY-LIST THAT WAS SPLIT OFF
        public DoublyList<T> splitBeforeNode(DoublyNode<T> node)
        {
            if (this.isHead(node))
                return null;

            DoublyNode<T> splitHead = this.head,
                splitTail = node.splitPrev();
            this.head = node;
            return new DoublyList<T>(splitHead, splitTail);
        }

        public DoublyList<T> splitAfterNode(DoublyNode<T> node)
        {
            if (this.isTail(node))
                return null;

            DoublyNode<T> splitHead = node.splitNext(),
                splitTail = this.tail;
            this.tail = node;
            return new DoublyList<T>(splitHead, splitTail);
        }


        // SWAP THE POSITION OF TWO SPECIFIC NODES
        public void swapNodes(DoublyNode<T> n1, DoublyNode<T> n2)
        {
            if (this.isHead(n1)) this.head = n2;
            else if (this.isHead(n2)) this.head = n1;
            else if (this.isTail(n1)) this.tail = n2;
            else if (this.isTail(n2)) this.tail = n1;
            n1.swapWith(n2);
        }


        // ITERATOR ( go crazy with that forEach :) )  
        public IEnumerator<DoublyNode<T>> GetEnumerator()
        {
            DoublyNode<T> node = this.head;
            while (node != null)
            {
                yield return node;
                node = node.next;
            }
        }

        // CREATE A NEW DOUBLY-LIST FROM THE NODES FILTERED BY A CERTAIN PREDICATE
        public DoublyList<T> filterNodes(Func<DoublyNode<T>, bool> predicate)
        {
            DoublyNode<T> head = null, tail = null;
            foreach (DoublyNode<T> node in this)
            {
                if (predicate(node))
                {
                    if (Object.ReferenceEquals(head, null))
                        head = tail = new DoublyNode<T>(node.value);
                    else
                        tail = tail.extendNext(node.value);
                }
            }
            return new DoublyList<T>(head, tail);
        }

        // Check if this DLL contains the specified element
        public bool Contains(T target)
        {
            DoublyNode<T> current = this.head;
            if (this.head == null)
                return false;

            while (current.next != null)
            {
                if (current.value.Equals(target))
                    return true;
                else
                    current = current.next;
            }
            if (current.value.Equals(target))
                return true;
            else return false;
        }
    }



    // REPRESENTS A SINGLE NODE IN THE CONTEXT OF A DOUBLY LINKED LIST
    // PRIMARILY HANDLES SEQUENTIALIZATION BETWEEN LINKED NEIGHBOR NODES
    // NOTE: HEAD/TAIL SYNCHRONIZATION OF DOUBLY-LIST CAN GET FUCKT WHEN DOUBLY-NODES THEMSELVES ARE MODIFIED DIRECTLY
    class DoublyNode<T>
    {

        public T value;
        public DoublyNode<T> prev, next;

        public DoublyNode(T value)
        {
            this.value = value;
        }


        // ADD VALUE TO BACK/FRONT; ASSUMING THAT NODE TO BE EMPTY (OVERWRITTEN OTHERWISE!!)
        public DoublyNode<T> extendPrev(T prevValue)
        {
            this.prev = new DoublyNode<T>(prevValue);
            this.prev.next = this;
            return this.prev;
        }

        public DoublyNode<T> extendNext(T nextValue)
        {
            this.next = new DoublyNode<T>(nextValue);
            this.next.prev = this;
            return this.next;
        }


        // INSERT VALUE AT BACK/FRONT; TAKES INTO ACCOUNT WHETHER THAT NODE IS EMPTY OR NOT
        public DoublyNode<T> insertPrev(T prevValue)
        {
            if (Object.ReferenceEquals(this.prev, null))
                return this.extendPrev(prevValue);

            DoublyNode<T> insert = new DoublyNode<T>(prevValue);
            this.prev.next = insert;
            insert.prev = this.prev;
            insert.next = this;
            this.prev = insert;
            return insert;
        }

        public DoublyNode<T> insertNext(T nextValue)
        {
            if (Object.ReferenceEquals(this.next, null))
                return this.extendNext(nextValue);

            DoublyNode<T> insert = new DoublyNode<T>(nextValue);
            this.next.prev = insert;
            insert.next = this.next;
            insert.prev = this;
            this.next = insert;
            return insert;
        }


        // EJECT NODE AT BACK/FRONT; FULLY SEPARATING IT AND STITCHING THIS NODE TOGETHER WITH EJECTEE'S PREDECESSOR/SUCESSOR
        public DoublyNode<T> ejectPrev()
        {
            DoublyNode<T> eject = this.prev;
            if (!Object.ReferenceEquals(eject, null))
            {
                this.prev = eject.prev;
                this.prev.next = this;
                eject.prev = null;
                eject.next = null;
            }
            return eject;
        }

        public DoublyNode<T> ejectNext()
        {
            DoublyNode<T> eject = this.next;
            if (!Object.ReferenceEquals(eject, null))
            {
                this.next = eject.next;
                this.next.prev = this;
                eject.prev = null;
                eject.next = null;
            }
            return eject;
        }


        // SPLIT NODE AT BACK/FRONT; ENDING THE BACK/FRONT OF THIS NODE AND KEEPING PREDECESSORS/SUCESSORS BOUND TO REMOVED NODE
        public DoublyNode<T> splitPrev()
        {
            DoublyNode<T> split = this.next;
            if (!Object.ReferenceEquals(split, null))
            {
                this.next = null;
                split.prev = null;
            }
            return split;
        }

        public DoublyNode<T> splitNext()
        {
            DoublyNode<T> split = this.next;
            if (!Object.ReferenceEquals(split, null))
            {
                this.next = null;
                split.prev = null;
            }
            return split;
        }



        // RETRIEVE A NODE THAT LIES 'N' NODES BEHIND/AHEAD THIS NODE; NULL IF END OF LIST
        public DoublyNode<T> skipBackward(int n)
        {
            if (n > 0)
            {
                return Object.ReferenceEquals(this.next, null)
                    ? null
                    : this.next.skipBackward(n - 1);
            }
            return this;
        }

        public DoublyNode<T> skipForward(int n)
        {
            if (n > 0)
            {
                return Object.ReferenceEquals(this.next, null)
                    ? null
                    : this.next.skipForward(n - 1);
            }
            return this;
        }


        // SWAP THIS NODE WITH ANOTHER NODE
        public void swapWith(DoublyNode<T> n1)
        {
            DoublyNode<T> newPrev = n1.prev, newNext = n1.next;
            n1.prev = this.prev;
            n1.next = this.next;
            this.prev = newPrev;
            this.next = newNext;
        }

    }

}
