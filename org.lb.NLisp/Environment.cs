using System.Collections.Generic;
using System.Linq;

namespace org.lb.NLisp
{
    internal sealed class Environment
    {
        private static readonly Dictionary<Symbol, LispObject> protectedValues = new Dictionary<Symbol, LispObject>();
        private static readonly Dictionary<Symbol, Lambda> protectedMacros = new Dictionary<Symbol, Lambda>();
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
            protectedValues[symbol] = Get(symbol);
        }

        public void DefineMacro(Symbol symbol, Lambda value)
        {
            if (protectedMacros.ContainsKey(symbol)) throw new ConstantCanNotBeChangedException(symbol);
            macros[symbol] = value;
        }

        public Lambda GetMacro(Symbol symbol)
        {
            Lambda ret;
            if (protectedMacros.TryGetValue(symbol, out ret)) return ret;
            if (macros.TryGetValue(symbol, out ret)) return ret;
            if (outer == null) return null;
            return outer.GetMacro(symbol);
        }

        public void MakeMacroConstant(Symbol symbol)
        {
            protectedMacros[symbol] = GetMacro(symbol);
        }

        internal object GetSymbols()
        {
            return values.Keys.ToList();
        }

        internal object GetMacros()
        {
            return macros.Keys.ToList();
        }
    }
}
