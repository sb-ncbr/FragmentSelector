using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BP01
{
    class Chain : IComparable<Chain>
    {
        public char ID { get; private set; }
        private ChainBuilder chainBuilder;
        public int FirstResidueNumber { get { return chainBuilder.FirstResidueNumber; } }
        public int LastResidueNumber { get { return chainBuilder.LastResidueNumber; } }
        public int Count { get { return chainBuilder.Count; } }

        public SortedSet<Residue> GetResidues() { return chainBuilder.GetResidues(); }
        public SortedSet<Atom> GetAtoms() { return chainBuilder.GetAtoms(); }

        public Residue GetResidue(int resSeq) {
            if (GetResidues().Count == 0 || resSeq < FirstResidueNumber || resSeq > LastResidueNumber)
            {
                throw new IndexOutOfRangeException();
            }
            return chainBuilder.GetResidue(resSeq);
        }

        public void AddResidue(int resSeq, Residue newResidue)
        {
            if (Count == 0 || resSeq == FirstResidueNumber - 1 || resSeq == LastResidueNumber + 1)
            {
                chainBuilder.AddResidue(newResidue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public Chain()
        {
            chainBuilder = new ChainBuilder();
        }
        
        public void AddAtom(Atom atom) { chainBuilder.AddAtom(atom); }

        public int CompareTo(Chain other) { return this.chainBuilder.CompareTo(other.chainBuilder); }

        public class LengthComparer : IComparer<Chain>
        {
            public int Compare(Chain c1, Chain c2) {
                int result = c1.Count - c2.Count;
                if (result !=0) return result;
                else return c1.CompareTo(c2);
            }
        }
    }
}
