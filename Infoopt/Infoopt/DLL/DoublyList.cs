
using System.Collections.Generic;
using System;

class DoublyList<T>
{
    public DoublyNode<T> head, tail;
    public int Length = 0;

    // CHECKS
    public bool IsEmpty { get { return Object.ReferenceEquals(this.head, null); } }
    public bool IsHead(DoublyNode<T> node) => node == this.head;
    public bool IsTail(DoublyNode<T> node) => node == this.tail;

    public bool IsHeadOrTail(DoublyNode<T> node) => this.IsHead(node) || this.IsTail(node);

    // CONSTRUCTOR METHODS
    public static DoublyList<T> FromArray(T[] values)
    {
        DoublyNode<T> head = null, tail = null;
        foreach (T value in values)
        {
            if (Object.ReferenceEquals(head, null))
                head = tail = new DoublyNode<T>(value);
            else
                tail = tail.ExtendNext(value);
        }
        // TODO: Voeg de lengte van values[] toe aan de Length variabel
        return new DoublyList<T>(head, tail);
    }

    public DoublyList(DoublyNode<T> head = null, DoublyNode<T> tail = null)
    {
        if (!Object.ReferenceEquals(head, null) && !Object.ReferenceEquals(tail, null))
        {
            this.head = head;
            this.tail = this.head.ExtendNext(tail.value);
            this.Length = 2;
        }
    }

    // EXTEND AT HEAD/TAIL
    public DoublyNode<T> ExtendAtHead(T value)
    {
        if (this.IsEmpty)
            this.head = this.tail = new DoublyNode<T>(value);
        else
            this.head = this.head.ExtendPrev(value);
        Length++;
        return this.head;
    }

    public DoublyNode<T> ExtendAtTail(T value)
    {
        if (this.IsEmpty)
            this.head = this.tail = new DoublyNode<T>(value);
        else
            this.tail = this.tail.ExtendNext(value);
        Length++;
        return this.tail;
    }


    // INSERT BEFORE OR AFTER A SPECIFIC NODE
    public void InsertBeforeNode(T value, DoublyNode<T> node)
    {
        DoublyNode<T> prev = node.InsertPrev(value);
        if (this.IsHead(node))
        {
            this.head = prev;
        }
        Length++;
    }

    public void InsertAfterNode(T value, DoublyNode<T> node)
    {
        DoublyNode<T> next = node.InsertNext(value);
        if (this.IsTail(node))
        {
            this.tail = next;
        }
        Length++;
    }


    // EJECT (POP) NODE BEFORE OR AFTER A SPECIFIC NODE
    public DoublyNode<T> EjectBeforeNode(DoublyNode<T> node)
    {
        if (this.IsHead(node))
        {
            this.head = node;
        }
        Length--;
        return node.EjectPrev();
    }

    public DoublyNode<T> EjectAfterNode(DoublyNode<T> node)
    {
        if (this.IsTail(node))
        {
            this.tail = node;
        }
        Length--;
        return node.EjectNext();
    }

    // SWAP THE VALUES OF TWO SPECIFIC NODES (v2)
    public static void SwapNodes(DoublyNode<T> n1, DoublyNode<T> n2)
    {
        n1.SwapWith(n2);
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
    public DoublyList<T> FilterNodes(Func<DoublyNode<T>, bool> predicate)
    {
        DoublyNode<T> head = null, tail = null;
        foreach (DoublyNode<T> node in this)
        {
            if (predicate(node))
            {
                if (Object.ReferenceEquals(head, null))
                    head = tail = new DoublyNode<T>(node.value);
                else
                    tail = tail.ExtendNext(node.value);
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

    // Find a specific target and return the node it's in.
    public DoublyNode<T> Find(T target)
    {
        DoublyNode<T> current = this.head;
        if (this.head == null)
            return null;

        while (current.next != null)
        {
            if (current.value.Equals(target))
                return current;
            else
                current = current.next;
        }
        if (current.value.Equals(target))
            return current;
        else return null;
    }
}
