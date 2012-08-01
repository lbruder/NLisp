using System.Collections.Generic;
using System.Linq;

namespace org.lb.NLisp
{
    internal sealed class Environment
    {
        private static readonly Dictionary<Symbol, LispObject> protectedValues = new Dictionary<Symbol, LispObject>();
        private readonly Environment outer;
        private readonly Dictionary<Symbol, LispObject> values = new Dictionary<Symbol, LispObject>();
        private readonly Dictionary<Symbol, Lambda> macros = new Dictionary<Symbol, Lambda>();

        public Environment() { }
        public Environment(Environment outer) { this.outer = outer; }

        public LispObject Define(Symbol symbol, LispObject value)
        {
            if (protectedValues.ContainsKey(symbol)) throw new ConstantCanNotBeChangedException(symbol);
            values[symbol] = value;
            return value;
        }

        public LispObject Set(Symbol symbol, LispObject value)
        {
            if (protectedValues.ContainsKey(symbol)) throw new ConstantCanNotBeChangedException(symbol);
            if (values.ContainsKey(symbol)) values[symbol] = value;
            else if (outer == null) throw new SymbolNotFoundException(symbol);
            else outer.Set(symbol, value);
            return value;
        }

        public LispObject Get(Symbol symbol)
        {
            LispObject ret;
            if (protectedValues.TryGetValue(symbol, out ret)) return ret;
            if (values.TryGetValue(symbol, out ret)) return ret;
            if (outer == null) throw new SymbolNotFoundException(symbol);
            return outer.Get(symbol);
        }

        public void MakeSymbolConstant(Symbol symbol)
        {
            if (protectedValues.ContainsKey(symbol)) throw new ConstantCanNotBeChangedException(symbol);
            protectedValues[symbol] = Get(symbol);
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

        internal object GetSymbols()
        {
            return values.Keys.ToList();
        }
    }
}
