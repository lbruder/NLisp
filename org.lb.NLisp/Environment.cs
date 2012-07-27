using System.Collections.Generic;

namespace org.lb.NLisp
{
    public sealed class Environment
    {
        private static readonly Symbol tSym = Symbol.fromString("t");
        private static readonly Symbol nilSym = Symbol.fromString("nil");
        private readonly Environment outer;
        private readonly Dictionary<Symbol, LispObject> values = new Dictionary<Symbol, LispObject>();
        public Environment()
        {
            values[Symbol.fromString("nil")] = Nil.GetInstance();
            values[Symbol.fromString("t")] = T.GetInstance();
        }
        public Environment(Environment outer)
        {
            values[Symbol.fromString("nil")] = Nil.GetInstance();
            values[Symbol.fromString("t")] = T.GetInstance();
            this.outer = outer;
        }

        public LispObject Define(Symbol symbol, LispObject value)
        {
            if (tSym.Equals(symbol) || nilSym.Equals(symbol)) throw new LispConstantCanNotBeChangedException(symbol);
            values[symbol] = value;
            return value;
        }

        public LispObject Set(Symbol symbol, LispObject value)
        {
            if (values.ContainsKey(symbol)) values[symbol] = value;
            else if (outer == null) throw new LispSymbolNotFoundException(symbol);
            else outer.Set(symbol, value);
            return value;
        }

        public LispObject Get(Symbol symbol)
        {
            LispObject ret;
            if (values.TryGetValue(symbol, out ret)) return ret;
            if (outer == null) throw new LispSymbolNotFoundException(symbol);
            return outer.Get(symbol);
        }
    }
}
