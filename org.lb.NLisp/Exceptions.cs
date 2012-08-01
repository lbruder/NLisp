using System;

namespace org.lb.NLisp
{
    // TODO: New base classes LispException, LispRuntimeError, LispCliError

    public sealed class ExtraneousClosingParenException : Exception
    {
        internal ExtraneousClosingParenException()
            : base("Extraneous )") // TODO: I18N
        {
        }
    }

    public sealed class ConstantCanNotBeChangedException : Exception
    {
        internal ConstantCanNotBeChangedException(Symbol sym)
            : base(sym + " is a constant and can not be changed") // TODO: I18N
        {
        }
    }

    public sealed class StreamCanNotBeEvaluatedException : Exception
    {
        internal StreamCanNotBeEvaluatedException()
            : base("Streams can not be evaluated") // TODO: I18N
        {
        }
    }

    public sealed class StreamNotReadableException : Exception
    {
        internal StreamNotReadableException()
            : base("Stream is not readable") // TODO: I18N
        {
        }
    }

    public sealed class StreamNotWritableException : Exception
    {
        internal StreamNotWritableException()
            : base("Stream is not readable") // TODO: I18N
        {
        }
    }

    public sealed class ObjectCouldNotBeConvertedException : Exception
    {
        internal ObjectCouldNotBeConvertedException(object obj)
            : base("CLR object of type " + obj.GetType() + " could not be converted to LispObject") // TODO: I18N
        {
        }
    }

    public sealed class ObjectIsNotAListException : Exception
    {
        internal ObjectIsNotAListException(object obj)
            : base("The value " + obj + " is not a list") // TODO: I18N
        {
        }
    }

    public sealed class InvalidOperationException : Exception
    {
        internal InvalidOperationException(LispObject o1, string op)
            : base("Invalid operation: (" + op + " " + o1 + ")") // TODO: I18N
        {
        }
        internal InvalidOperationException(LispObject o1, LispObject o2, string op)
            : base("Invalid operation: (" + op + " " + o1 + " " + o2 + ")") // TODO: I18N
        {
        }
    }

    public sealed class DivisionByZeroException : Exception
    {
        internal DivisionByZeroException()
            : base("Division by zero") // TODO: I18N
        {
        }
    }

    public sealed class UnexpectedEndOfStreamException : Exception
    {
        internal UnexpectedEndOfStreamException()
            : base("Unexpected end of stream") // TODO: I18N
        {
        }
    }

    public sealed class SymbolNotFoundException : Exception
    {
        internal SymbolNotFoundException(Symbol symbol)
            : base("Undefined symbol " + symbol) // TODO: I18N
        {
        }
    }

    public sealed class ExpectedNParametersGotMException : Exception
    {
        internal ExpectedNParametersGotMException(LispObject symbol, int expected, int got)
            : base(symbol + ": Expected " + expected + " parameter(s), got " + got) // TODO: I18N
        {
        }
    }

    public sealed class ExpectedAtLeastNParametersGotMException : Exception
    {
        internal ExpectedAtLeastNParametersGotMException(LispObject symbol, int expected, int got)
            : base(symbol + ": Expected at least " + expected + " parameter(s), got " + got) // TODO: I18N
        {
        }
    }

    public sealed class SymbolExpectedException : Exception
    {
        internal SymbolExpectedException(LispObject got)
            : base("Expected symbol, got " + got) // TODO: I18N
        {
        }
    }

    public sealed class ListExpectedException : Exception
    {
        internal ListExpectedException(LispObject got)
            : base("Expected list, got " + got) // TODO: I18N
        {
        }
    }

    public sealed class UndefinedFunctionException : Exception
    {
        internal UndefinedFunctionException(LispObject got)
            : base("Undefined function " + got) // TODO: I18N
        {
        }
    }

    public sealed class RestSymbolNotAllowedHereException : Exception
    {
        internal RestSymbolNotAllowedHereException()
            : base("&rest symbol not allowed here") // TODO: I18N
        {
        }
    }
}
