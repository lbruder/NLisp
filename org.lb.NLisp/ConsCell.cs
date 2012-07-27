using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.lb.NLisp
{
    internal sealed class ConsCell : LispObject, IEnumerable<LispObject>
    {
        private static readonly Symbol quoteSym = Symbol.fromString("quote");
        private static readonly Symbol ifSym = Symbol.fromString("if");
        private static readonly Symbol prognSym = Symbol.fromString("progn");
        private static readonly Symbol defineSym = Symbol.fromString("define");
        private static readonly Symbol defunSym = Symbol.fromString("defun");
        private static readonly Symbol setSym = Symbol.fromString("setq");
        private static readonly Symbol lambdaSym = Symbol.fromString("lambda");
        private static readonly Symbol whileSym = Symbol.fromString("while");

        private readonly LispObject car;
        private readonly LispObject cdr;
        private ConsCell(LispObject car, LispObject cdr) { this.car = car; this.cdr = cdr; }
        public static ConsCell Cons(LispObject car, LispObject cdr) { return new ConsCell(car, cdr); }
        internal override LispObject Car() { return car; }
        internal override LispObject Cdr() { return cdr; }
        internal override LispObject Add(LispObject other) { var list = this.ToList(); list.AddRange(OtherConsCell(other, "+")); return FromClrObject(list); }
        private ConsCell OtherConsCell(LispObject other, string op)
        {
            ConsCell n = other as ConsCell;
            if (n == null) throw new InvalidOperationException(this, other, op);
            return n;
        }

        internal override LispObject Eval(Environment env)
        {
            if (quoteSym.Equals(car)) return ((ConsCell)cdr).car;
            if (ifSym.Equals(car)) return EvalIf(env);
            if (prognSym.Equals(car)) return EvalProgn(env);
            if (defineSym.Equals(car)) return EvalDefine(env);
            if (setSym.Equals(car)) return EvalSet(env);
            if (lambdaSym.Equals(car)) return EvalLambda(env);
            if (defunSym.Equals(car)) return EvalDefun(env);
            if (whileSym.Equals(car)) return EvalWhile(env);
            return EvalCall(env);
        }

        private LispObject EvalIf(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length != 4) throw new ExpectedNParametersGotMException(car, 3, asArray.Length - 1);
            return asArray[1].Eval(env).IsTrue() ? asArray[2].Eval(env) : asArray[3].Eval(env);
        }

        private LispObject EvalProgn(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length == 1) return Nil.GetInstance();
            for (int i = 1; i < asArray.Length - 1; ++i) asArray[i].Eval(env);
            return asArray[asArray.Length - 1].Eval(env);
        }

        private LispObject EvalDefine(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length != 3) throw new ExpectedNParametersGotMException(car, 2, asArray.Length - 1);
            if (asArray[1] is Symbol) return env.Define((Symbol)asArray[1], asArray[2].Eval(env));
            throw new SymbolExpectedException(asArray[1]);
        }

        private LispObject EvalSet(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length != 3) throw new ExpectedNParametersGotMException(car, 2, asArray.Length - 1);
            if (asArray[1] is Symbol) return env.Set((Symbol)asArray[1], asArray[2].Eval(env));
            throw new SymbolExpectedException(asArray[1]);
        }

        private LispObject EvalLambda(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length < 2) throw new ExpectedAtLeastNParametersGotMException(car, 1, asArray.Length - 1);
            if (asArray[1] is ConsCell) return new Lambda(env, (ConsCell)asArray[1], this.Skip(2).ToList());
            if (asArray[1] is Nil) return new Lambda(env, new LispObject[] { }, this.Skip(2).ToList());
            throw new ListExpectedException(asArray[1]);
        }

        private LispObject EvalDefun(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length < 4) throw new ExpectedAtLeastNParametersGotMException(car, 3, asArray.Length - 1);
            if (!(asArray[1] is Symbol)) throw new SymbolExpectedException(asArray[1]);
            if (asArray[2] is ConsCell) return env.Define((Symbol)asArray[1], new Lambda(env, (ConsCell)asArray[2], this.Skip(3).ToList()));
            if (asArray[2] is Nil) return env.Define((Symbol)asArray[1], new Lambda(env, new LispObject[] { }, this.Skip(3).ToList()));
            throw new ListExpectedException(asArray[2]);
        }

        private LispObject EvalWhile(Environment env)
        {
            var asArray = this.ToArray();
            if (asArray.Length < 3) throw new ExpectedAtLeastNParametersGotMException(car, 2, asArray.Length - 1);
            while (asArray[1].Eval(env).IsTrue()) for (int i = 2; i < asArray.Length; ++i) asArray[i].Eval(env);
            return Nil.GetInstance();
        }

        private LispObject EvalCall(Environment env)
        {
            var f = car.Eval(env);
            if (f is LispFunction) return ((LispFunction)f).Call(this.Skip(1).Select(o => o.Eval(env)).ToList());
            throw new UndefinedFunctionException(car);
        }

        public IEnumerator<LispObject> GetEnumerator()
        {
            yield return car;
            LispObject i = cdr;
            while (true)
            {
                if (i.NullP()) yield break;
                var asConsp = i as ConsCell;
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
                var asConsp = i as ConsCell;
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
            var other = obj as ConsCell;
            return (other != null) && other.car.Equals(car) && other.cdr.Equals(cdr);
        }

        public override int GetHashCode() { return 17 + car.GetHashCode() * 31 + cdr.GetHashCode(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}