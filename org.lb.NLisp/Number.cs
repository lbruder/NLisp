using System.Globalization;

namespace org.lb.NLisp
{
    internal sealed class Number : LispObject
    {
        private readonly double number;
        public Number(double number) { this.number = number; }
        public int NumberAsInt { get { return (int)number; } }
        public override LispObject Eval(Environment env) { return this; }
        public override string ToString() { return number.ToString(CultureInfo.InvariantCulture); }
        public override bool Equals(object obj) { return (obj is Number) && ((Number)obj).number == number; }
        public override int GetHashCode() { return number.GetHashCode(); }
        public override LispObject Add(LispObject other) { return new Number(number + OtherNumber(other, "+")); }
        public override LispObject Sub(LispObject other) { return new Number(number - OtherNumber(other, "-")); }
        public override LispObject Mul(LispObject other) { return new Number(number * OtherNumber(other, "*")); }
        public override LispObject Div(LispObject other) { if (OtherNumber(other, "/") == 0) throw new LispDivisionByZeroException(); return new Number(number / OtherNumber(other, "/")); }
        public override LispObject Mod(LispObject other) { if (OtherNumber(other, "mod") == 0) throw new LispDivisionByZeroException(); return new Number(number % OtherNumber(other, "mod")); }
        public override LispObject NumEq(LispObject other) { return FromClrObject(number == OtherNumber(other, "=")); }
        public override LispObject Lt(LispObject other) { return FromClrObject(number < OtherNumber(other, "<")); }
        public override LispObject Gt(LispObject other) { return FromClrObject(number > OtherNumber(other, ">")); }
        private double OtherNumber(LispObject other, string op)
        {
            Number n = other as Number;
            if (n == null) throw new LispInvalidOperationException(this, other, op);
            return n.number;
        }
    }
}