using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace org.lb.NLisp
{
    // TODO:
    // - augment lambda with &rest parameters
    // - quasiquoting
    // - some basic(!) kind of while... loop. Or tagbody? Or TCO (prob Environments)?

    // - clr-methods
    // - clr-properties
    // - clr-get
    // - clr-set
    // - clr-new
    // - clr-call (".")

    // - eval
    // - port operations
    // - read (from string, port)

    // Prelude: Lisp script to read and execute on startup -> flesh out the language in Lisp, keep the C# code under 1000 lines
    // - let over lambda
    // - equal
    // - append
    // - push
    // - <=
    // - >=
    // - and
    // - or

    #region Exceptions

    public sealed class LispConstantCanNotBeChangedException : Exception
    {
        internal LispConstantCanNotBeChangedException(LispSymbol sym)
            : base(sym + " is a constant and can not be changed") // TODO: I18N
        {
        }
    }

    public sealed class LispCannotEvaluateEmptyListException : Exception
    {
        internal LispCannotEvaluateEmptyListException()
            : base("Can not evaluate empty list") // TODO: I18N
        {
        }
    }

    public sealed class LispObjectCouldNotBeConvertedException : Exception
    {
        internal LispObjectCouldNotBeConvertedException(object obj)
            : base("CLR object of type " + obj.GetType() + " could not be converted to LispObject") // TODO: I18N
        {
        }
    }

    public sealed class LispObjectIsNotAListException : Exception
    {
        internal LispObjectIsNotAListException(object obj)
            : base("The value " + obj + " is not a list") // TODO: I18N
        {
        }
    }

    public sealed class LispInvalidOperationException : Exception
    {
        internal LispInvalidOperationException(LispObject o1, string op)
            : base("Invalid operation: (" + op + " " + o1 + ")") // TODO: I18N
        {
        }
        internal LispInvalidOperationException(LispObject o1, LispObject o2, string op)
            : base("Invalid operation: (" + op + " " + o1 + " " + o2 + ")") // TODO: I18N
        {
        }
    }

    public sealed class LispDivisionByZeroException : Exception
    {
        internal LispDivisionByZeroException()
            : base("Division by zero") // TODO: I18N
        {
        }
    }

    public sealed class LispUnexpectedEndOfStreamException : Exception
    {
        internal LispUnexpectedEndOfStreamException()
            : base("Unexpected end of stream") // TODO: I18N
        {
        }
    }

    public sealed class LispSymbolNotFoundException : Exception
    {
        internal LispSymbolNotFoundException(LispSymbol symbol)
            : base("Undefined symbol " + symbol) // TODO: I18N
        {
        }
    }

    public sealed class LispExpectedNParametersGotMException : Exception
    {
        internal LispExpectedNParametersGotMException(LispObject symbol, int expected, int got)
            : base(symbol + ": Expected " + expected + " parameter(s), got " + got) // TODO: I18N
        {
        }
    }

    public sealed class LispExpectedAtLeastNParametersGotMException : Exception
    {
        internal LispExpectedAtLeastNParametersGotMException(LispObject symbol, int expected, int got)
            : base(symbol + ": Expected at least " + expected + " parameter(s), got " + got) // TODO: I18N
        {
        }
    }

    public sealed class LispSymbolExpectedException : Exception
    {
        internal LispSymbolExpectedException(LispObject got)
            : base("Expected symbol, got " + got) // TODO: I18N
        {
        }
    }

    public sealed class LispListExpectedException : Exception
    {
        internal LispListExpectedException(LispObject got)
            : base("Expected list, got " + got) // TODO: I18N
        {
        }
    }

    public sealed class LispUndefinedFunctionException : Exception
    {
        internal LispUndefinedFunctionException(LispObject got)
            : base("Undefined function " + got) // TODO: I18N
        {
        }
    }

    #endregion

    #region Lisp Objects

    internal abstract class LispObject
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
        public virtual LispObject Ge(LispObject other) { throw new LispInvalidOperationException(this, other, ">="); }
        public virtual LispObject Le(LispObject other) { throw new LispInvalidOperationException(this, other, "<="); }

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
        public override LispObject Le(LispObject other) { return FromClrObject(number <= OtherNumber(other, "<=")); }
        public override LispObject Ge(LispObject other) { return FromClrObject(number >= OtherNumber(other, ">=")); }
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
        public override LispObject Le(LispObject other) { return FromClrObject(String.CompareOrdinal(value, OtherString(other, "<=")) <= 0); }
        public override LispObject Ge(LispObject other) { return FromClrObject(String.CompareOrdinal(value, OtherString(other, ">=")) >= 0); }
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

    internal sealed class LispSymbol : LispObject
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

    #endregion

    #region Reader

    internal sealed class Reader
    {
        private static readonly LispSymbol defmacroSymbol = LispSymbol.fromString("defmacro");
        private static readonly LispSymbol quoteSymbol = LispSymbol.fromString("quote");
        private readonly HashSet<LispSymbol> macros = new HashSet<LispSymbol>();
        private readonly Lisp lisp;
        private TextReader reader;

        private enum Mode
        {
            normal,
            quoting,
            //TODO: quasiquoting
        }

        public Reader(Lisp lisp)
        {
            this.lisp = lisp;
        }

        private char Peek()
        {
            int p = reader.Peek();
            if (p == -1) throw new LispUnexpectedEndOfStreamException();
            return (char)p;
        }

        public LispObject Read(TextReader rd)
        {
            reader = rd;
            return Read(Mode.normal);
        }

        private LispObject Read(Mode mode)
        {
            SkipWhitespace();
            char c = Peek();
            if (c == '\'')
            {
                reader.Read();
                return LispConsCell.Cons(quoteSymbol, LispConsCell.Cons(Read(Mode.quoting), LispNil.GetInstance()));
            }
            if (c == '(') return ReadCons(mode);
            if (c == '"') return ReadString();
            if (char.IsDigit(c)) return ReadNumber();
            return ReadSymbol();
        }

        private void SkipWhitespace()
        {
            while (Char.IsWhiteSpace(Peek())) reader.Read();
        }

        private LispObject ReadCons(Mode mode)
        {
            var ret = new List<LispObject>();
            reader.Read(); // Opening parenthesis
            SkipWhitespace();
            while (Peek() != ')')
            {
                ret.Add(Read(mode));
                SkipWhitespace();
            }
            reader.Read(); // Closing parenthesis
            return EvalMacros(mode, ret);
        }

        private LispObject EvalMacros(Mode mode, List<LispObject> list)
        {
            if (mode == Mode.normal && list.Count > 0 && list[0] is LispSymbol)
            {
                LispSymbol symbol = (LispSymbol)list[0];
                if (defmacroSymbol.Equals(symbol))
                {
                    list[0] = LispSymbol.fromString("defun");
                    macros.Add((LispSymbol)list[1]);
                    lisp.Eval(list);
                    return LispT.GetInstance();
                }
                if (macros.Contains(symbol))
                {
                    // Quote all parameters to prevent premature evaluation
                    for (int i = 1; i < list.Count; ++i)
                        list[i] = LispConsCell.Cons(quoteSymbol, LispConsCell.Cons(list[i], LispNil.GetInstance()));
                    return lisp.Eval(list);
                }
            }
            return LispObject.FromClrObject(list);
        }

        private LispObject ReadString()
        {
            StringBuilder ret = new StringBuilder();
            reader.Read(); // Opening quote
            while (Peek() != '"')
            {
                char c = (char)reader.Read();
                if (c == '\\')
                {
                    c = (char)reader.Read();
                    if (c == 'n') c = '\n';
                    if (c == 'r') c = '\r';
                    if (c == 't') c = '\t';
                }
                ret.Append(c);
            }
            reader.Read(); // Closing quote
            return new LispString(ret.ToString());
        }

        private LispObject ReadNumber()
        {
            var value = new StringBuilder();
            while (reader.Peek() != -1 && (Char.IsNumber(Peek()) || Peek() == '.')) value.Append((char)reader.Read());
            return new LispNumber(double.Parse(value.ToString(), CultureInfo.InvariantCulture));
        }

        private LispObject ReadSymbol()
        {
            var value = new StringBuilder();
            while (reader.Peek() != -1 && (Peek() != ')' && !Char.IsWhiteSpace(Peek()))) value.Append((char)reader.Read());
            return LispSymbol.fromString(value.ToString());
        }
    }

    #endregion

    #region Environment

    internal sealed class Environment
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

    #endregion

    #region Standard functions

    internal static class LispStandardFunctions
    {
        public static LispObject Length(LispObject obj)
        {
            if (obj is IEnumerable<LispObject>) return LispObject.FromClrObject(((IEnumerable<LispObject>)obj).Count());
            throw new LispInvalidOperationException(obj, "length");
        }

        public static LispObject Reverse(LispObject list)
        {
            if (list is LispString) return LispObject.FromClrObject(string.Join("", ((LispString)list).Value.Reverse().ToArray()));

            LispObject ret = LispNil.GetInstance();
            while (!list.NullP())
            {
                ret = LispConsCell.Cons(list.Car(), ret);
                list = list.Cdr();
                if (list is LispNil) break;
                if (!(list is LispConsCell)) return LispConsCell.Cons(list, ret);
            }
            return ret;
        }
    }

    internal sealed class BuiltinGensymFunction : BuiltinLispFunction
    {
        public BuiltinGensymFunction() : base("gensym") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 0);
            return LispSymbol.Gensym();
        }
    }

    internal sealed class BuiltinListFunction : BuiltinLispFunction
    {
        public BuiltinListFunction() : base("list") { }
        public override LispObject Call(List<LispObject> parameters) { return FromClrObject(parameters); }
    }

    internal sealed class BuiltinMapFunction : BuiltinLispFunction // TODO: Strings
    {
        public BuiltinMapFunction() : base("map") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            var ret = new List<LispObject>();
            LispFunction f = (LispFunction)parameters[0];
            var p = new List<LispObject>();
            p.Add(LispNil.GetInstance());
            foreach (var i in (IEnumerable<LispObject>)parameters[1])
            {
                p[0] = i;
                ret.Add(f.Call(p));
            }
            return FromClrObject(ret);
        }
    }

    internal sealed class BuiltinFilterFunction : BuiltinLispFunction // TODO: Strings
    {
        public BuiltinFilterFunction() : base("filter") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            var ret = new List<LispObject>();
            LispFunction f = (LispFunction)parameters[0];
            var p = new List<LispObject>();
            p.Add(LispNil.GetInstance());
            foreach (var i in (IEnumerable<LispObject>)parameters[1])
            {
                p[0] = i;
                if (f.Call(p).IsTrue()) ret.Add(i);
            }
            return FromClrObject(ret);
        }
    }

    internal sealed class BuiltinReduceFunction : BuiltinLispFunction
    {
        public BuiltinReduceFunction() : base("reduce") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            LispFunction f = (LispFunction)parameters[0];
            var p = new List<LispObject>();
            p.Add(LispNil.GetInstance());
            p.Add(LispNil.GetInstance());
            var values = (IEnumerable<LispObject>)parameters[1];
            var acc = values.First();
            foreach (var i in values.Skip(1))
            {
                p[0] = acc;
                p[1] = i;
                acc = f.Call(p);
            }
            return FromClrObject(acc);
        }
    }

    internal sealed class BuiltinAllFunction : BuiltinLispFunction
    {
        public BuiltinAllFunction() : base("all") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            LispFunction f = (LispFunction)parameters[0];
            var p = new List<LispObject>();
            p.Add(LispNil.GetInstance());
            foreach (var i in (IEnumerable<LispObject>)parameters[1])
            {
                p[0] = i;
                if (f.Call(p).IsTrue()) continue;
                return LispNil.GetInstance();
            }
            return LispT.GetInstance();
        }
    }

    internal sealed class BuiltinAnyFunction : BuiltinLispFunction
    {
        public BuiltinAnyFunction() : base("any") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            LispFunction f = (LispFunction)parameters[0];
            var p = new List<LispObject>();
            p.Add(LispNil.GetInstance());
            foreach (var i in (IEnumerable<LispObject>)parameters[1])
            {
                p[0] = i;
                if (!f.Call(p).IsTrue()) continue;
                return LispT.GetInstance();
            }
            return LispNil.GetInstance();
        }
    }

    internal sealed class BuiltinRangeFunction : BuiltinLispFunction
    {
        public BuiltinRangeFunction() : base("range") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            int from = 0;
            int to;
            int step = 1;
            AssertParameterCountAtLeast(parameters, 1);
            switch (parameters.Count)
            {
                case 1:
                    to = ((LispNumber)parameters[0]).NumberAsInt;
                    break;
                case 2:
                    from = ((LispNumber)parameters[0]).NumberAsInt;
                    to = ((LispNumber)parameters[1]).NumberAsInt;
                    break;
                default:
                    from = ((LispNumber)parameters[0]).NumberAsInt;
                    to = ((LispNumber)parameters[1]).NumberAsInt;
                    step = ((LispNumber)parameters[2]).NumberAsInt;
                    break;
            }

            var ret = new List<LispObject>();
            for (int i = from; i < to; i += step) ret.Add(new LispNumber(i));
            return FromClrObject(ret);
        }
    }

    internal sealed class BuiltinUnaryOperationFunction : BuiltinLispFunction
    {
        private readonly Func<LispObject, LispObject> op;
        public BuiltinUnaryOperationFunction(string name, Func<LispObject, LispObject> op) : base(name) { this.op = op; }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 1);
            return op(parameters[0]);
        }
    }

    internal sealed class BuiltinBinaryOperationFunction : BuiltinLispFunction
    {
        private readonly Func<LispObject, LispObject, LispObject> op;
        public BuiltinBinaryOperationFunction(string name, Func<LispObject, LispObject, LispObject> op) : base(name) { this.op = op; }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            return op(parameters[0], parameters[1]);
        }
    }

    #endregion

    #region User Interface

    internal sealed class Lisp
    {
        private readonly Environment global = new Environment();
        private readonly Reader reader;

        public event Action<string> Print = delegate { };

        public Lisp()
        {
            reader = new Reader(this);

            SetVariable("list", new BuiltinListFunction());
            SetVariable("map", new BuiltinMapFunction());
            SetVariable("filter", new BuiltinFilterFunction());
            SetVariable("reduce", new BuiltinReduceFunction());
            SetVariable("all", new BuiltinAllFunction());
            SetVariable("any", new BuiltinAnyFunction());
            SetVariable("range", new BuiltinRangeFunction());
            SetVariable("gensym", new BuiltinGensymFunction());

            AddUnaryFunction("car", obj => obj.Car());
            AddUnaryFunction("cdr", obj => obj.Cdr());
            AddUnaryFunction("caar", obj => obj.Car().Car());
            AddUnaryFunction("cadr", obj => obj.Cdr().Car());
            AddUnaryFunction("cdar", obj => obj.Car().Cdr());
            AddUnaryFunction("cddr", obj => obj.Cdr().Cdr());
            AddUnaryFunction("not", obj => LispObject.FromClrObject(!obj.IsTrue()));
            AddUnaryFunction("nullp", obj => LispObject.FromClrObject(obj.NullP()));
            AddUnaryFunction("consp", obj => LispObject.FromClrObject(obj is LispConsCell));
            AddUnaryFunction("symbolp", obj => LispObject.FromClrObject(obj is LispSymbol));
            AddUnaryFunction("length", LispStandardFunctions.Length);
            AddUnaryFunction("reverse", LispStandardFunctions.Reverse);
            AddUnaryFunction("print", obj => { Print(obj.ToString()); return obj; });

            AddBinaryFunction("eq", (o1, o2) => LispObject.FromClrObject((o1 == o2) || (o1 is LispNumber && o1.Equals(o2))));
            AddBinaryFunction("cons", LispConsCell.Cons);
            AddBinaryFunction("+", (o1, o2) => o1.Add(o2));
            AddBinaryFunction("-", (o1, o2) => o1.Sub(o2));
            AddBinaryFunction("*", (o1, o2) => o1.Mul(o2));
            AddBinaryFunction("/", (o1, o2) => o1.Div(o2));
            AddBinaryFunction("mod", (o1, o2) => o1.Mod(o2));
            AddBinaryFunction("=", (o1, o2) => o1.NumEq(o2));
            AddBinaryFunction("<", (o1, o2) => o1.Lt(o2));
            AddBinaryFunction(">", (o1, o2) => o1.Gt(o2));
            AddBinaryFunction("<=", (o1, o2) => o1.Le(o2));
            AddBinaryFunction(">=", (o1, o2) => o1.Ge(o2));
        }

        public LispObject Evaluate(string script)
        {
            LispObject ret = LispNil.GetInstance();
            var stream = new StringReader(script);
            SkipWhitespaceInStream(stream);
            while (stream.Peek() != -1)
            {
                ret = reader.Read(stream).Eval(global);
                SkipWhitespaceInStream(stream);
            }
            return ret;
        }

        public void SetVariable(string identifier, object value) { global.Define(LispSymbol.fromString(identifier), LispObject.FromClrObject(value)); }
        public void AddFunction(string identifier, Delegate f) { SetVariable(identifier, f); }

        internal LispObject Eval(List<LispObject> ast) { return LispObject.FromClrObject(ast).Eval(global); }

        private void AddUnaryFunction(string name, Func<LispObject, LispObject> op) { SetVariable(name, new BuiltinUnaryOperationFunction(name, op)); }
        private void AddBinaryFunction(string name, Func<LispObject, LispObject, LispObject> op) { SetVariable(name, new BuiltinBinaryOperationFunction(name, op)); }
        private static void SkipWhitespaceInStream(TextReader stream) { while (char.IsWhiteSpace((char)stream.Peek())) stream.Read(); }
    }

    #endregion
}
