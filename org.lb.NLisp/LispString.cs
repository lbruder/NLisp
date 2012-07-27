using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace org.lb.NLisp
{
    internal sealed class LispString : LispObject, IEnumerable<LispObject>
    {
        private readonly string value;
        public LispString(string value) { this.value = value; }
        public override LispObject Eval(Environment env) { return this; }
        public override string ToString() { return '"' + value.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + '"'; }
        public override bool Equals(object obj) { return (obj is LispString) && ((LispString)obj).value.Equals(value); }
        public override int GetHashCode() { return value.GetHashCode(); }
        public override LispObject Add(LispObject other) { return new LispString(value + OtherString(other, "+")); }
        public override LispObject NumEq(LispObject other) { return FromClrObject(String.CompareOrdinal(value, OtherString(other, "=")) == 0); }
        public override LispObject Lt(LispObject other) { return FromClrObject(String.CompareOrdinal(value, OtherString(other, "<")) < 0); }
        public override LispObject Gt(LispObject other) { return FromClrObject(String.CompareOrdinal(value, OtherString(other, ">")) > 0); }
        private string OtherString(LispObject other, string op)
        {
            LispString n = other as LispString;
            if (n == null) throw new LispInvalidOperationException(this, other, op);
            return n.value;
        }
        public IEnumerator<LispObject> GetEnumerator() { return value.Select(c => FromClrObject(c)).GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        internal string Value { get { return value; } }
    }
}