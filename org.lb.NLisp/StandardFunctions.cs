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
            if (obj.NullP()) return LispObject.FromClrObject(0);
            if (obj is IEnumerable<LispObject>) return LispObject.FromClrObject(((IEnumerable<LispObject>)obj).Count());
            throw new InvalidOperationException(obj, "length");
        }
    }

    // TODO: Refactor this file!!!

    internal sealed class BuiltinRandomFunction : BuiltinLispFunction
    {
        private static readonly Random random = new Random();
        public BuiltinRandomFunction() : base("random") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 1);
            return FromClrObject(random.Next(((Number)parameters[0]).NumberAsInt));
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

    internal sealed class BuiltinApplyFunction : BuiltinLispFunction
    {
        public BuiltinApplyFunction() : base("apply") { }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 2);
            return ((LispFunction) parameters[0]).Call(parameters[1].NullP() ? new List<LispObject>() : ((ConsCell)parameters[1]).ToList());
        }
    }

    internal sealed class BuiltinGetSymbolsFunction : BuiltinLispFunction
    {
        private readonly Environment global;
        public BuiltinGetSymbolsFunction(Environment global) : base("sys:get-symbols") { this.global = global; }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 0);
            return FromClrObject(global.GetSymbols());
        }
    }
    
    internal sealed class BuiltinGetMacrosFunction : BuiltinLispFunction
    {
        private readonly Environment global;
        public BuiltinGetMacrosFunction(Environment global) : base("sys:get-macros") { this.global = global; }
        public override LispObject Call(List<LispObject> parameters)
        {
            AssertParameterCount(parameters, 0);
            return FromClrObject(global.GetMacros());
        }
    }

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
