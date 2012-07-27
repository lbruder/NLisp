using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.lb.NLisp
{
    internal static class LispStandardFunctions
    {
        public static LispObject Length(LispObject obj)
        {
            if (obj is IEnumerable<LispObject>) return LispObject.FromClrObject(((IEnumerable<LispObject>)obj).Count());
            throw new LispInvalidOperationException(obj, "length");
        }

        public static LispObject Reverse(LispObject list)
        {
            if (list is LispString)
            {
                string s = ((LispString)list).Value;
                var sb = new StringBuilder(s.Length);
                for (int i = s.Length - 1; i >= 0; --i) sb.Append(s[i]);
                return LispObject.FromClrObject(sb.ToString());
            }

            LispObject ret = Nil.GetInstance();
            while (!list.NullP())
            {
                ret = ConsCell.Cons(list.Car(), ret);
                list = list.Cdr();
                if (list is Nil) break;
                if (!(list is ConsCell)) return ConsCell.Cons(list, ret);
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
            return Symbol.Gensym();
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
            p.Add(Nil.GetInstance());
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
            p.Add(Nil.GetInstance());
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
            p.Add(Nil.GetInstance());
            p.Add(Nil.GetInstance());
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
                    to = ((Number)parameters[0]).NumberAsInt;
                    break;
                case 2:
                    from = ((Number)parameters[0]).NumberAsInt;
                    to = ((Number)parameters[1]).NumberAsInt;
                    break;
                default:
                    from = ((Number)parameters[0]).NumberAsInt;
                    to = ((Number)parameters[1]).NumberAsInt;
                    step = ((Number)parameters[2]).NumberAsInt;
                    break;
            }

            var ret = new List<LispObject>();
            for (int i = from; i < to; i += step) ret.Add(new Number(i));
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
}
