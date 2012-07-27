using System;
using System.Collections.Generic;
using System.Linq;

namespace org.lb.NLisp
{
    internal abstract class LispFunction : LispObject
    {
        internal override LispObject Eval(Environment env) { return this; }
        public abstract LispObject Call(List<LispObject> parameters);
    }

    internal abstract class BuiltinLispFunction : LispFunction
    {
        private readonly string name;
        protected BuiltinLispFunction(string name) { this.name = name; }
        public override string ToString() { return "<Builtin function " + name + ">"; }
        public override bool Equals(object obj) { return (obj as BuiltinLispFunction) != null && ((BuiltinLispFunction)obj).name.Equals(name); }
        public override int GetHashCode() { return name.GetHashCode(); }
        protected void AssertParameterCount(List<LispObject> parameters, int expected)
        {
            if (parameters.Count != expected) throw new LispExpectedNParametersGotMException(Symbol.fromString(name), expected, parameters.Count);
        }
        protected void AssertParameterCountAtLeast(List<LispObject> parameters, int expected)
        {
            if (parameters.Count < expected) throw new LispExpectedAtLeastNParametersGotMException(Symbol.fromString(name), expected, parameters.Count);
        }
    }

    internal sealed class LispFunctionProxy : LispFunction
    {
        private readonly Delegate f;
        public LispFunctionProxy(Delegate f) { this.f = f; }
        public override string ToString() { return "<Native function " + f + ">"; }
        public override bool Equals(object obj) { return (obj as LispFunctionProxy) != null && ((LispFunctionProxy)obj).f.Equals(f); }
        public override int GetHashCode() { return f.GetHashCode(); }

        public override LispObject Call(List<LispObject> parameters)
        {
            throw new NotImplementedException(); // TODO: Parametertypen anpassen, Funktion aufrufen per Reflection
        }
    }

    internal sealed class Lambda : BuiltinLispFunction
    {
        private static readonly Symbol restSym = Symbol.fromString("&rest");

        private readonly Environment outerEnvironment;
        private readonly Symbol[] parameterNames;
        private readonly bool hasRestParameter;
        private readonly List<LispObject> body;

        public override string ToString() { return "Lambda"; }

        public Lambda(Environment env, IEnumerable<LispObject> parameterNames, List<LispObject> body)
            : base("lambda")
        {
            outerEnvironment = env;
            this.parameterNames = parameterNames.Cast<Symbol>().ToArray();
            this.hasRestParameter = this.parameterNames.Length >= 2 && restSym.Equals(this.parameterNames[this.parameterNames.Length - 2]);
            this.body = body;
        }

        public override LispObject Call(List<LispObject> parameters)
        {
            var inner = hasRestParameter ? GetEnvironmentWithRestParameter(parameters) : GetEnvironmentWithoutRestParameter(parameters);
            LispObject ret = Nil.GetInstance();
            foreach (var i in body) ret = i.Eval(inner);
            return ret;
        }

        private Environment GetEnvironmentWithRestParameter(List<LispObject> parameters)
        {
            AssertParameterCountAtLeast(parameters, parameterNames.Length - 2);
            Environment inner = new Environment(outerEnvironment);
            for (int i = 0; i < parameterNames.Length - 2; ++i) inner.Define(parameterNames[i], parameters[i]);
            inner.Define(parameterNames[parameterNames.Length - 1], FromClrObject(parameters.Skip(parameterNames.Length - 2).ToList()));
            return inner;
        }

        private Environment GetEnvironmentWithoutRestParameter(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, parameterNames.Length);
            Environment inner = new Environment(outerEnvironment);
            for (int i = 0; i < parameterNames.Length; ++i) inner.Define(parameterNames[i], parameters[i]);
            return inner;
        }
    }
}