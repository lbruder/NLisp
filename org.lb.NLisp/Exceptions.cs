using System;

namespace org.lb.NLisp
{
    // TODO: New base classes LispException, LispRuntimeError, LispCliError

    public sealed class LispConstantCanNotBeChangedException : Exception
    {
        internal LispConstantCanNotBeChangedException(Symbol sym)
            : base(sym + " is a constant and can not be changed") // TODO: I18N
        {
        }
    }

    public sealed class LispCannotEvaluateEmptyListException : Exception
    {
        internal LispCannotEvaluateEmptyListException()
            : base("Can not evaluate empty list") // TODO: I18N
        {
        }
    }

    public sealed class LispObjectCouldNotBeConvertedException : Exception
    {
        internal LispObjectCouldNotBeConvertedException(object obj)
            : base("CLR object of type " + obj.GetType() + " could not be converted to LispObject") // TODO: I18N
        {
        }
    }

    public sealed class LispObjectIsNotAListException : Exception
    {
        internal LispObjectIsNotAListException(object obj)
            : base("The value " + obj + " is not a list") // TODO: I18N
        {
        }
    }

    public sealed class LispInvalidOperationException : Exception
    {
        internal LispInvalidOperationException(LispObject o1, string op)
            : base("Invalid operation: (" + op + " " + o1 + ")") // TODO: I18N
        {
        }
        internal LispInvalidOperationException(LispObject o1, LispObject o2, string op)
            : base("Invalid operation: (" + op + " " + o1 + " " + o2 + ")") // TODO: I18N
        {
        }
    }

    public sealed class LispDivisionByZeroException : Exception
    {
        internal LispDivisionByZeroException()
            : base("Division by zero") // TODO: I18N
        {
        }
    }

    public sealed class LispUnexpectedEndOfStreamException : Exception
    {
        internal LispUnexpectedEndOfStreamException()
            : base("Unexpected end of stream") // TODO: I18N
        {
        }
    }

    public sealed class LispSymbolNotFoundException : Exception
    {
        internal LispSymbolNotFoundException(Symbol symbol)
            : base("Undefined symbol " + symbol) // TODO: I18N
        {
        }
    }

    public sealed class LispExpectedNParametersGotMException : Exception
    {
        internal LispExpectedNParametersGotMException(LispObject symbol, int expected, int got)
            : base(symbol + ": Expected " + expected + " parameter(s), got " + got) // TODO: I18N
        {
        }
    }

    public sealed class LispExpectedAtLeastNParametersGotMException : Exception
    {
        internal LispExpectedAtLeastNParametersGotMException(LispObject symbol, int expected, int got)
            : base(symbol + ": Expected at least " + expected + " parameter(s), got " + got) // TODO: I18N
        {
        }
    }

    public sealed class LispSymbolExpectedException : Exception
    {
        internal LispSymbolExpectedException(LispObject got)
            : base("Expected symbol, got " + got) // TODO: I18N
        {
        }
    }

    public sealed class LispListExpectedException : Exception
    {
        internal LispListExpectedException(LispObject got)
            : base("Expected list, got " + got) // TODO: I18N
        {
        }
    }

    public sealed class LispUndefinedFunctionException : Exception
    {
        internal LispUndefinedFunctionException(LispObject got)
            : base("Undefined function " + got) // TODO: I18N
        {
        }
    }
}
