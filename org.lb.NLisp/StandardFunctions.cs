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
            foreach (var i in parameters) sb.Append(i is LispString ? ((LispString)i).Value : i.ToString());
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

    internal sealed class BuiltinMakeArrayFunction : BuiltinLispFunction
    {
        public BuiltinMakeArrayFunction() : base("make-array") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCountAtLeast(parameters, 1);
            int dimension = ((Number)parameters[0]).NumberAsInt;
            LispObject initialElement = (parameters.Count > 1) ? parameters[1] : Nil.GetInstance();
            return new Array(dimension, initialElement);
        }
    }

    internal sealed class BuiltinAsetFunction : BuiltinLispFunction
    {
        public BuiltinAsetFunction() : base("sys:aset") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 3);
            Array array = (Array)parameters[0];
            int index = ((Number)parameters[1]).NumberAsInt;
            LispObject newValue = parameters[2];
            return array.SetElt(index, newValue);
        }
    }
}
