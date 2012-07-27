using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace org.lb.NLisp
{
    public abstract class LispObject
    {
        public virtual bool IsTrue() { return true; }
        public virtual bool NullP() { return false; }
        public virtual LispObject Eval(Environment env) { return LispNil.GetInstance(); }
        public virtual LispObject Car() { throw new LispObjectIsNotAListException(this); }
        public virtual LispObject Cdr() { throw new LispObjectIsNotAListException(this); }
        public virtual LispObject Add(LispObject other) { throw new LispInvalidOperationException(this, other, "+"); }
        public virtual LispObject Sub(LispObject other) { throw new LispInvalidOperationException(this, other, "-"); }
        public virtual LispObject Mul(LispObject other) { throw new LispInvalidOperationException(this, other, "*"); }
        public virtual LispObject Div(LispObject other) { throw new LispInvalidOperationException(this, other, "/"); }
        public virtual LispObject Mod(LispObject other) { throw new LispInvalidOperationException(this, other, "mod"); }
        public virtual LispObject NumEq(LispObject other) { throw new LispInvalidOperationException(this, other, "="); }
        public virtual LispObject Gt(LispObject other) { throw new LispInvalidOperationException(this, other, ">"); }
        public virtual LispObject Lt(LispObject other) { throw new LispInvalidOperationException(this, other, "<"); }

        public static LispObject FromClrObject(object source)
        {
            if (source == null) return LispNil.GetInstance();
            if (source is LispObject) return (LispObject)source;
            if (source is bool) return ((bool)source) ? (LispObject)LispT.GetInstance() : LispNil.GetInstance();
            if (source is byte) return new LispNumber((byte)source);
            if (source is char) return new LispString(source.ToString()); // TODO: LispChar type?
            if (source is short) return new LispNumber((short)source);
            if (source is ushort) return new LispNumber((ushort)source);
            if (source is int) return new LispNumber((int)source);
            if (source is uint) return new LispNumber((uint)source);
            if (source is long) return new LispNumber((long)source);
            if (source is ulong) return new LispNumber((ulong)source);
            if (source is float) return new LispNumber((float)source);
            if (source is double) return new LispNumber((double)source);
            if (source is string) return new LispString((string)source);
            if (source is IList)
            {
                LispObject ret = LispNil.GetInstance();
                var list = (IList)source;
                for (int i = list.Count - 1; i >= 0; --i) ret = LispConsCell.Cons(FromClrObject(list[i]), ret);
                return ret;
            }
            if (source is Delegate) return new LispFunctionProxy(source as Delegate);
            throw new LispObjectCouldNotBeConvertedException(source);
        }
    }

    internal sealed class LispNil : LispObject
    {
        private static readonly LispNil instance = new LispNil();
        private LispNil() { }
        public static LispNil GetInstance() { return instance; }
        public override LispObject Car() { return this; }
        public override LispObject Cdr() { return this; }
        public override bool NullP() { return true; }
        public override bool IsTrue() { return false; }
        public override LispObject Eval(Environment env) { throw new LispCannotEvaluateEmptyListException(); }
        public override string ToString() { return "NIL"; }
        public override bool Equals(object obj) { return obj is LispNil; }
        public override int GetHashCode() { return 4711; }
    }

    internal sealed class LispT : LispObject
    {
        private static readonly LispT instance = new LispT();
        private LispT() { }
        public static LispT GetInstance() { return instance; }
        public override LispObject Eval(Environment env) { return this; }
        public override string ToString() { return "T"; }
        public override bool Equals(object obj) { return obj is LispT; }
        public override int GetHashCode() { return 0815; }
    }

    internal sealed class LispNumber : LispObject
    {
        private readonly double number;
        public LispNumber(double number) { this.number = number; }
        public int NumberAsInt { get { return (int)number; } }
        public override LispObject Eval(Environment env) { return this; }
        public override string ToString() { return number.ToString(CultureInfo.InvariantCulture); }
        public override bool Equals(object obj) { return (obj is LispNumber) && ((LispNumber)obj).number == number; }
        public override int GetHashCode() { return number.GetHashCode(); }
        public override LispObject Add(LispObject other) { return new LispNumber(number + OtherNumber(other, "+")); }
        public override LispObject Sub(LispObject other) { return new LispNumber(number - OtherNumber(other, "-")); }
        public override LispObject Mul(LispObject other) { return new LispNumber(number * OtherNumber(other, "*")); }
        public override LispObject Div(LispObject other) { if (OtherNumber(other, "/") == 0) throw new LispDivisionByZeroException(); return new LispNumber(number / OtherNumber(other, "/")); }
        public override LispObject Mod(LispObject other) { if (OtherNumber(other, "mod") == 0) throw new LispDivisionByZeroException(); return new LispNumber(number % OtherNumber(other, "mod")); }
        public override LispObject NumEq(LispObject other) { return FromClrObject(number == OtherNumber(other, "=")); }
        public override LispObject Lt(LispObject other) { return FromClrObject(number < OtherNumber(other, "<")); }
        public override LispObject Gt(LispObject other) { return FromClrObject(number > OtherNumber(other, ">")); }
        private double OtherNumber(LispObject other, string op)
        {
            LispNumber n = other as LispNumber;
            if (n == null) throw new LispInvalidOperationException(this, other, op);
            return n.number;
        }
    }

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

    public sealed class LispSymbol : LispObject
    {
        private readonly string value;
        private LispSymbol(string value) { this.value = value; }

        private static readonly Dictionary<string, LispSymbol> cache = new Dictionary<string, LispSymbol>();
        public static LispSymbol fromString(string value)
        {
            LispSymbol ret;
            if (cache.TryGetValue(value, out ret)) return ret;
            ret = new LispSymbol(value);
            cache[value] = ret;
            return ret;
        }

        public override bool IsTrue() { return value != "nil"; }
        public override LispObject Eval(Environment env) { return env.Get(this); }
        public override string ToString() { return value; }
        public override bool Equals(object obj) { return obj == this; }
        public override int GetHashCode() { return value.GetHashCode(); }
        public static LispSymbol Gensym() { return fromString("#:G" + cache.Count.ToString(CultureInfo.InvariantCulture)); }
    }

    internal sealed class LispConsCell : LispObject, IEnumerable<LispObject>
    {
        private static readonly LispSymbol quoteSym = LispSymbol.fromString("quote");
        private static readonly LispSymbol ifSym = LispSymbol.fromString("if");
        private static readonly LispSymbol prognSym = LispSymbol.fromString("progn");
        private static readonly LispSymbol defineSym = LispSymbol.fromString("define");
        private static readonly LispSymbol defunSym = LispSymbol.fromString("defun");
        private static readonly LispSymbol setSym = LispSymbol.fromString("setq");
        private static readonly LispSymbol lambdaSym = LispSymbol.fromString("lambda");

        private readonly LispObject car;
        private readonly LispObject cdr;
        private LispConsCell(LispObject car, LispObject cdr) { this.car = car; this.cdr = cdr; }
        public static LispConsCell Cons(LispObject car, LispObject cdr) { return new LispConsCell(car, cdr); }
        public override LispObject Car() { return car; }
        public override LispObject Cdr() { return cdr; }
        public override LispObject Add(LispObject other) { var list = this.ToList(); list.AddRange(OtherConsCell(other, "+")); return FromClrObject(list); }
        private LispConsCell OtherConsCell(LispObject other, string op)
        {
            LispConsCell n = other as LispConsCell;
            if (n == null) throw new LispInvalidOperationException(this, other, op);
            return n;
        }

        public override LispObject Eval(Environment env)
        {
            if (quoteSym.Equals(car)) return ((LispConsCell)cdr).car;
            if (ifSym.Equals(car)) return EvalIf(env);
            if (prognSym.Equals(car)) return EvalProgn(env);
            if (defineSym.Equals(car)) return EvalDefine(env);
            if (setSym.Equals(car)) return EvalSet(env);
            if (lambdaSym.Equals(car)) return EvalLambda(env);
            if (defunSym.Equals(car)) return EvalDefun(env);
            return EvalCall(env);
        }

        private LispObject EvalIf(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length != 4) throw new LispExpectedNParametersGotMException(car, 3, asArray.Length - 1);
            return asArray[1].Eval(env).IsTrue() ? asArray[2].Eval(env) : asArray[3].Eval(env);
        }

        private LispObject EvalProgn(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length == 1) return LispNil.GetInstance();
            for (int i = 1; i < asArray.Length - 1; ++i) asArray[i].Eval(env);
            return asArray[asArray.Length - 1].Eval(env);
        }

        private LispObject EvalDefine(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length != 3) throw new LispExpectedNParametersGotMException(car, 2, asArray.Length - 1);
            if (asArray[1] is LispSymbol) return env.Define((LispSymbol)asArray[1], asArray[2].Eval(env));
            throw new LispSymbolExpectedException(asArray[1]);
        }

        private LispObject EvalSet(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length != 3) throw new LispExpectedNParametersGotMException(car, 2, asArray.Length - 1);
            if (asArray[1] is LispSymbol) return env.Set((LispSymbol)asArray[1], asArray[2].Eval(env));
            throw new LispSymbolExpectedException(asArray[1]);
        }

        private LispObject EvalLambda(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length < 2) throw new LispExpectedAtLeastNParametersGotMException(car, 1, asArray.Length - 1);
            if (asArray[1] is LispConsCell) return new Lambda(env, (LispConsCell)asArray[1], this.Skip(2).ToList());
            if (asArray[1] is LispNil) return new Lambda(env, new LispObject[] { }, this.Skip(2).ToList());
            throw new LispListExpectedException(asArray[1]);
        }

        private LispObject EvalDefun(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length < 4) throw new LispExpectedAtLeastNParametersGotMException(car, 3, asArray.Length - 1);
            if (!(asArray[1] is LispSymbol)) throw new LispSymbolExpectedException(asArray[1]);
            if (asArray[2] is LispConsCell) return env.Define((LispSymbol)asArray[1], new Lambda(env, (LispConsCell)asArray[2], this.Skip(3).ToList()));
            if (asArray[2] is LispNil) return env.Define((LispSymbol)asArray[1], new Lambda(env, new LispObject[] { }, this.Skip(3).ToList()));
            throw new LispListExpectedException(asArray[2]);
        }

        private LispObject EvalCall(Environment env)
        {
            var f = car.Eval(env);
            if (f is LispFunction) return ((LispFunction)f).Call(this.Skip(1).Select(o => o.Eval(env)).ToList());
            throw new LispUndefinedFunctionException(car);
        }

        public IEnumerator<LispObject> GetEnumerator()
        {
            yield return car;
            LispObject i = cdr;
            while (true)
            {
                if (i.NullP()) yield break;
                var asConsp = i as LispConsCell;
                if (asConsp == null)
                {
                    yield return i;
                    yield break;
                }
                yield return asConsp.car;
                i = asConsp.cdr;
            }
        }

        public override string ToString()
        {
            var ret = new StringBuilder();
            ret.Append('(');
            ret.Append(car);
            LispObject i = cdr;

            while (true)
            {
                if (i.NullP()) break;
                var asConsp = i as LispConsCell;
                if (asConsp == null)
                {
                    ret.Append(" . ");
                    ret.Append(i);
                    break;
                }
                ret.Append(' ');
                ret.Append(asConsp.car);
                i = asConsp.cdr;
            }
            ret.Append(')');
            return ret.ToString();
        }

        public override bool Equals(object obj)
        {
            var other = obj as LispConsCell;
            return (other != null) && other.car.Equals(car) && other.cdr.Equals(cdr);
        }

        public override int GetHashCode() { return 17 + car.GetHashCode() * 31 + cdr.GetHashCode(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

    internal abstract class LispFunction : LispObject
    {
        public override LispObject Eval(Environment env) { return this; }
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
            if (parameters.Count != expected) throw new LispExpectedNParametersGotMException(LispSymbol.fromString(name), expected, parameters.Count);
        }
        protected void AssertParameterCountAtLeast(List<LispObject> parameters, int expected)
        {
            if (parameters.Count < expected) throw new LispExpectedAtLeastNParametersGotMException(LispSymbol.fromString(name), expected, parameters.Count);
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
        private readonly Environment outerEnvironment;
        private readonly LispSymbol[] parameterNames;
        private readonly List<LispObject> body;
        public override string ToString() { return "Lambda"; }
        public Lambda(Environment env, IEnumerable<LispObject> parameterNames, List<LispObject> body)
            : base("lambda")
        {
            outerEnvironment = env;
            this.parameterNames = parameterNames.Cast<LispSymbol>().ToArray();
            this.body = body;
        }

        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, parameterNames.Length);
            Environment inner = new Environment(outerEnvironment);
            for (int i = 0; i < parameterNames.Length; ++i) inner.Define(parameterNames[i], parameters[i]);
            LispObject ret = LispNil.GetInstance();
            foreach (var i in body) ret = i.Eval(inner);
            return ret;
        }
    }
}
