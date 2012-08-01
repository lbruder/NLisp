using System.Collections.Generic;
using System.Text;

namespace org.lb.NLisp
{
    internal sealed class BuiltinStringFunction : BuiltinLispFunction
    {
        public BuiltinStringFunction() : base("string") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCountAtLeast(parameters, 1);
            var sb = new StringBuilder();
            foreach (var i in parameters) sb.Append(i.ToString());
            return new LispString(sb.ToString());
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
