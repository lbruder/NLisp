using System;
using System.Collections.Generic;
using System.IO;

namespace org.lb.NLisp
{
    // TODO:
    // - substring
    // - augment lambda with &rest parameters
    // - quasiquoting
    // - clr FFI

    // - port operations (strings, files, sockets)
    // - apply, eval, read
    // - thread, join, semaphore, sem-p, sem-v

    public sealed class NLisp
    {
        private readonly Environment global = new Environment();
        private readonly Reader reader;

        public event Action<string> Print = delegate { };

        public NLisp()
        {
            reader = new Reader(this);

            SetVariable("list", new BuiltinListFunction());
            SetVariable("map", new BuiltinMapFunction());
            SetVariable("filter", new BuiltinFilterFunction());
            SetVariable("reduce", new BuiltinReduceFunction());
            SetVariable("range", new BuiltinRangeFunction());
            SetVariable("gensym", new BuiltinGensymFunction());
            SetVariable("substring", new BuiltinSubstringFunction());

            AddUnaryFunction("car", obj => obj.Car());
            AddUnaryFunction("cdr", obj => obj.Cdr());
            AddUnaryFunction("nullp", obj => LispObject.FromClrObject(obj.NullP()));
            AddUnaryFunction("consp", obj => LispObject.FromClrObject(obj is ConsCell));
            AddUnaryFunction("symbolp", obj => LispObject.FromClrObject(obj is Symbol));
            AddUnaryFunction("numberp", obj => LispObject.FromClrObject(obj is Number));
            AddUnaryFunction("stringp", obj => LispObject.FromClrObject(obj is LispString));
            AddUnaryFunction("length", LispStandardFunctions.Length);
            AddUnaryFunction("reverse", LispStandardFunctions.Reverse);
            AddUnaryFunction("print", obj => { Print(obj.ToString()); return obj; });

            AddBinaryFunction("eq", (o1, o2) => LispObject.FromClrObject((o1 == o2) || (o1 is Number && o1.Equals(o2))));
            AddBinaryFunction("cons", ConsCell.Cons);
            AddBinaryFunction("+", (o1, o2) => o1.Add(o2));
            AddBinaryFunction("-", (o1, o2) => o1.Sub(o2));
            AddBinaryFunction("*", (o1, o2) => o1.Mul(o2));
            AddBinaryFunction("/", (o1, o2) => o1.Div(o2));
            AddBinaryFunction("mod", (o1, o2) => o1.Mod(o2));
            AddBinaryFunction("=", (o1, o2) => o1.NumEq(o2));
            AddBinaryFunction("<", (o1, o2) => o1.Lt(o2));
            AddBinaryFunction(">", (o1, o2) => o1.Gt(o2));

            if (File.Exists("Init.lsp")) Evaluate(File.ReadAllText("Init.lsp"));
        }

        public LispObject Evaluate(string script)
        {
            LispObject ret = Nil.GetInstance();
            var stream = new StringReader(script);
            SkipWhitespaceInStream(stream);
            while (stream.Peek() != -1)
            {
                ret = reader.Read(stream).Eval(global);
                SkipWhitespaceInStream(stream);
            }
            return ret;
        }

        public void SetVariable(string identifier, object value) { global.Define(Symbol.fromString(identifier), LispObject.FromClrObject(value)); }
        public void AddFunction(string identifier, Delegate f) { SetVariable(identifier, f); }

        internal LispObject Eval(List<LispObject> ast) { return LispObject.FromClrObject(ast).Eval(global); }

        private void AddUnaryFunction(string name, Func<LispObject, LispObject> op) { SetVariable(name, new BuiltinUnaryOperationFunction(name, op)); }
        private void AddBinaryFunction(string name, Func<LispObject, LispObject, LispObject> op) { SetVariable(name, new BuiltinBinaryOperationFunction(name, op)); }
        private static void SkipWhitespaceInStream(TextReader stream) { while (char.IsWhiteSpace((char)stream.Peek())) stream.Read(); }
    }
}
