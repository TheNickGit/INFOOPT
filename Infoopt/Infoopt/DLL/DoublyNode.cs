
using System;

class DoublyNode<T>
{

    public T value;
    public DoublyNode<T> prev, next;

    public DoublyNode(T value)
    {
        this.value = value;
    }


    // ADD VALUE TO BACK/FRONT; ASSUMING THAT NODE TO BE EMPTY (OVERWRITTEN OTHERWISE!!)
    public DoublyNode<T> ExtendPrev(T prevValue)
    {
        this.prev = new DoublyNode<T>(prevValue);
        this.prev.next = this;
        return this.prev;
    }

    public DoublyNode<T> ExtendNext(T nextValue)
    {
        this.next = new DoublyNode<T>(nextValue);
        this.next.prev = this;
        return this.next;
    }


    // INSERT VALUE AT BACK/FRONT; TAKES INTO ACCOUNT WHETHER THAT NODE IS EMPTY OR NOT
    public DoublyNode<T> InsertPrev(T prevValue)
    {
        if (Object.ReferenceEquals(this.prev, null))
            return this.ExtendPrev(prevValue);

        DoublyNode<T> insert = new DoublyNode<T>(prevValue);
        this.prev.next = insert;
        insert.prev = this.prev;
        insert.next = this;
        this.prev = insert;
        return insert;
    }

    public DoublyNode<T> InsertNext(T nextValue)
    {
        if (Object.ReferenceEquals(this.next, null))
            return this.ExtendNext(nextValue);

        DoublyNode<T> insert = new DoublyNode<T>(nextValue);
        this.next.prev = insert;
        insert.next = this.next;
        insert.prev = this;
        this.next = insert;
        return insert;
    }


    // EJECT NODE AT BACK/FRONT; FULLY SEPARATING IT AND STITCHING THIS NODE TOGETHER WITH EJECTEE'S PREDECESSOR/SUCESSOR
    public DoublyNode<T> EjectPrev()
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

    public DoublyNode<T> EjectNext()
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

    // RETRIEVE A NODE THAT LIES 'N' NODES BEHIND/AHEAD THIS NODE; NULL IF END OF LIST
    public DoublyNode<T> SkipBackward(int n)
    {
        if (n > 0)
        {
            return Object.ReferenceEquals(this.next, null)
                ? null
                : this.next.SkipBackward(n - 1);
        }
        return this;
    }

    public DoublyNode<T> SkipForward(int n)
    {
        if (n > 0)
        {
            return Object.ReferenceEquals(this.next, null)
                ? null
                : this.next.SkipForward(n - 1);
        }
        return this;
    }

    // SWAP THIS NODE WITH ANOTHER NODE - version 2
    public void SwapWith(DoublyNode<T> n1)
    {
        (n1.value, this.value) = (this.value, n1.value);
    }
}