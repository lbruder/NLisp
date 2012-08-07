using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace org.lb.NLisp
{
    // TODO: Quasiquoting, Arrays, Dotted Lists
    internal sealed class Reader
    {
        private static readonly Symbol defmacroSymbol = Symbol.fromString("defmacro");
        private static readonly Symbol quoteSymbol = Symbol.fromString("quote");
        private readonly NLisp lisp;
        private TextReader reader;
        private bool expandMacros;

        private enum Mode
        {
            normal,
            quoting,
            //TODO: quasiquoting
        }

        public Reader(NLisp lisp)
        {
            this.lisp = lisp;
        }

        private char Peek()
        {
            int p = reader.Peek();
            if (p == -1) throw new UnexpectedEndOfStreamException();
            return (char)p;
        }

        public LispObject Read(TextReader rd, bool expandMacros)
        {
            this.expandMacros = expandMacros;
            reader = rd;
            return Read(Mode.normal);
        }

        private LispObject Read(Mode mode)
        {
            SkipWhitespace();
            char c = Peek();
            if (c == ')') throw new ExtraneousClosingParenException();
            if (c == ';')
            {
                SkipToEndOfLine();
                return Read(mode);
            }
            if (c == '\'')
            {
                reader.Read();
                return ConsCell.Cons(quoteSymbol, ConsCell.Cons(Read(Mode.quoting), Nil.GetInstance()));
            }
            if (c == '(') return ReadCons(mode);
            if (c == '"') return ReadString();
            return ReadSymbolOrNumber();
        }

        private void SkipWhitespace()
        {
            while (Char.IsWhiteSpace(Peek())) reader.Read();
        }

        private void SkipToEndOfLine()
        {
            while (Peek() != '\n') reader.Read();
        }

        private LispObject ReadCons(Mode mode)
        {
            var ret = new List<LispObject>();
            reader.Read(); // Opening parenthesis
            SkipWhitespace();
            while (Peek() != ')')
            {
                ret.Add(Read(mode));
                SkipWhitespace();
            }
            reader.Read(); // Closing parenthesis
            return EvalMacros(mode, ret);
        }

        private LispObject EvalMacros(Mode mode, List<LispObject> list)
        {
            if (!expandMacros) return LispObject.FromClrObject(list);

            if (mode == Mode.normal && list.Count > 0 && list[0] is Symbol)
            {
                if (defmacroSymbol.Equals(list[0]))
                {
                    Symbol name = (Symbol)list[1];
                    list[0] = Symbol.fromString("lambda");
                    list.RemoveAt(1);
                    lisp.AddMacro(name, (Lambda)lisp.Eval(list));
                    return T.GetInstance();
                }
            }
            return (mode == Mode.normal)
                ? lisp.Macroexpand(LispObject.FromClrObject(list))
                : LispObject.FromClrObject(list);
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

        private LispObject ReadSymbolOrNumber()
        {
            var value = new StringBuilder();
            while (reader.Peek() != -1 && (Peek() != ')' && !Char.IsWhiteSpace(Peek()))) value.Append((char)reader.Read());
            double d; if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return new Number(d);
            return Symbol.fromString(value.ToString());
        }
    }
}
