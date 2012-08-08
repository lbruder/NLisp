using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace org.lb.NLisp
{
    internal sealed class Array : LispObject, IEnumerable<LispObject>
    {
        private readonly List<LispObject> values = new List<LispObject>();
        private readonly LispObject initialElement;
        public Array(int dimension, LispObject initialElement) { this.initialElement = initialElement; GrowArrayIfNecessary(dimension - 1); }
        internal override LispObject Eval(Environment env) { return this; }
        public IEnumerator<LispObject> GetEnumerator() { return values.GetEnumerator(); }

        internal override LispObject Elt(int index)
        {
            GrowArrayIfNecessary(index);
            return values[index];
        }

        internal override LispObject SetElt(int index, LispObject value)
        {
            GrowArrayIfNecessary(index);
            return values[index] = value;
        }

        private void GrowArrayIfNecessary(int requestedIndex)
        {
            int oldSize = values.Count;
            if (requestedIndex < oldSize) return;
            for (int i = oldSize; i <= requestedIndex; ++i)
                values.Add(initialElement);
        }

        public override string ToString()
        {
            var ret = new StringBuilder();
            ret.Append("[");
            bool firstItem = true;

            foreach (var item in values)
            {
                if (firstItem) firstItem = false; else ret.Append(' ');
                ret.Append(item.ToString());
            }

            ret.Append(']');
            return ret.ToString();
        }

        public override bool Equals(object obj)
        {
            var other = obj as Array;
            return (other != null) && other.values.Equals(values);
        }

        public override int GetHashCode() { return values.GetHashCode(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}