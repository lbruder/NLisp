using System.Collections.Generic;

namespace org.lb.NLisp
{
    internal sealed class Environment
    {
        private static readonly Symbol tSym = Symbol.fromString("t");
        private static readonly Symbol nilSym = Symbol.fromString("nil");
        private readonly Environment outer;
        private readonly Dictionary<Symbol, LispObject> values = new Dictionary<Symbol, LispObject>();
        private readonly Dictionary<Symbol, Lambda> macros = new Dictionary<Symbol, Lambda>();

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
            if (tSym.Equals(symbol) || nilSym.Equals(symbol)) throw new ConstantCanNotBeChangedException(symbol); // TODO: Rather use a "Initialization phase" and don't allow changing values created in that phase afterwards
            values[symbol] = value;
            return value;
        }

        public LispObject Set(Symbol symbol, LispObject value)
        {
            if (values.ContainsKey(symbol)) values[symbol] = value;
            else if (outer == null) throw new SymbolNotFoundException(symbol);
            else outer.Set(symbol, value);
            return value;
        }

        public LispObject Get(Symbol symbol)
        {
            LispObject ret;
            if (values.TryGetValue(symbol, out ret)) return ret;
            if (outer == null) throw new SymbolNotFoundException(symbol);
            return outer.Get(symbol);
        }

        public void DefineMacro(Symbol symbol, Lambda value)
        {
            macros[symbol] = value;
        }

        public Lambda GetMacro(Symbol symbol)
        {
            Lambda ret;
            if (macros.TryGetValue(symbol, out ret)) return ret;
            if (outer == null) return null;
            return outer.GetMacro(symbol);
        }
    }
}
