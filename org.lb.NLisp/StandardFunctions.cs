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
}
