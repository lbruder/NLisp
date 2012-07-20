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

    // defun (+ &rest parameters)
    // defmacro
    // gensym

    //not
    //listp
    //symbolp
    //nullp
    //alert
    //equalp
    //length
    //append
    //reduce
    //any?
    //push
    //range

    //clr-methods, clr-properties, clr-get, clr-set, clr-new, clr-call (".")
    //eval
    //port operations
    //read (from string, port)

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
        public abstract bool IsTrue();
        public virtual bool NullP() { return false; }
        public virtual LispObject Eval(Environment env) { return LispNil.GetInstance(); }
        public virtual LispObject Car() { throw new LispObjectIsNotAListException(this); }
        public virtual LispObject Cdr() { throw new LispObjectIsNotAListException(this); }

        public static LispObject FromClrObject(object source)
        {
            if (source == null) return LispNil.GetInstance();
            if (source is LispObject) return (LispObject)source;
            if (source is bool) return ((bool)source) ? (LispObject)LispT.GetInstance() : LispNil.GetInstance();
            if (source is byte) return new LispNumber((byte)source);
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
                for (int i = list.Count - 1; i >= 0; --i) ret = new LispConsCell(FromClrObject(list[i]), ret);
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
        public override string ToString() { return "()"; }
        public override bool Equals(object obj) { return obj is LispNil; }
        public override int GetHashCode() { return 4711; }
    }

    internal sealed class LispT : LispObject
    {
        private static readonly LispT instance = new LispT();
        private LispT() { }
        public static LispT GetInstance() { return instance; }
        public override bool IsTrue() { return true; }
        public override LispObject Eval(Environment env) { return this; }
        public override string ToString() { return "t"; }
        public override bool Equals(object obj) { return obj is LispT; }
        public override int GetHashCode() { return 0815; }
    }

    internal sealed class LispNumber : LispObject
    {
        private readonly double number;
        public LispNumber(double number) { this.number = number; }
        public double Number { get { return number; } }
        public override bool IsTrue() { return true; }
        public override LispObject Eval(Environment env) { return this; }
        public override string ToString() { return number.ToString(CultureInfo.InvariantCulture); }
        public override bool Equals(object obj) { return (obj is LispNumber) && ((LispNumber)obj).number == number; }
        public override int GetHashCode() { return number.GetHashCode(); }
    }

    internal sealed class LispString : LispObject
    {
        private readonly string value;
        public LispString(string value) { this.value = value; }
        public override bool IsTrue() { return true; }
        public override LispObject Eval(Environment env) { return this; }
        public override string ToString() { return '"' + value + '"'; } // TODO: Escapes
        public override bool Equals(object obj) { return (obj is LispString) && ((LispString)obj).value.Equals(value); }
        public override int GetHashCode() { return value.GetHashCode(); }
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
    }

    internal sealed class LispConsCell : LispObject, IEnumerable<LispObject>
    {
        private static readonly LispSymbol quoteSym = LispSymbol.fromString("quote");
        private static readonly LispSymbol ifSym = LispSymbol.fromString("if");
        private static readonly LispSymbol prognSym = LispSymbol.fromString("progn");
        private static readonly LispSymbol defineSym = LispSymbol.fromString("define");
        private static readonly LispSymbol setSym = LispSymbol.fromString("setf");
        private static readonly LispSymbol lambdaSym = LispSymbol.fromString("lambda");

        private readonly LispObject car;
        private readonly LispObject cdr;
        public LispConsCell(LispObject car, LispObject cdr) { this.car = car; this.cdr = cdr; }
        public override LispObject Car() { return car; }
        public override LispObject Cdr() { return cdr; }
        public override bool IsTrue() { return true; }

        public override LispObject Eval(Environment env)
        {
            if (quoteSym.Equals(car)) return ((LispConsCell)cdr).car;
            if (ifSym.Equals(car))
            {
                var asArray = this.ToArray();
                if (asArray.Length != 4) throw new LispExpectedNParametersGotMException(car, 3, asArray.Length - 1);
                return asArray[1].Eval(env).IsTrue() ? asArray[2].Eval(env) : asArray[3].Eval(env);
            }
            if (prognSym.Equals(car))
            {
                var asArray = this.ToArray();
                if (asArray.Length == 1) return LispNil.GetInstance();
                for (int i = 1; i < asArray.Length - 1; ++i) asArray[i].Eval(env);
                return asArray[asArray.Length - 1].Eval(env);
            }
            if (defineSym.Equals(car))
            {
                var asArray = this.ToArray();
                if (asArray.Length != 3) throw new LispExpectedNParametersGotMException(car, 2, asArray.Length - 1);
                if (asArray[1] is LispSymbol) return env.Define((LispSymbol)asArray[1], asArray[2].Eval(env));
                throw new LispSymbolExpectedException(asArray[1]);
            }
            if (setSym.Equals(car))
            {
                var asArray = this.ToArray();
                if (asArray.Length != 3) throw new LispExpectedNParametersGotMException(car, 2, asArray.Length - 1);
                if (asArray[1] is LispSymbol) return env.Set((LispSymbol)asArray[1], asArray[2].Eval(env));
                throw new LispSymbolExpectedException(asArray[1]);
            }
            if (lambdaSym.Equals(car))
            {
                var asArray = this.ToArray();
                if (asArray.Length < 2) throw new LispExpectedAtLeastNParametersGotMException(car, 1, asArray.Length - 1);
                if (asArray[1] is LispConsCell) return new Lambda(env, (LispConsCell)asArray[1], this.Skip(2).ToList());
                throw new LispListExpectedException(asArray[1]);
            }

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
        public override bool IsTrue() { return true; }
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
        private readonly TextReader reader;

        public Reader(TextReader reader)
        {
            this.reader = reader;
        }

        private char Peek()
        {
            int p = reader.Peek();
            if (p == -1) throw new LispUnexpectedEndOfStreamException();
            return (char)p;
        }

        public LispObject Read()
        {
            SkipWhitespace();
            char c = Peek();
            if (c == '\'')
            {
                reader.Read();
                return new LispConsCell(LispSymbol.fromString("quote"), new LispConsCell(Read(), LispNil.GetInstance()));
            }
            if (c == '(') return ReadCons();
            if (c == '"') return ReadString();
            if (char.IsDigit(c)) return ReadNumber();
            return ReadSymbol();
        }

        private void SkipWhitespace()
        {
            while (Char.IsWhiteSpace(Peek())) reader.Read();
        }

        private LispObject ReadCons()
        {
            var ret = new List<LispObject>();
            reader.Read(); // Opening parenthesis
            SkipWhitespace();
            while (Peek() != ')')
            {
                ret.Add(Read());
                SkipWhitespace();
            }
            reader.Read(); // Closing parenthesis
            return LispObject.FromClrObject(ret);
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
        public static LispObject Reverse(LispObject list)
        {
            LispObject ret = LispNil.GetInstance();
            while (!list.NullP())
            {
                ret = new LispConsCell(list.Car(), ret);
                list = list.Cdr();
                if (list is LispNil) break;
                if (!(list is LispConsCell)) return new LispConsCell(list, ret);
            }
            return ret;
        }
    }

    internal sealed class BuiltinCarFunction : BuiltinLispFunction
    {
        public BuiltinCarFunction() : base("car") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 1);
            return parameters[0].Car();
        }
    }

    internal sealed class BuiltinCdrFunction : BuiltinLispFunction
    {
        public BuiltinCdrFunction() : base("cdr") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 1);
            return parameters[0].Cdr();
        }
    }

    internal sealed class BuiltinConsFunction : BuiltinLispFunction
    {
        public BuiltinConsFunction() : base("cons") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            return new LispConsCell(parameters[0], parameters[1]);
        }
    }

    internal sealed class BuiltinListFunction : BuiltinLispFunction
    {
        public BuiltinListFunction() : base("list") { }
        public override LispObject Call(List<LispObject> parameters) { return FromClrObject(parameters); }
    }

    internal sealed class BuiltinReverseFunction : BuiltinLispFunction
    {
        public BuiltinReverseFunction() : base("reverse") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 1);
            return LispStandardFunctions.Reverse(parameters[0]);
        }
    }

    internal sealed class BuiltinPrintFunction : BuiltinLispFunction
    {
        private readonly Lisp interp;
        public BuiltinPrintFunction(Lisp interp) : base("print") { this.interp = interp; }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 1);
            interp.print(parameters[0]);
            return parameters[0];
        }
    }

    internal sealed class BuiltinMapFunction : BuiltinLispFunction
    {
        public BuiltinMapFunction() : base("map") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            var ret = new List<LispObject>();
            LispFunction f = (LispFunction)parameters[0];
            var p = new List<LispObject>();
            p.Add(LispNil.GetInstance());
            foreach (var i in (LispConsCell)parameters[1])
            {
                p[0] = i;
                ret.Add(f.Call(p));
            }
            return FromClrObject(ret);
        }
    }

    internal sealed class BuiltinFilterFunction : BuiltinLispFunction
    {
        public BuiltinFilterFunction() : base("filter") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            var ret = new List<LispObject>();
            LispFunction f = (LispFunction)parameters[0];
            var p = new List<LispObject>();
            p.Add(LispNil.GetInstance());
            foreach (var i in (LispConsCell)parameters[1])
            {
                p[0] = i;
                if (f.Call(p).IsTrue()) ret.Add(i);
            }
            return FromClrObject(ret);
        }
    }

    internal sealed class BuiltinAllFunction : BuiltinLispFunction
    {
        public BuiltinAllFunction() : base("allp") { } // TODO: Name?
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            LispFunction f = (LispFunction)parameters[0];
            var p = new List<LispObject>();
            p.Add(LispNil.GetInstance());
            foreach (var i in (LispConsCell)parameters[1])
            {
                p[0] = i;
                if (f.Call(p).IsTrue()) continue;
                return LispNil.GetInstance();
            }
            return LispT.GetInstance();
        }
    }

    internal sealed class BuiltinRangeFunction : BuiltinLispFunction
    {
        public BuiltinRangeFunction() : base("range") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            int start = 0;
            int count;
            int step = 1;
            AssertParameterCountAtLeast(parameters, 1);
            switch (parameters.Count)
            {
                case 1:
                    count = (int)((LispNumber)parameters[0]).Number;
                    break;
                case 2:
                    start = (int)((LispNumber)parameters[0]).Number;
                    count = (int)((LispNumber)parameters[1]).Number;
                    break;
                default:
                    start = (int)((LispNumber)parameters[0]).Number;
                    count = (int)((LispNumber)parameters[1]).Number;
                    step = (int)((LispNumber)parameters[2]).Number;
                    break;
            }

            var ret = new List<LispObject>();
            int value = start;
            for (int i = 0; i < count; i++)
            {
                ret.Add(FromClrObject(value));
                value += step;
            }
            return FromClrObject(ret);
        }
    }

    internal abstract class BuiltinBinaryOperationFunction : BuiltinLispFunction
    {
        protected BuiltinBinaryOperationFunction(string name) : base(name) { }
        protected abstract LispObject PerformOperation(LispObject o1, LispObject o2);
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            return PerformOperation(parameters[0], parameters[1]);
        }
    }

    internal sealed class BuiltinAdditionFunction : BuiltinBinaryOperationFunction
    {
        public BuiltinAdditionFunction() : base("+") { }
        protected override LispObject PerformOperation(LispObject o1, LispObject o2) { return FromClrObject(((LispNumber)o1).Number + ((LispNumber)o2).Number); }
    }

    internal sealed class BuiltinSubtractionFunction : BuiltinBinaryOperationFunction
    {
        public BuiltinSubtractionFunction() : base("-") { }
        protected override LispObject PerformOperation(LispObject o1, LispObject o2) { return FromClrObject(((LispNumber)o1).Number - ((LispNumber)o2).Number); }
    }

    internal sealed class BuiltinMultiplicationFunction : BuiltinBinaryOperationFunction
    {
        public BuiltinMultiplicationFunction() : base("*") { }
        protected override LispObject PerformOperation(LispObject o1, LispObject o2) { return FromClrObject(((LispNumber)o1).Number * ((LispNumber)o2).Number); }
    }

    internal sealed class BuiltinDivisionFunction : BuiltinBinaryOperationFunction
    {
        public BuiltinDivisionFunction() : base("/") { }
        protected override LispObject PerformOperation(LispObject o1, LispObject o2) { return FromClrObject(((LispNumber)o1).Number / ((LispNumber)o2).Number); }
    }

    internal sealed class BuiltinRemainderFunction : BuiltinBinaryOperationFunction
    {
        public BuiltinRemainderFunction() : base("rem") { }
        protected override LispObject PerformOperation(LispObject o1, LispObject o2) { return FromClrObject(((LispNumber)o1).Number % ((LispNumber)o2).Number); }
    }

    internal sealed class BuiltinNumericalEqualFunction : BuiltinBinaryOperationFunction
    {
        public BuiltinNumericalEqualFunction() : base("=") { }
        protected override LispObject PerformOperation(LispObject o1, LispObject o2) { return FromClrObject(((LispNumber)o1).Number == ((LispNumber)o2).Number); }
    }

    internal sealed class BuiltinNumericalGreaterFunction : BuiltinBinaryOperationFunction
    {
        public BuiltinNumericalGreaterFunction() : base(">") { }
        protected override LispObject PerformOperation(LispObject o1, LispObject o2) { return FromClrObject(((LispNumber)o1).Number > ((LispNumber)o2).Number); }
    }

    internal sealed class BuiltinNumericalLesserFunction : BuiltinBinaryOperationFunction
    {
        public BuiltinNumericalLesserFunction() : base("<") { }
        protected override LispObject PerformOperation(LispObject o1, LispObject o2) { return FromClrObject(((LispNumber)o1).Number < ((LispNumber)o2).Number); }
    }

    internal sealed class BuiltinNumericalGEFunction : BuiltinBinaryOperationFunction
    {
        public BuiltinNumericalGEFunction() : base(">=") { }
        protected override LispObject PerformOperation(LispObject o1, LispObject o2) { return FromClrObject(((LispNumber)o1).Number >= ((LispNumber)o2).Number); }
    }

    internal sealed class BuiltinNumericalLEFunction : BuiltinBinaryOperationFunction
    {
        public BuiltinNumericalLEFunction() : base("<=") { }
        protected override LispObject PerformOperation(LispObject o1, LispObject o2) { return FromClrObject(((LispNumber)o1).Number <= ((LispNumber)o2).Number); }
    }

    #endregion

    #region Interface

    public sealed class Lisp
    {
        private readonly Environment global = new Environment();

        public event Action<string> Print = delegate { };

        public Lisp()
        {
            SetVariable("cons", new BuiltinConsFunction());
            SetVariable("car", new BuiltinCarFunction());
            SetVariable("cdr", new BuiltinCdrFunction());
            SetVariable("list", new BuiltinListFunction());
            SetVariable("reverse", new BuiltinReverseFunction());
            SetVariable("map", new BuiltinMapFunction());
            SetVariable("filter", new BuiltinFilterFunction());
            SetVariable("allp", new BuiltinAllFunction());
            SetVariable("range", new BuiltinRangeFunction());
            SetVariable("+", new BuiltinAdditionFunction());
            SetVariable("-", new BuiltinSubtractionFunction());
            SetVariable("*", new BuiltinMultiplicationFunction());
            SetVariable("/", new BuiltinDivisionFunction());
            SetVariable("rem", new BuiltinRemainderFunction());
            SetVariable("=", new BuiltinNumericalEqualFunction());
            SetVariable(">", new BuiltinNumericalGreaterFunction());
            SetVariable("<", new BuiltinNumericalLesserFunction());
            SetVariable(">=", new BuiltinNumericalGEFunction());
            SetVariable("<=", new BuiltinNumericalLEFunction());
            SetVariable("print", new BuiltinPrintFunction(this));
        }

        public object Evaluate(string expression) { return new Reader(new StringReader(expression)).Read().Eval(global); }
        public object EvaluateScript(string[] script) { return Evaluate("(progn " + string.Join("\n", script) + ")"); }
        public void SetVariable(string identifier, object value) { global.Define(LispSymbol.fromString(identifier), LispObject.FromClrObject(value)); }
        public void AddFunction(string identifier, Delegate f) { SetVariable(identifier, f); }
        public string ObjectToString(object value) { return value.ToString(); }
        internal void print(LispObject lispObject) { Print(lispObject.ToString()); }
    }

    #endregion
}
