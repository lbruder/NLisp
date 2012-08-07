using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace org.lb.NLisp
{
    public sealed class NLisp
    {
        private readonly Environment global = new Environment();
        private readonly Reader reader;
        private static readonly Symbol quoteSym = Symbol.fromString("quote");
        private static readonly Random random = new Random();

        public event Action<string> Print = delegate { };

        public NLisp()
        {
            reader = new Reader(this);
            AddStandardSymbols();
            AddStandardFunctions();
            AddSystemFunctions();

            if (File.Exists("Init.lsp")) Evaluate(File.ReadAllText("Init.lsp"));
        }

        private void AddStandardSymbols()
        {
            SetVariable("t", T.GetInstance());
            SetVariable("nil", Nil.GetInstance());
            SetVariable("string", new BuiltinStringFunction());
            SetVariable("substring", new BuiltinSubstringFunction());
        }

        private void AddStandardFunctions()
        {
            AddNullaryFunction("gensym", Symbol.Gensym);

            AddUnaryFunction("car", obj => obj.Car());
            AddUnaryFunction("cdr", obj => obj.Cdr());
            AddUnaryFunction("nullp", obj => LispObject.FromClrObject(obj.NullP()));
            AddUnaryFunction("consp", obj => LispObject.FromClrObject(obj is ConsCell));
            AddUnaryFunction("symbolp", obj => LispObject.FromClrObject(obj is Symbol));
            AddUnaryFunction("numberp", obj => LispObject.FromClrObject(obj is Number));
            AddUnaryFunction("stringp", obj => LispObject.FromClrObject(obj is LispString));
            AddUnaryFunction("print", obj => { Print(obj.ToString()); return obj; });
            AddUnaryFunction("macroexpand-1", obj => { bool expandP; return Macroexpand1(obj, out expandP); });
            AddUnaryFunction("macroexpand", Macroexpand);
            AddUnaryFunction("eval", obj => obj.Eval(global));
            AddUnaryFunction("random", p => LispObject.FromClrObject(random.Next(((Number)p).NumberAsInt)));
            AddUnaryFunction("length", obj =>
            {
                if (obj.NullP()) return LispObject.FromClrObject(0);
                if (obj is IEnumerable<LispObject>) return LispObject.FromClrObject(((IEnumerable<LispObject>)obj).Count());
                throw new InvalidOperationException(obj, "length");
            });

            AddBinaryFunction("eq", (o1, o2) => LispObject.FromClrObject(o1 == o2)); // Numbers are not automatically considered eq!
            AddBinaryFunction("cons", ConsCell.Cons);
            AddBinaryFunction("+", (o1, o2) => o1.Add(o2));
            AddBinaryFunction("-", (o1, o2) => o1.Sub(o2));
            AddBinaryFunction("*", (o1, o2) => o1.Mul(o2));
            AddBinaryFunction("/", (o1, o2) => o1.Div(o2));
            AddBinaryFunction("mod", (o1, o2) => o1.Mod(o2));
            AddBinaryFunction("=", (o1, o2) => o1.NumEq(o2));
            AddBinaryFunction("<", (o1, o2) => o1.Lt(o2));
            AddBinaryFunction(">", (o1, o2) => o1.Gt(o2));
            AddBinaryFunction("apply", (f, list) => ((LispFunction)f).Call(list.NullP() ? new List<LispObject>() : ((ConsCell)list).ToList()));
        }

        private void AddSystemFunctions()
        {
            AddNullaryFunction("sys:get-global-symbols", () => LispObject.FromClrObject(global.GetSymbols()));
            AddNullaryFunction("sys:get-global-macros", () => LispObject.FromClrObject(global.GetMacros()));

            AddUnaryFunction("sys:make-symbol-constant", symbol => { global.MakeSymbolConstant((Symbol)symbol); return T.GetInstance(); });
            AddUnaryFunction("sys:make-macro-constant", symbol => { global.MakeMacroConstant((Symbol)symbol); return T.GetInstance(); });

            AddUnaryFunction("sys:open-file-for-input", filename => LispObject.FromClrObject(File.OpenRead(((LispString)filename).Value)));
            AddUnaryFunction("sys:open-file-for-output", filename => LispObject.FromClrObject(File.OpenWrite(((LispString)filename).Value)));

            AddBinaryFunction("sys:print", (obj, stream) =>
            {
                if (!(stream is LispStream)) throw new InvalidOperationException(obj, "print");
                ((LispStream)stream).GetWriteStream().Write(obj.ToString() + "\n");
                return obj;
            });

            AddUnaryFunction("sys:read", obj =>
            {
                if (!(obj is LispStream)) throw new InvalidOperationException(obj, "read");
                var stream = ((LispStream)obj).GetReadStream();
                SkipWhitespaceInStream(stream);
                return stream.Peek() == -1 ? Nil.GetInstance() : reader.Read(stream, false);
            });

            AddUnaryFunction("sys:read-line", obj =>
            {
                if (!(obj is LispStream)) throw new InvalidOperationException(obj, "read-line");
                var stream = ((LispStream)obj).GetReadStream();
                return stream.Peek() == -1 ? Nil.GetInstance() : LispObject.FromClrObject(stream.ReadLine());
            });

            AddUnaryFunction("sys:close", obj =>
            {
                if (!(obj is IDisposable)) throw new InvalidOperationException(obj, "close");
                ((IDisposable)obj).Dispose();
                return T.GetInstance();
            });
        }

        public LispObject Evaluate(string script)
        {
            LispObject ret = Nil.GetInstance();
            var stream = new StringReader(script);
            SkipWhitespaceInStream(stream);
            while (stream.Peek() != -1)
            {
                ret = reader.Read(stream, true).Eval(global);
                SkipWhitespaceInStream(stream);
            }
            return ret;
        }

        private void SetVariable(string identifier, object value) { global.Define(Symbol.fromString(identifier), LispObject.FromClrObject(value)); }
        // TODO later public void AddFunction(string identifier, Delegate f) { SetVariable(identifier, f); }

        internal LispObject Eval(List<LispObject> ast) { return LispObject.FromClrObject(ast).Eval(global); }
        internal void AddMacro(Symbol identifier, Lambda expansionFunction) { global.DefineMacro(identifier, expansionFunction); }

        private void AddNullaryFunction(string name, Func<LispObject> op) { SetVariable(name, new BuiltinNullaryFunction(name, op)); }
        private void AddUnaryFunction(string name, Func<LispObject, LispObject> op) { SetVariable(name, new BuiltinUnaryOperationFunction(name, op)); }
        private void AddBinaryFunction(string name, Func<LispObject, LispObject, LispObject> op) { SetVariable(name, new BuiltinBinaryOperationFunction(name, op)); }
        private static void SkipWhitespaceInStream(TextReader stream) { while (char.IsWhiteSpace((char)stream.Peek())) stream.Read(); }

        private LispObject Macroexpand1(LispObject objectToExpand, out bool expandP)
        {
            expandP = false;
            if (objectToExpand is ConsCell) return ExpandCons1((ConsCell)objectToExpand, out expandP);
            return objectToExpand;
        }

        private LispObject ExpandCons1(ConsCell cell, out bool expandP)
        {
            expandP = false;
            if (quoteSym.Equals(cell.Car())) return cell;

            var tmp = new List<LispObject>();

            foreach (var i in cell.Skip(1))
            {
                bool expanded;
                tmp.Add(Macroexpand1(i, out expanded));
                if (expanded) expandP = true;
            }

            Lambda macro = null;
            if (cell.Car() is Symbol) macro = global.GetMacro((Symbol)cell.Car());
            if (macro != null)
            {
                expandP = true;
                return macro.Call(tmp);
            }
            tmp.Insert(0, cell.Car());
            return LispObject.FromClrObject(tmp);
        }

        internal LispObject Macroexpand(LispObject objectToExpand)
        {
            bool expandP = true;
            LispObject ret = objectToExpand;
            while (expandP) ret = Macroexpand1(ret, out expandP);
            return ret;
        }
    }
}
