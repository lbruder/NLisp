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
            throw new InvalidOperationException(obj, "length");
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

    internal sealed class BuiltinSubstringFunction : BuiltinLispFunction
    {
        public BuiltinSubstringFunction() : base("substring") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCountAtLeast(parameters, 2);
            string str = ((LispString)parameters[0]).Value;
            int from = ((Number)parameters[1]).NumberAsInt;
            return FromClrObject(parameters.Count == 2
                ? str.Substring(from)
                : str.Substring(from, ((Number)parameters[2]).NumberAsInt - from));
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
