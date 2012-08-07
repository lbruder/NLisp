using System;
using System.Collections;
using System.IO;

namespace org.lb.NLisp
{
    public abstract class LispObject
    {
        public virtual bool IsTrue() { return true; }
        internal virtual bool NullP() { return false; }
        internal virtual LispObject Eval(Environment env) { return Nil.GetInstance(); }
        internal virtual LispObject Car() { throw new ObjectIsNotAConsCellException(this); }
        internal virtual LispObject Cdr() { throw new ObjectIsNotAConsCellException(this); }
        internal virtual LispObject Add(LispObject other) { throw new InvalidOperationException(this, other, "+"); }
        internal virtual LispObject Sub(LispObject other) { throw new InvalidOperationException(this, other, "-"); }
        internal virtual LispObject Mul(LispObject other) { throw new InvalidOperationException(this, other, "*"); }
        internal virtual LispObject Div(LispObject other) { throw new InvalidOperationException(this, other, "/"); }
        internal virtual LispObject Mod(LispObject other) { throw new InvalidOperationException(this, other, "mod"); }
        internal virtual LispObject NumEq(LispObject other) { throw new InvalidOperationException(this, other, "="); }
        internal virtual LispObject Gt(LispObject other) { throw new InvalidOperationException(this, other, ">"); }
        internal virtual LispObject Lt(LispObject other) { throw new InvalidOperationException(this, other, "<"); }
        internal virtual LispObject Elt(int index) { throw new ObjectIsNotASequenceException(this); }
        internal virtual LispObject SetElt(int index, LispObject value) { throw new ObjectIsNotASequenceException(this); }

        public static LispObject FromClrObject(object source)
        {
            if (source == null) return Nil.GetInstance();
            if (source is LispObject) return (LispObject)source;
            if (source is bool) return ((bool)source) ? (LispObject)T.GetInstance() : Nil.GetInstance();
            if (source is byte) return new Number((byte)source);
            if (source is char) return new LispString(source.ToString()); // TODO: LispChar type?
            if (source is short) return new Number((short)source);
            if (source is ushort) return new Number((ushort)source);
            if (source is int) return new Number((int)source);
            if (source is uint) return new Number((uint)source);
            if (source is long) return new Number((long)source);
            if (source is ulong) return new Number((ulong)source);
            if (source is float) return new Number((float)source);
            if (source is double) return new Number((double)source);
            if (source is string) return new LispString((string)source);
            if (source is Stream) return new LispStream((Stream)source);
            if (source is IList)
            {
                LispObject ret = Nil.GetInstance();
                var list = (IList)source;
                for (int i = list.Count - 1; i >= 0; --i) ret = ConsCell.Cons(FromClrObject(list[i]), ret);
                return ret;
            }
            if (source is Delegate) return new LispFunctionProxy(source as Delegate);
            throw new ObjectCouldNotBeConvertedException(source);
        }
    }

    internal sealed class Nil : LispObject
    {
        private static readonly Nil instance = new Nil();
        private Nil() { }
        public static Nil GetInstance() { return instance; }
        internal override LispObject Car() { return this; }
        internal override LispObject Cdr() { return this; }
        internal override bool NullP() { return true; }
        public override bool IsTrue() { return false; }
        internal override LispObject Eval(Environment env) { return this; }
        public override string ToString() { return "NIL"; }
        public override bool Equals(object obj) { return obj is Nil; }
        public override int GetHashCode() { return 4711; }
    }

    internal sealed class T : LispObject
    {
        private static readonly T instance = new T();
        private T() { }
        public static T GetInstance() { return instance; }
        internal override LispObject Eval(Environment env) { return this; }
        public override string ToString() { return "T"; }
        public override bool Equals(object obj) { return obj is T; }
        public override int GetHashCode() { return 0815; }
    }
}
