using System;
using System.IO;
using System.Text;

namespace org.lb.NLisp
{
    internal sealed class LispStream : LispObject, IDisposable
    {
        private readonly Stream stream;
        private readonly StreamReader reader;
        private readonly StreamWriter writer;

        public StreamReader GetReadStream()
        {
            if (reader != null) return reader;
            throw new StreamNotReadableException();
        }

        public StreamWriter GetWriteStream()
        {
            if (writer != null) return writer;
            throw new StreamNotWritableException();
        }

        public LispStream(Stream stream)
        {
            this.stream = stream;

            try { this.reader = new StreamReader(stream, Encoding.UTF8); }
            catch { this.reader = null; }

            try { this.writer = new StreamWriter(stream, Encoding.UTF8); }
            catch { this.writer = null; }
        }

        internal override LispObject Eval(Environment env) { throw new StreamCanNotBeEvaluatedException(); }
        public override string ToString() { return "#<stream>"; }
        public override bool Equals(object obj) { return obj is LispStream && ((LispStream)obj).stream.Equals(stream); }
        public override int GetHashCode() { return stream.GetHashCode(); }

        ~LispStream()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (reader != null) reader.Dispose();
            if (writer != null) writer.Dispose();
            stream.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}