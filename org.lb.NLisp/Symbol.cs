using System.Collections.Generic;
using System.Globalization;

namespace org.lb.NLisp
{
    public sealed class Symbol : LispObject
    {
        private readonly string value;
        private Symbol(string value) { this.value = value; }

        private static readonly Dictionary<string, Symbol> cache = new Dictionary<string, Symbol>();
        public static Symbol fromString(string value)
        {
            Symbol ret;
            if (cache.TryGetValue(value, out ret)) return ret;
            ret = new Symbol(value);
            cache[value] = ret;
            return ret;
        }

        public override bool IsTrue() { return value != "nil"; }
        public override LispObject Eval(Environment env) { return env.Get(this); }
        public override string ToString() { return value; }
        public override bool Equals(object obj) { return obj == this; }
        public override int GetHashCode() { return value.GetHashCode(); }
        public static Symbol Gensym() { return fromString("#:G" + cache.Count.ToString(CultureInfo.InvariantCulture)); }
    }
}