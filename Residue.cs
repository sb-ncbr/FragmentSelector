using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragmentSelector
{
    class Residue : IComparable<Residue>
    {
        private String name;
        public String Name
        {
            get
            {
                return name;
            }
            private set
            {
                if (value.Length == 3)
                {
                    name = value;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }
        public char ChainID { get; private set; }
        public int ResSeq { get; private set; }
        private SortedSet<Atom> atoms;

        public Residue(String name, char chainID, int resSeq)
        {
            Program.logger.Debug("new Residue(" + name + " " + chainID + " " + resSeq);
            Name = name;
            ChainID = chainID;
            ResSeq = resSeq;
            atoms = new SortedSet<Atom>();
        }

        public override bool Equals(object obj)
        {
            if (obj is Residue)
            {
                Residue res = obj as Residue;
                return Name == res.Name && ChainID == res.ChainID && ResSeq == res.ResSeq;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + ChainID.GetHashCode() + ResSeq.GetHashCode();
        }

        public int CompareTo(Residue other)
        {
            int result = this.ChainID - other.ChainID;
            if (result != 0) return result;
            result = this.ResSeq - other.ResSeq;
            if (result != 0) return result;
            return this.Name.CompareTo(other.Name);
        }

        public SortedSet<Atom> GetAtoms() { return new SortedSet<Atom>(atoms); }
        
        public void AddAtom(Atom atom)
        {
            if (atom.ResName != name)
                throw new ArgumentException(name + " expected " + atom.ResName + " found");
            if (atom.ChainID != ChainID)
                throw new ArgumentException(ChainID + " expected " + atom.ChainID + " found");
            if (atom.ResSeq != ResSeq)
                throw new ArgumentException(ResSeq + " expected " + atom.ResSeq + " found");
            atoms.Add(atom);
        }

        // public class DistanceComparer : IComparer<Residue>
        // {
        //     public Atom CentralAtom { get; private set; }

        //     public DistanceComparer(Atom centralAtom)
        //     {
        //         CentralAtom = centralAtom;
        //     }

        //     public int Compare(Residue r1, Residue r2)
        //     {
        //         Atom a1 = null;
        //         Atom a2 = null;
        //         foreach (Atom atom in r1.GetAtoms()) if (atom.Name.Equals(" CA ")) a1 = atom;
        //         foreach (Atom atom in r2.GetAtoms()) if (atom.Name.Equals(" CA ")) a2 = atom;
        //         if (a1 == null && a2 == null) return a1.CompareTo(a1);
        //         if (a1 == null) return 1;
        //         if (a2 == null) return -1;
        //         int result = Math.Sign(Lib.Distance(a1, CentralAtom) - Lib.Distance(a2, CentralAtom));
        //         if (result != 0) return result;
        //         else return a1.CompareTo(a2);
        //     }
        // }

        public class DistanceComparer : IComparer<Residue>
        {
            public Atom[] CentralAtoms { get; private set; }

            public DistanceComparer(Atom[] centralAtoms)
            {
                if (centralAtoms.Length == 0){
                    throw new ArgumentException("centralAtoms must not be empty");
                }
                CentralAtoms = centralAtoms;
            }

            public int Compare(Residue r1, Residue r2)
            {
                Atom a1 = null;
                Atom a2 = null;
                foreach (Atom atom in r1.GetAtoms()) if (atom.Name.Equals(" CA ")) a1 = atom;
                foreach (Atom atom in r2.GetAtoms()) if (atom.Name.Equals(" CA ")) a2 = atom;
                if (a1 == null && a2 == null) return a1.CompareTo(a1);
                if (a1 == null) return 1;
                if (a2 == null) return -1;
                double d1 = CentralAtoms.Min(a => Lib.Distance(a, a1));
                double d2 = CentralAtoms.Min(a => Lib.Distance(a, a2));
                int result = Math.Sign(d1 - d2);
                if (result != 0) return result;
                else return a1.CompareTo(a2);
            }
        }
        
    }
}
