using System.Collections.Generic;

namespace org.lb.NLisp
{
    public sealed class Environment
    {
        private static readonly LispSymbol tSym = LispSymbol.fromString("t");
        private static readonly LispSymbol nilSym = LispSymbol.fromString("nil");
        private readonly Environment outer;
        private readonly Dictionary<LispSymbol, LispObject> values = new Dictionary<LispSymbol, LispObject>();
        public Environment()
        {
            values[LispSymbol.fromString("nil")] = LispNil.GetInstance();
            values[LispSymbol.fromString("t")] = LispT.GetInstance();
        }
        public Environment(Environment outer)
        {
            values[LispSymbol.fromString("nil")] = LispNil.GetInstance();
            values[LispSymbol.fromString("t")] = LispT.GetInstance();
            this.outer = outer;
        }

        public LispObject Define(LispSymbol symbol, LispObject value)
        {
            if (tSym.Equals(symbol) || nilSym.Equals(symbol)) throw new LispConstantCanNotBeChangedException(symbol);
            values[symbol] = value;
            return value;
        }

        public LispObject Set(LispSymbol symbol, LispObject value)
        {
            if (values.ContainsKey(symbol)) values[symbol] = value;
            else if (outer == null) throw new LispSymbolNotFoundException(symbol);
            else outer.Set(symbol, value);
            return value;
        }

        public LispObject Get(LispSymbol symbol)
        {
            LispObject ret;
            if (values.TryGetValue(symbol, out ret)) return ret;
            if (outer == null) throw new LispSymbolNotFoundException(symbol);
            return outer.Get(symbol);
        }
    }
}
